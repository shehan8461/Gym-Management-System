using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using GymManagementSystem.Services;
using System.IO;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class SyncUsersDialog : Window
    {
        private List<DeviceUserDisplay> _allUsers;
        private string _deviceName;

        public SyncUsersDialog(List<UserInfoResponse> deviceUsers, string deviceName)
        {
            InitializeComponent();
            _deviceName = deviceName;
            _allUsers = new List<DeviceUserDisplay>();
            
            LoadUsers(deviceUsers);
        }

        private void LoadUsers(List<UserInfoResponse> deviceUsers)
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Get all members from database
                    var members = context.Members.ToList();

                    _allUsers.Clear();

                    foreach (var user in deviceUsers)
                    {
                        var display = new DeviceUserDisplay
                        {
                            EmployeeNo = user.EmployeeNo ?? "N/A",
                            Name = user.Name ?? "Unknown",
                            UserType = user.UserType ?? "N/A",
                            Valid = user.Valid?.Enable ?? false,
                            ValidDisplay = user.Valid?.Enable == true ? "✓ Yes" : "✗ No"
                        };

                        // Try to match with member in database
                        if (int.TryParse(user.EmployeeNo, out int memberId))
                        {
                            var member = members.FirstOrDefault(m => m.MemberId == memberId);
                            if (member != null)
                            {
                                display.MappingStatus = "✓ Mapped";
                                display.MappedMemberName = member.FullName;
                            }
                            else
                            {
                                display.MappingStatus = "⚠ Not in System";
                                display.MappedMemberName = "No matching member";
                            }
                        }
                        else
                        {
                            display.MappingStatus = "⚠ Invalid ID";
                            display.MappedMemberName = "Not a valid member ID";
                        }

                        _allUsers.Add(display);
                    }
                }

                UpdateDisplay();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDisplay()
        {
            var searchTerm = txtSearch.Text.ToLower();
            
            var filtered = _allUsers.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filtered = filtered.Where(u => 
                    u.EmployeeNo.ToLower().Contains(searchTerm) ||
                    u.Name.ToLower().Contains(searchTerm));
            }

            dgUsers.ItemsSource = filtered.ToList();
        }

        private void UpdateSummary()
        {
            txtDeviceInfo.Text = $"Device: {_deviceName} | Users fetched at: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            txtSummary.Text = $"Total Users: {_allUsers.Count}";
            
            int mapped = _allUsers.Count(u => u.MappingStatus == "✓ Mapped");
            int unmapped = _allUsers.Count - mapped;
            
            txtMapped.Text = $"Mapped: {mapped}";
            txtUnmapped.Text = $"Unmapped: {unmapped}";
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDisplay();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("To refresh, close this dialog and click 'Sync Users' again.", 
                "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"DeviceUsers_{_deviceName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var writer = new StreamWriter(saveDialog.FileName))
                    {
                        // Write header
                        writer.WriteLine("Employee No,Name,User Type,Valid,Mapping Status,Mapped Member");

                        // Write data
                        foreach (var user in _allUsers)
                        {
                            writer.WriteLine($"\"{user.EmployeeNo}\",\"{user.Name}\",\"{user.UserType}\",\"{user.ValidDisplay}\",\"{user.MappingStatus}\",\"{user.MappedMemberName}\"");
                        }
                    }

                    MessageBox.Show($"Users exported successfully to:\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting users: {ex.Message}", 
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Display model for device users
    public class DeviceUserDisplay
    {
        public string EmployeeNo { get; set; } = "";
        public string Name { get; set; } = "";
        public string UserType { get; set; } = "";
        public bool Valid { get; set; }
        public string ValidDisplay { get; set; } = "";
        public string MappingStatus { get; set; } = "";
        public string MappedMemberName { get; set; } = "";
    }
}
