using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;
using GymManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Pages
{
    public partial class BiometricPage : Page
    {
        public BiometricPage()
        {
            InitializeComponent();
            LoadDevices();
        }

        private void LoadDevices()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var devices = context.BiometricDevices
                        .Select(d => new
                        {
                            d.DeviceId,
                            d.DeviceName,
                            d.IPAddress,
                            d.Port,
                            d.DeviceType,
                            d.IsConnected,
                            d.LastConnectedDate,
                            StatusDisplay = d.IsConnected ? "🟢 Connected" : "🔴 Disconnected"
                        })
                        .OrderBy(d => d.DeviceName)
                        .ToList();

                    dgDevices.ItemsSource = devices;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading devices: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddDevice_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditDeviceDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadDevices();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int deviceId)
            {
                var dialog = new AddEditDeviceDialog(deviceId);
                if (dialog.ShowDialog() == true)
                {
                    LoadDevices();
                }
            }
        }

        private async void btnTest_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int deviceId)
            {
                try
                {
                    button.IsEnabled = false;
                    button.Content = "Testing...";

                    using (var context = new GymDbContext())
                    {
                        var device = context.BiometricDevices.AsTracking().FirstOrDefault(d => d.DeviceId == deviceId);
                        if (device != null)
                        {
                            using (var hikvisionService = new HikvisionService())
                            {
                                var result = await hikvisionService.ConnectAsync(device.IPAddress, device.Port, device.Username, device.Password);
                                
                                if (result.success)
                                {
                                    device.IsConnected = true;
                                    device.LastConnectedDate = DateTime.UtcNow;
                                    context.SaveChanges();
                                    
                                    MessageBox.Show("✅ Connection successful! Device is online.", 
                                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadDevices();
                                }
                                else
                                {
                                    device.IsConnected = false;
                                    context.SaveChanges();
                                    
                                    MessageBox.Show($"❌ Connection failed!\n\n{result.message}", 
                                        "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    LoadDevices();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error testing connection: {ex.Message}", "Error");
                }
                finally
                {
                    button.IsEnabled = true;
                    button.Content = "Test";
                }
            }
        }

        private async void btnSyncUsers_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int deviceId)
            {
                try
                {
                    button.IsEnabled = false;
                    var originalContent = button.Content;
                    button.Content = "Loading...";
                    
                    using (var context = new GymDbContext())
                    {
                        var device = context.BiometricDevices.Find(deviceId);
                        if (device == null)
                        {
                            MessageBox.Show("Device not found.", "Error");
                            return;
                        }

                        using (var service = new HikvisionService())
                        {
                            var result = await service.ConnectAsync(device.IPAddress, device.Port, device.Username, device.Password);
                            if (!result.success)
                            {
                                MessageBox.Show($"Failed to connect to device: {result.message}", "Connection Error");
                                return;
                            }

                            var users = await service.GetAllUsersAsync();
                            if (users == null)
                            {
                                MessageBox.Show("Failed to retrieve users from device.", "Error");
                                return;
                            }

                            string userList = users.Count > 0 
                                ? string.Join("\n", users.Select(u => $"ID: {u.EmployeeNo}, Name: {u.Name}"))
                                : "No users found on device.";
                            
                            MessageBox.Show($"Device Users ({users.Count}):\n\n{userList}", 
                                "Device Users", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error");
                }
                finally
                {
                    button.IsEnabled = true;
                    button.Content = "Sync Users";
                }
            }
        }

        private void btnEnroll_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int deviceId)
            {
                var dialog = new EnrollFingerprintDialog(deviceId, true);
                dialog.ShowDialog();
            }
        }
    }
}
