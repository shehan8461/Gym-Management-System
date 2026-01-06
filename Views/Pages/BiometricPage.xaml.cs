using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;
using GymManagementSystem.Services;

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

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int deviceId)
            {
                try
                {
                    using (var context = new GymDbContext())
                    {
                        var device = context.BiometricDevices.FirstOrDefault(d => d.DeviceId == deviceId);
                        if (device != null)
                        {
                            var hikvisionService = new HikvisionService();
                            if (hikvisionService.TestConnection(device.IPAddress, device.Port, device.Username, device.Password))
                            {
                                device.IsConnected = true;
                                device.LastConnectedDate = DateTime.UtcNow;
                                context.SaveChanges();
                                
                                MessageBox.Show("Connection successful! Device is online.", 
                                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadDevices();
                            }
                            else
                            {
                                device.IsConnected = false;
                                context.SaveChanges();
                                
                                MessageBox.Show("Connection failed! Please check device settings and network connectivity.", 
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                LoadDevices();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error testing connection: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnEnroll_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int deviceId)
            {
                var dialog = new EnrollFingerprintDialog(deviceId);
                dialog.ShowDialog();
            }
        }
    }
}
