using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Services;
using GymManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Pages
{
    public partial class AttendancePage : Page
    {
        private HikvisionService _hikvisionService;

        public AttendancePage()
        {
            InitializeComponent();
            _hikvisionService = new HikvisionService();
            
            // Default Date Range: Today
            dpStartDate.SelectedDate = DateTime.Today;
            dpEndDate.SelectedDate = DateTime.Today;
        }

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Please select both Start and End dates.", "Selection Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime start = dpStartDate.SelectedDate.Value;
            DateTime end = dpEndDate.SelectedDate.Value.AddDays(1).AddSeconds(-1); // End of day

            if (end < start)
            {
                MessageBox.Show("End date cannot be before Start date.", "Invalid Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // UI Loading State
            btnLoad.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
            dgAttendance.ItemsSource = null;
            txtStatus.Text = "Connecting to device and fetching logs...";

            try
            {
                // 1. Get Connection Config from DB
                Models.BiometricDevice? device = null;
                using (var context = new GymDbContext())
                {
                    device = await context.BiometricDevices.FirstOrDefaultAsync();
                }

                if (device == null)
                {
                    MessageBox.Show("No biometric device configured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetUI();
                    return;
                }

                // 2. Connect
                if (!_hikvisionService.IsConnected)
                {
                    var connected = await _hikvisionService.ConnectAsync(device.IPAddress, device.Port, device.Username, device.Password);
                    if (!connected.success)
                    {
                        MessageBox.Show($"Connection failed: {connected.message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        ResetUI();
                        return;
                    }
                }

                // 3. Fetch Logs
                var logs = await _hikvisionService.GetAttendanceLogsAsync(start, end);

                if (logs == null || !logs.Any())
                {
                    txtStatus.Text = "No records found for the selected period.";
                    ResetUI();
                    return;
                }

                // 4. Map Member Names from DB
                var mappedLogs = await MapMemberNames(logs);

                dgAttendance.ItemsSource = mappedLogs;
                txtStatus.Text = $"Loaded {mappedLogs.Count} records.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Error occurred.";
            }
            finally
            {
                ResetUI();
            }
        }

        private async Task<List<AttendanceViewModel>> MapMemberNames(List<AcsEvent> logs)
        {
            using (var context = new GymDbContext())
            {
                // Get all active member IDs/Names for fast lookup
                var members = await context.Members
                    .Select(m => new { m.MemberId, m.FullName })
                    .ToListAsync();
                
                var memberDict = members.ToDictionary(m => m.MemberId.ToString(), m => m.FullName);

                var viewModels = new List<AttendanceViewModel>();

                foreach (var log in logs)
                {
                    string empNo = log.employeeNoString ?? ""; // employeeNoString is the field in AcsEvent
                    // Note: AcsEvent def: public string? employeeNoString { get; set; }
                    // It does NOT have 'employeeNo' field according to line 923.
                    // Wait, let's check AcsEvent definition again.
                    // Line 923: public string? employeeNoString { get; set; }
                    // Line 922: public string? time { get; set; }
                    // Line 920: public int major { get; set; }
                    
                    // So 'log.employeeNo' in my previous code might be wrong if it doesn't exist.
                    // I should check if I need to add 'employeeNo' to AcsEvent or just use 'employeeNoString'.
                    // User's JSON usually returns 'employeeNoString' for events.
                    
                    string name = "Unknown / Guest";

                    if (!string.IsNullOrEmpty(empNo) && memberDict.TryGetValue(empNo, out string? dbName))
                    {
                        name = dbName;
                    }
                    else if (!string.IsNullOrEmpty(log.name))
                    {
                        name = log.name; 
                    }

                    viewModels.Add(new AttendanceViewModel
                    {
                        DateTimeString = log.GetDateTime().ToString("yyyy-MM-dd hh:mm:ss tt"),
                        MemberName = name,
                        EmployeeNo = empNo,
                        EventType = log.major == 5 && log.minor == 0 ? "Access Granted" : $"Event {log.major}-{log.minor}"
                    });
                }
                
                // Sort by latest first
                return viewModels.OrderByDescending(x => x.DateTimeString).ToList();
            }
        }

        private void ResetUI()
        {
            btnLoad.IsEnabled = true;
            progressBar.Visibility = Visibility.Collapsed;
        }
    }

    public class AttendanceViewModel
    {
        public string DateTimeString { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string EmployeeNo { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string EventTypeString => EventType; // Binding alias
    }
}
