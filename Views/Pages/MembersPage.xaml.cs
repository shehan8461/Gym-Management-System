using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;
using System.Windows.Threading;
using GymManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using GymManagementSystem.Models;

namespace GymManagementSystem.Views.Pages
{
    public partial class MembersPage : Page
    {
        public MembersPage()
        {
            InitializeComponent();
            
            // Initialize Polling Timer
            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromSeconds(2);
            _pollTimer.Tick += PollTimer_Tick;

            this.Loaded += MembersPage_Loaded;
            this.Unloaded += MembersPage_Unloaded;

            LoadMembers();
        }

        private System.Threading.CancellationTokenSource _searchCts;
        private System.Threading.CancellationTokenSource _loadCts;
        private DispatcherTimer _pollTimer;
        private DateTime _lastPollTime = DateTime.Now;

        private void MembersPage_Loaded(object sender, RoutedEventArgs e)
        {
            _pollTimer.Start();
        }

        private void MembersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _pollTimer.Stop();
        }

        private async void PollTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Find first connected device
                    var device = context.BiometricDevices.FirstOrDefault(d => d.IsConnected);
                    if (device != null)
                    {
                        using (var service = new HikvisionService())
                        {
                            // We need to re-authenticate/connect to set the BaseURL
                            // In a production app, we might maintain a singleton service, 
                            // but for now, we quickly reconnect (lightweight HTTP)
                            var connected = await service.ConnectAsync(device.IPAddress, device.Port, device.Username, device.Password);
                            
                            if (connected.success)
                            {
                                var events = await service.GetRecentEventsAsync(_lastPollTime);
                                if (events != null && events.Any())
                                {
                                    // Process events older to newer
                                    foreach (var evt in events.OrderBy(x => x.GetDateTime()))
                                    {
                                        var evtTime = evt.GetDateTime();
                                        if (evtTime > _lastPollTime)
                                        {
                                            _lastPollTime = evtTime;
                                            
                                            // Only process fingerprint verification events (minor=25 is fingerprint)
                                            // currentVerifyMode: 25 = fingerprint, 1 = card, etc.
                                            bool isFingerprintEvent = evt.currentVerifyMode == 25 || evt.minor == 25;
                                            
                                            if (!isFingerprintEvent)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Skipping non-fingerprint event: verifyMode={evt.currentVerifyMode}, minor={evt.minor}");
                                                continue;
                                            }
                                            
                                            // Check if it's a known member
                                            if (!string.IsNullOrEmpty(evt.employeeNoString) && int.TryParse(evt.employeeNoString, out int memberId))
                                            {
                                                var member = context.Members.Find(memberId);
                                                if (member != null)
                                                {
                                                    // Verify fingerprint is enrolled on device
                                                    bool fingerprintEnrolled = await service.CheckFingerprintEnrolledAsync(memberId);
                                                    
                                                    if (fingerprintEnrolled)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"✅ Fingerprint match found for Member ID: {memberId} ({member.FullName})");
                                                        
                                                        // Check if attendance already recorded today (prevent duplicates)
                                                        var today = DateTime.UtcNow.Date;
                                                        var existingAttendance = context.Attendances
                                                            .FirstOrDefault(a => a.MemberId == memberId && 
                                                                                 a.CheckInDate.Date == today &&
                                                                                 a.CheckOutDate == null);
                                                        
                                                        if (existingAttendance == null)
                                                        {
                                                            // Record new attendance
                                                            var attendance = new Models.Attendance
                                                            {
                                                                MemberId = memberId,
                                                                CheckInDate = DateTime.UtcNow,
                                                                CheckInTime = DateTime.UtcNow.TimeOfDay,
                                                                AttendanceType = "Biometric",
                                                                Remarks = "Fingerprint scan - Auto recorded"
                                                            };
                                                            
                                                            context.Attendances.Add(attendance);
                                                            context.SaveChanges();
                                                            
                                                            System.Diagnostics.Debug.WriteLine($"📝 Attendance recorded for {member.FullName} at {DateTime.Now:HH:mm:ss}");
                                                        }
                                                        else
                                                        {
                                                            System.Diagnostics.Debug.WriteLine($"ℹ️ Attendance already recorded today for {member.FullName}");
                                                        }
                                                        
                                                        // Stop timer while dialog is open to prevent overlap
                                                        _pollTimer.Stop();
                                                        
                                                        // Show member history dialog with fingerprint match confirmation
                                                        var dialog = new MemberHistoryDialog(memberId, true);
                                                        dialog.ShowDialog();
                                                        
                                                        _pollTimer.Start();
                                                    }
                                                    else
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"⚠️ Member {memberId} ({member.FullName}) scanned but fingerprint NOT enrolled on device");
                                                        // Could show a notification here if needed
                                                    }
                                                }
                                                else
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"⚠️ Fingerprint scanned for EmployeeNo={evt.employeeNoString} but member not found in database");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail on polling errors to not disrupt UI
                System.Diagnostics.Debug.WriteLine($"Polling error: {ex.Message}");
            }
        }

        private async void LoadMembers(string searchTerm = "")
        {
            // Cancel previous loading operation if it's still running
            _loadCts?.Cancel();
            _loadCts = new System.Threading.CancellationTokenSource();
            var token = _loadCts.Token;

            try
            {
                // Run heavy DB and processing logic on background thread
                var result = await Task.Run(async () =>
                {
                    if (token.IsCancellationRequested) return null;

                    using (var context = new GymDbContext())
                    {
                        var today = DateTime.UtcNow.Date;
                        
                        // Use eager loading to prevent N+1 query problem
                        var query = context.Members
                            .Include(m => m.AssignedPackage)  // Load package data
                            .AsQueryable();

                        if (!string.IsNullOrEmpty(searchTerm))
                        {
                            var lowerTerm = searchTerm.ToLower();
                            query = query.Where(m => 
                                m.FullName.ToLower().Contains(lowerTerm) || 
                                m.PhoneNumber.Contains(searchTerm) || // Phone numbers usually numeric/exact
                                m.NIC.ToLower().Contains(lowerTerm));
                        }

                        // Fetch members asynchronously with cancellation check
                        var members = await query.OrderByDescending(m => m.RegistrationDate).ToListAsync(token);
                        
                        if (token.IsCancellationRequested) return null;

                        // Get all member IDs for batch payment / fingerprint history queries
                        var memberIds = members.Select(m => m.MemberId).ToList();
                        
                        // Load all payments in one query (prevents N+1)
                        var lastPayments = await context.Payments
                            .Where(p => memberIds.Contains(p.MemberId))
                            .ToListAsync(token); 

                        if (token.IsCancellationRequested) return null;

                        // Load fingerprint enrollment history in one query
                        var fingerprintHistory = await context.FingerprintEnrollmentHistories
                            .Where(f => memberIds.Contains(f.MemberId))
                            .OrderByDescending(f => f.EnrollmentTimeUtc)
                            .ToListAsync(token);

                        // Process in-memory (Grouping)
                        var lastPaymentsGrouped = lastPayments
                            .GroupBy(p => new { p.MemberId, p.PackageId })
                            .Select(g => g.OrderByDescending(p => p.PaymentDate).FirstOrDefault())
                            .ToList();

                        var lastFingerprintByMember = fingerprintHistory
                            .GroupBy(f => f.MemberId)
                            .Select(g => g.OrderByDescending(f => f.EnrollmentTimeUtc).FirstOrDefault())
                            .Where(f => f != null)
                            .ToDictionary(f => f!.MemberId, f => f!);
                        
                        // Calculate payment + fingerprint status for each member
                        foreach (var member in members)
                        {
                            // Get assigned package
                            if (member.AssignedPackageId.HasValue)
                            {
                                member.AssignedPackageName = member.AssignedPackage?.PackageName ?? "Unknown";
                                
                                // Get last payment for the CURRENT assigned package from preloaded data
                                var lastPaymentForCurrentPackage = lastPaymentsGrouped
                                    .FirstOrDefault(p => p.MemberId == member.MemberId && p.PackageId == member.AssignedPackageId.Value);
                                
                                if (lastPaymentForCurrentPackage != null)
                                {
                                    // Show dates from the payment for the current package
                                    member.LastPaymentDate = lastPaymentForCurrentPackage.PaymentDate;
                                    member.NextDueDate = lastPaymentForCurrentPackage.NextDueDate;
                                    
                                    // Calculate payment status
                                    var daysUntilDue = (lastPaymentForCurrentPackage.NextDueDate.Date - today).Days;
                                    
                                    if (daysUntilDue < 0)
                                    {
                                        member.PaymentStatus = "Overdue";
                                    }
                                    else if (daysUntilDue <= 7)
                                    {
                                        member.PaymentStatus = "Due Soon";
                                    }
                                    else
                                    {
                                        member.PaymentStatus = "Paid";
                                    }
                                }
                                else
                                {
                                    // Package assigned but no payment yet
                                    member.PaymentStatus = "Payment Required";
                                    member.LastPaymentDate = null;
                                    member.NextDueDate = null;
                                }
                            }
                            else
                            {
                                member.AssignedPackageName = "No Package";
                                member.PaymentStatus = "No Package";
                                member.LastPaymentDate = null;
                                member.NextDueDate = null;
                            }

                            // Fingerprint summary
                            if (lastFingerprintByMember.TryGetValue(member.MemberId, out var fp))
                            {
                                member.LastFingerprintEnrollmentDateUtc = fp.EnrollmentTimeUtc;
                                if (fp.IsSuccess)
                                {
                                    member.FingerprintStatus = "Enrolled";
                                }
                                else
                                {
                                    member.FingerprintStatus = $"Last failed ({fp.Status})";
                                }
                            }
                            else
                            {
                                member.FingerprintStatus = "Not Enrolled";
                                member.LastFingerprintEnrollmentDateUtc = null;
                            }
                        }
                        
                        return members;
                    }
                }, token);

                // If operation was cancelled or returned null, do nothing
                if (token.IsCancellationRequested || result == null) return;

                // Update UI on main thread
                dgMembers.ItemsSource = result;
                
                // Update statistics cards
                UpdateStatistics(result);
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                // Ignore Oracle cancellation error (ORA-01013)
                if (ex.Message.Contains("ORA-01013") || ex.Message.Contains("user requested cancel"))
                {
                    return;
                }

                MessageBox.Show($"Error loading members: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics(System.Collections.Generic.List<Models.Member> members)
        {
            txtTotalMembers.Text = members.Count.ToString();
            txtActiveMembers.Text = members.Count(m => m.IsActive).ToString();
            txtExpiringSoon.Text = members.Count(m => m.PaymentStatus == "Due Soon").ToString();
            txtOverdue.Text = members.Count(m => m.PaymentStatus == "Overdue").ToString();
        }

        private void btnAddMember_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditMemberDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadMembers();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new AddEditMemberDialog(memberId);
                if (dialog.ShowDialog() == true)
                {
                    LoadMembers();
                }
            }
        }

        private void btnPayment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                // Check if current package payment is already completed
                using (var context = new GymDbContext())
                {
                    var member = context.Members.FirstOrDefault(m => m.MemberId == memberId);
                    
                    if (member == null)
                    {
                        MessageBox.Show("Member not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Check if member has an assigned package
                    if (member.AssignedPackageId.HasValue)
                    {
                        // Get the last payment for this member and the assigned package
                        var lastPayment = context.Payments
                            .Where(p => p.MemberId == memberId && p.PackageId == member.AssignedPackageId.Value)
                            .OrderByDescending(p => p.PaymentDate)
                            .FirstOrDefault();

                        if (lastPayment != null)
                        {
                            var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
                            
                            // Check if the package period is still active (not expired)
                            var daysUntilEnd = (lastPayment.EndDate.Date - today).Days;
                            var daysUntilDue = (lastPayment.NextDueDate.Date - today).Days;
                            
                            // Payment is considered complete if:
                            // 1. Package hasn't expired (end date is in the future)
                            // 2. Payment is not due soon (more than 7 days until due)
                            if (daysUntilEnd >= 0 && daysUntilDue > 7)
                            {
                                var packageName = context.MembershipPackages
                                    .FirstOrDefault(p => p.PackageId == member.AssignedPackageId.Value)?.PackageName ?? "Unknown";
                                
                                MessageBox.Show(
                                    "Payment is already completed for the current package.\n\n" +
                                    $"Package: {packageName}\n" +
                                    $"Payment Date: {lastPayment.PaymentDate:dd/MM/yyyy}\n" +
                                    $"Next Due Date: {lastPayment.NextDueDate:dd/MM/yyyy}\n" +
                                    $"Package End Date: {lastPayment.EndDate:dd/MM/yyyy}\n" +
                                    $"Days Until Due: {daysUntilDue}\n\n" +
                                    "You can only make a payment when:\n" +
                                    "• The payment is due soon (within 7 days) or overdue\n" +
                                    "• The package has been changed to a different one\n" +
                                    "• The package period has expired",
                                    "Payment Already Completed",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                                return;
                            }
                        }
                    }
                }

                // If checks pass, open payment dialog
                var dialog = new AddPaymentDialog(memberId);
                if (dialog.ShowDialog() == true)
                {
                    LoadMembers();
                }
            }
        }
        
        private void btnAssignPackage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new AssignPackageDialog(memberId);
                if (dialog.ShowDialog() == true)
                {
                    LoadMembers();
                }
            }
        }
        
        private void btnBiometric_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new EnrollFingerprintDialog(memberId);
                dialog.ShowDialog();
            }
        }
        
        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new MemberHistoryDialog(memberId);
                dialog.ShowDialog();
            }
        }

        private void btnAttendance_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new MemberAttendanceHistoryDialog(memberId);
                dialog.ShowDialog();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is not int memberId)
                return;

            try
            {
                using (var context = new GymDbContext())
                {
                    var member = context.Members.FirstOrDefault(m => m.MemberId == memberId);
                    if (member == null)
                    {
                        MessageBox.Show("Member not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var paymentsCount = context.Payments.Count(p => p.MemberId == memberId);
                    var attendanceCount = context.Attendances.Count(a => a.MemberId == memberId);

                    var result = MessageBox.Show(
                        "Are you sure you want to delete this member?\n\n" +
                        $"Member: {member.FullName}\n" +
                        $"Payments: {paymentsCount}\n" +
                        $"Attendance: {attendanceCount}\n\n" +
                        "This will permanently delete the member and all related records.",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                        return;

                    using (var tx = context.Database.BeginTransaction())
                    {
                        var payments = context.Payments.AsTracking().Where(p => p.MemberId == memberId).ToList();
                        if (payments.Count > 0)
                            context.Payments.RemoveRange(payments);

                        var attendances = context.Attendances.AsTracking().Where(a => a.MemberId == memberId).ToList();
                        if (attendances.Count > 0)
                            context.Attendances.RemoveRange(attendances);

                        var trackedMember = context.Members.AsTracking().FirstOrDefault(m => m.MemberId == memberId);
                        if (trackedMember != null)
                            context.Members.Remove(trackedMember);

                        context.SaveChanges();
                        tx.Commit();
                    }
                }

                MessageBox.Show("Member deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadMembers(txtSearch.Text.Trim());
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? $"\n\nInner: {ex.InnerException.Message}" : "";
                MessageBox.Show($"Error deleting member: {ex.Message}{innerMsg}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadMembers();
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Debounce search input to prevent excessive DB queries
            _searchCts?.Cancel();
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token); // Wait 300ms
                LoadMembers(txtSearch.Text.Trim());
            }
            catch (TaskCanceledException)
            {
                // Ignored
            }
        }

        private void dgMembers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgMembers.SelectedItem != null)
            {
                var member = dgMembers.SelectedItem as dynamic;
                if (member != null)
                {
                    var dialog = new AddEditMemberDialog(member.MemberId);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadMembers();
                    }
                }
            }
        }
    }
}
