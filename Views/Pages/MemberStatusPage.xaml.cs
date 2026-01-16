using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using GymManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Pages
{
    public partial class MemberStatusPage : Page, IDisposable
    {
        private readonly HikvisionService _hikvisionService;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isListening = false;

        public MemberStatusPage()
        {
            InitializeComponent();
            _hikvisionService = new HikvisionService();
        }

        private async void btnCheckStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_isListening)
            {
                StopListening();
                return;
            }

            // Start Listening
            await StartListeningAsync();
        }

        private async Task StartListeningAsync()
        {
            _isListening = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Update UI
            btnCheckStatus.Background = Brushes.Gray;
            btnCheckStatus.BorderBrush = Brushes.Gray;
            ((StackPanel)btnCheckStatus.Content).Children.OfType<TextBlock>().First().Text = "CANCEL SCANNING";
            txtInstructions.Text = "Waiting for device event...";
            pnlScanning.Visibility = Visibility.Visible;
            pnlResult.Visibility = Visibility.Collapsed;

            // Connection Check
            if (!_hikvisionService.IsConnected)
            {
                // Get device config from DB
                Models.BiometricDevice? device = null;
                try
                {
                    using (var context = new GymDbContext())
                    {
                        // Get the first active device. In future, allow selection.
                        device = await context.BiometricDevices.FirstOrDefaultAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database error loading device config: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StopListening();
                    return;
                }

                if (device == null)
                {
                    MessageBox.Show("No biometric device configured in the system. Please add a device first.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StopListening();
                    return;
                }
                
                // Connect using DB credentials
                var connected = await _hikvisionService.ConnectAsync(device.IPAddress, device.Port, device.Username, device.Password); 
                if (!connected.success)
                {
                    MessageBox.Show($"Could not connect to device: {connected.message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StopListening();
                    return;
                }
            }

            try
            {
                // Start Loop
                await ListenForEvents(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal cancel
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error listening for events: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopListening();
            }
        }

        private async Task ListenForEvents(CancellationToken token)
        {
            var lastCheckTime = DateTime.Now.AddSeconds(-5); // Look back slightly to ensure we don't miss immediate scans
            
            while (!token.IsCancellationRequested)
            {
                // Poll events
                var events = await _hikvisionService.GetRecentEventsAsync(lastCheckTime);
                if (events != null && events.Any())
                {
                    // Update check time
                    lastCheckTime = DateTime.Now;

                    // Filter for Access Granted Log (Major: 5) or similar verification logging
                    // And ensure employeeNo is valid
                    var validEvent = events.FirstOrDefault(e => 
                        !string.IsNullOrEmpty(e.employeeNoString) && 
                        e.employeeNoString != "0" &&
                        (e.major == 5 || e.major == 1) // 5=Event, 1=Alarm? Adjust based on device
                    );

                    if (validEvent != null)
                    {
                        // Found a scan!
                        int memberId;
                        if (int.TryParse(validEvent.employeeNoString, out memberId))
                        {
                            await LoadMemberDetails(memberId);
                            StopListening(); // Stop after successful scan
                            return;
                        }
                    }
                }

                await Task.Delay(1000, token); // Poll every 1s
            }
        }

        private async Task LoadMemberDetails(int memberId)
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var member = await context.Members
                        .Include(m => m.AssignedPackage)
                        .FirstOrDefaultAsync(m => m.MemberId == memberId);

                    if (member != null)
                    {
                        // Fetch History (Attendance for now, or Payments)
                        var recentAttendance = await context.Attendances
                            .Where(a => a.MemberId == memberId)
                            .OrderByDescending(a => a.CheckInTime)
                            .Take(20)
                            .Select(a => new { CheckInTime = a.CheckInTime, Type = "Attendance" }) 
                            .ToListAsync();

                        // Update UI
                        resName.Text = member.FullName;
                        resId.Text = $"ID: {member.MemberId}";
                        resPackage.Text = member.AssignedPackage?.PackageName ?? "No Package";
                        
                        // Status logic
                        bool isActive = member.IsActive; 
                        // You might also check payment dates etc.
                        
                        if (isActive)
                        {
                            statusBadge.Background = Brushes.Green;
                            resStatus.Text = "ACTIVE";
                        }
                        else
                        {
                            statusBadge.Background = Brushes.Red;
                            resStatus.Text = "INACTIVE";
                        }

                        // Use JoinDate + Duration or Payment Valid Until
                        resExpiry.Text = member.NextDueDate?.ToString("MMM dd, yyyy") ?? "N/A";

                        // Bind History
                        dgHistory.ItemsSource = recentAttendance;

                        // Show Result
                        pnlResult.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show($"Member ID {memberId} found on device but not in database.", "Member Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading member data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopListening()
        {
            _isListening = false;
            _cancellationTokenSource?.Cancel();
            
            // Reset UI
            btnCheckStatus.Background = (Brush)new BrushConverter().ConvertFrom("#E91E63");
            btnCheckStatus.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#E91E63");
            ((StackPanel)btnCheckStatus.Content).Children.OfType<TextBlock>().First().Text = "CHECK MEMBER STATUS";
            txtInstructions.Text = "Click the button to start identification mode";
            pnlScanning.Visibility = Visibility.Collapsed;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _hikvisionService?.Dispose();
        }
    }
}
