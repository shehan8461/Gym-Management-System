using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using GymManagementSystem.Data;
using GymManagementSystem.Models;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class MemberHistoryDialog : Window
    {
        private readonly int _memberId;
        private readonly bool _fingerprintMatch;

        public MemberHistoryDialog(int memberId, bool fingerprintMatch = false)
        {
            InitializeComponent();
            _memberId = memberId;
            _fingerprintMatch = fingerprintMatch;
            LoadMemberHistory();
        }

        private void LoadMemberHistory()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Load member details
                    var member = context.Members.FirstOrDefault(m => m.MemberId == _memberId);
                    
                    if (member == null)
                    {
                        MessageBox.Show("Member not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    // Display member info
                    txtMemberName.Text = member.FullName;
                    txtMemberId.Text = member.MemberId.ToString();
                    txtPhone.Text = member.PhoneNumber;
                    txtRegistrationDate.Text = member.RegistrationDate.ToString("dd/MM/yyyy");
                    
                    // Check fingerprint enrollment status on device
                    CheckFingerprintStatus(member);
                    
                    // Show notification if triggered by fingerprint match
                    if (_fingerprintMatch)
                    {
                        System.Windows.Media.Color successColor = System.Windows.Media.Color.FromRgb(76, 175, 80);
                        txtMemberName.Foreground = new SolidColorBrush(successColor);
                        
                        // You could also show a toast notification here
                        System.Diagnostics.Debug.WriteLine($"✅ Fingerprint Match - Member History displayed for {member.FullName}");
                    }

                    // Load member photo
                    try
                    {
                        if (!string.IsNullOrEmpty(member.PhotoPath) && System.IO.File.Exists(member.PhotoPath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(member.PhotoPath);
                            bitmap.EndInit();
                            imgMemberPhoto.ImageSource = bitmap;
                        }
                        else
                        {
                            // Could set a default image here if needed
                            imgMemberPhoto.ImageSource = null;
                        }
                    }
                    catch { /* Ignore image loading errors */ }

                    // Load current package info
                    if (member.AssignedPackageId.HasValue)
                    {
                        var package = context.MembershipPackages
                            .FirstOrDefault(p => p.PackageId == member.AssignedPackageId.Value);
                        txtCurrentPackage.Text = package?.PackageName ?? "Unknown";
                    }
                    else
                    {
                        txtCurrentPackage.Text = "No Package";
                    }

                    // Load payment history with package details
                    var payments = context.Payments
                        .Include(p => p.MembershipPackage)
                        .Where(p => p.MemberId == _memberId)
                        .OrderByDescending(p => p.PaymentDate)
                        .ToList();

                    dgPaymentHistory.ItemsSource = payments;

                    // Load attendance history
                    var rawAttendances = context.Attendances
                        .Where(a => a.MemberId == _memberId)
                        .OrderByDescending(a => a.CheckInDate)
                        .ThenByDescending(a => a.CheckInTime)
                        .ToList();

                    var attendances = rawAttendances
                        .Select(a => new
                        {
                            a.CheckInDate,
                            a.CheckInTime,
                            a.CheckOutTime,
                            a.AttendanceType,
                            a.Remarks,
                            DateDisplay = a.CheckInDate.ToLocalTime().ToString("dd/MM/yyyy"),
                            CheckInDisplay = a.CheckInTime.ToString(@"hh\:mm\:ss"),
                            CheckOutDisplay = a.CheckOutTime.HasValue ? a.CheckOutTime.Value.ToString(@"hh\:mm\:ss") : "-",
                            DurationDisplay = a.CheckOutTime.HasValue
                                ? CalculateDuration(a.CheckInTime, a.CheckOutTime.Value)
                                : "-"
                        })
                        .ToList();

                    dgAttendanceHistory.ItemsSource = attendances;
                    txtTotalAttendance.Text = $"Total Attendance: {attendances.Count} days";

                    // Load fingerprint enrollment history
                    var fpHistoryRaw = context.FingerprintEnrollmentHistories
                        .Where(f => f.MemberId == _memberId)
                        .OrderByDescending(f => f.EnrollmentTimeUtc)
                        .ToList();

                    var fpHistory = fpHistoryRaw
                        .Select(f => new
                        {
                            f.DeviceId,
                            f.Status,
                            f.IsSuccess,
                            f.Message,
                            EnrollmentLocal = f.EnrollmentTimeUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
                        })
                        .ToList();

                    dgFingerprintHistory.ItemsSource = fpHistory;

                    // Calculate and display summary
                    var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
                    
                    if (payments.Any())
                    {
                        var lastPayment = payments.First();
                        var totalAmount = payments.Sum(p => p.Amount);

                        txtTotalPayments.Text = payments.Count.ToString();
                        txtLastPaymentDate.Text = lastPayment.PaymentDate.ToString("dd/MM/yyyy");
                        txtTotalAmount.Text = $"Rs. {totalAmount:N2}";

                        // Check if the last payment is for the current assigned package
                        bool packageChanged = member.AssignedPackageId.HasValue && 
                                            lastPayment.PackageId != member.AssignedPackageId.Value;

                        if (packageChanged)
                        {
                            // Package changed but no payment yet for new package
                            // Calculate projected dates based on new package
                            var newPackage = context.MembershipPackages
                                .FirstOrDefault(p => p.PackageId == member.AssignedPackageId.Value);
                            
                            if (newPackage != null)
                            {
                                var projectedEndDate = today.AddMonths(newPackage.DurationMonths);
                                var projectedNextDue = today.AddMonths(newPackage.DurationMonths);
                                
                                txtPaymentStatus.Text = "PAYMENT REQUIRED";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Text = projectedNextDue.ToString("dd/MM/yyyy") + " (After Payment)";
                                txtNextDueDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                                txtPackageEndDate.Text = projectedEndDate.ToString("dd/MM/yyyy") + " (After Payment)";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                            }
                            else
                            {
                                txtPaymentStatus.Text = "PAYMENT REQUIRED";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Text = "Payment Required";
                                txtNextDueDate.Foreground = new SolidColorBrush(Colors.Red);
                                txtPackageEndDate.Text = "Payment Required";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Colors.Red);
                            }
                        }
                        else
                        {
                            // Display next due date
                            txtNextDueDate.Text = lastPayment.NextDueDate.ToString("dd/MM/yyyy");

                            // Display package end date
                            txtPackageEndDate.Text = lastPayment.EndDate.ToString("dd/MM/yyyy");

                            // Calculate payment status with color coding
                            var daysUntilDue = (lastPayment.NextDueDate.Date - today).Days;
                            var daysUntilEnd = (lastPayment.EndDate.Date - today).Days;

                            if (daysUntilDue < 0)
                            {
                                txtPaymentStatus.Text = "OVERDUE";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Foreground = new SolidColorBrush(Colors.Red);
                            }
                            else if (daysUntilDue <= 7)
                            {
                                txtPaymentStatus.Text = "DUE SOON";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                                txtNextDueDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                            }
                            else
                            {
                                txtPaymentStatus.Text = "PAID";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Green);
                                txtNextDueDate.Foreground = new SolidColorBrush(Colors.Green);
                            }

                            // Package end date color
                            if (daysUntilEnd < 0)
                            {
                                txtPackageEndDate.Text += " (EXPIRED)";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Colors.Red);
                            }
                            else if (daysUntilEnd <= 7)
                            {
                                txtPackageEndDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                            }
                            else
                            {
                                txtPackageEndDate.Foreground = new SolidColorBrush(Colors.Green);
                            }
                        }
                    }
                    else
                    {
                        // No payments at all
                        if (member.AssignedPackageId.HasValue)
                        {
                            // Calculate projected dates based on assigned package
                            var assignedPackage = context.MembershipPackages
                                .FirstOrDefault(p => p.PackageId == member.AssignedPackageId.Value);
                            
                            if (assignedPackage != null)
                            {
                                var projectedEndDate = today.AddMonths(assignedPackage.DurationMonths);
                                var projectedNextDue = today.AddMonths(assignedPackage.DurationMonths);
                                
                                txtPaymentStatus.Text = "PAYMENT REQUIRED";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Text = projectedNextDue.ToString("dd/MM/yyyy") + " (After Payment)";
                                txtNextDueDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                                txtPackageEndDate.Text = projectedEndDate.ToString("dd/MM/yyyy") + " (After Payment)";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                            }
                            else
                            {
                                txtPaymentStatus.Text = "PAYMENT REQUIRED";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Text = "Payment Required";
                                txtNextDueDate.Foreground = new SolidColorBrush(Colors.Red);
                                txtPackageEndDate.Text = "Payment Required";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Colors.Red);
                            }
                        }
                        else
                        {
                            txtPaymentStatus.Text = "NO PACKAGE";
                            txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Gray);
                            txtNextDueDate.Text = "N/A";
                            txtPackageEndDate.Text = "N/A";
                        }
                        
                        txtTotalPayments.Text = "0";
                        txtLastPaymentDate.Text = "N/A";
                        txtTotalAmount.Text = "Rs. 0.00";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading member history: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CheckFingerprintStatus(Models.Member member)
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var device = context.BiometricDevices.FirstOrDefault(d => d.IsConnected);
                    if (device != null)
                    {
                        using (var service = new GymManagementSystem.Services.HikvisionService())
                        {
                            var connected = await service.ConnectAsync(device.IPAddress, device.Port, device.Username, device.Password);
                            if (connected.success)
                            {
                                bool isEnrolled = await service.CheckFingerprintEnrolledAsync(member.MemberId);
                                
                                // Update UI to show fingerprint status
                                // You can add a TextBlock in XAML to display this
                                System.Diagnostics.Debug.WriteLine($"Fingerprint Status for {member.FullName}: {(isEnrolled ? "✅ Enrolled" : "❌ Not Enrolled")}");
                                
                                if (!isEnrolled && !_fingerprintMatch)
                                {
                                    // Show warning if fingerprint not enrolled
                                    MessageBox.Show(
                                        $"⚠️ Fingerprint Not Enrolled\n\n" +
                                        $"Member: {member.FullName}\n" +
                                        $"Member ID: {member.MemberId}\n\n" +
                                        $"This member's fingerprint is not enrolled on the biometric device.\n" +
                                        $"Please enroll fingerprint from Biometric page.",
                                        "Fingerprint Not Enrolled",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking fingerprint status: {ex.Message}");
            }
        }

        private static string CalculateDuration(TimeSpan checkIn, TimeSpan checkOut)
        {
            var duration = checkOut - checkIn;
            if (duration.TotalMinutes < 0)
                return "-";

            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }

            return $"{duration.Minutes}m";
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
