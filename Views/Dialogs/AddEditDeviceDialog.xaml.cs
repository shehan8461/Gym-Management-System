using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using GymManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class AddEditDeviceDialog : Window
    {
        private int? _deviceId;

        public AddEditDeviceDialog(int? deviceId = null)
        {
            InitializeComponent();
            _deviceId = deviceId;

            if (_deviceId.HasValue)
            {
                txtTitle.Text = "Edit Biometric Device";
                LoadDeviceData();
            }
        }

        private void LoadDeviceData()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var device = context.BiometricDevices.FirstOrDefault(d => d.DeviceId == _deviceId);
                    if (device != null)
                    {
                        txtDeviceName.Text = device.DeviceName;
                        txtIPAddress.Text = device.IPAddress;
                        txtPort.Text = device.Port.ToString();
                        txtUsername.Text = device.Username;
                        txtPassword.Password = device.Password;
                        cmbDeviceType.Text = device.DeviceType;
                        chkIsActive.IsChecked = device.IsActive;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading device data: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            try
            {
                using (var context = new GymDbContext())
                {
                    BiometricDevice device;

                    if (_deviceId.HasValue)
                    {
                        // Use AsTracking() to enable change tracking for update operations
                        device = context.BiometricDevices.AsTracking().FirstOrDefault(d => d.DeviceId == _deviceId);
                        if (device == null)
                        {
                            MessageBox.Show("Device not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        device = new BiometricDevice();
                        context.BiometricDevices.Add(device);
                    }

                    device.DeviceName = txtDeviceName.Text.Trim();
                    device.IPAddress = txtIPAddress.Text.Trim();
                    device.Port = int.Parse(txtPort.Text.Trim());
                    device.Username = txtUsername.Text.Trim();
                    device.Password = txtPassword.Password;
                    device.DeviceType = (cmbDeviceType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Hikvision";
                    device.IsActive = chkIsActive.IsChecked ?? true;

                    context.SaveChanges();
                    MessageBox.Show("Device saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving device: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
            {
                MessageBox.Show("Please enter device name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtIPAddress.Text))
            {
                MessageBox.Show("Please enter IP address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Please enter valid port number (1-65535).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Please enter username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Please enter password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private async void btnTest_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields first
            if (string.IsNullOrWhiteSpace(txtIPAddress.Text))
            {
                txtTestStatus.Text = "❌ Please enter IP address first";
                txtTestStatus.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
            {
                txtTestStatus.Text = "❌ Please enter valid port number first";
                txtTestStatus.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                txtTestStatus.Text = "❌ Please enter username first";
                txtTestStatus.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                txtTestStatus.Text = "❌ Please enter password first";
                txtTestStatus.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            try
            {
                btnTest.IsEnabled = false;
                btnTest.Content = "TESTING...";
                txtTestStatus.Text = "🔄 Testing connection...";
                txtTestStatus.Foreground = System.Windows.Media.Brushes.Blue;

                var hikvisionService = new HikvisionService();
                var result = await hikvisionService.ConnectAsync(
                    txtIPAddress.Text.Trim(), 
                    port, 
                    txtUsername.Text.Trim(), 
                    txtPassword.Password);

                if (result.success)
                {
                    // Double-check credentials are actually valid
                    var validation = await hikvisionService.ValidateCredentialsAsync();
                    if (validation.isValid)
                    {
                        txtTestStatus.Text = result.message + "\n\n" + validation.message;
                        txtTestStatus.Foreground = System.Windows.Media.Brushes.Green;
                    }
                    else
                    {
                        txtTestStatus.Text = result.message + "\n\n" + validation.message + 
                            "\n\n⚠️ Connection works but credentials are incorrect!\n" +
                            "Please update username/password to match Hikvision iVMS-4200.";
                        txtTestStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    }
                }
                else
                {
                    txtTestStatus.Text = result.message;
                    txtTestStatus.Foreground = System.Windows.Media.Brushes.Red;
                }

                hikvisionService.Dispose();
            }
            catch (Exception ex)
            {
                txtTestStatus.Text = $"❌ Error: {ex.Message}";
                txtTestStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                btnTest.IsEnabled = true;
                btnTest.Content = "TEST CONNECTION";
            }
        }

        private async void btnScan_Click(object sender, RoutedEventArgs e)
        {
            // Validate IP address field first
            if (string.IsNullOrWhiteSpace(txtIPAddress.Text))
            {
                MessageBox.Show("Please enter an IP address to scan.", "Input Required", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                btnScan.IsEnabled = false;
                btnScan.Content = "SCANNING...";
                
                var hikvisionService = new HikvisionService();
                var scanResults = await hikvisionService.ScanDevicePortsDetailedAsync(txtIPAddress.Text.Trim());

                // Display scan results in a message box
                string resultsText = $"Device Scan Results for {txtIPAddress.Text.Trim()}:\n\n";
                
                if (scanResults.Any(r => r.IsSuccessful))
                {
                    resultsText += "✅ SUCCESSFUL CONNECTIONS:\n";
                    foreach (var result in scanResults.Where(r => r.IsSuccessful))
                    {
                        resultsText += $"   • Port {result.Port}: {result.Message}\n";
                    }
                    resultsText += "\n";
                }

                if (scanResults.Any(r => !r.IsSuccessful))
                {
                    resultsText += "❌ FAILED CONNECTIONS:\n";
                    foreach (var result in scanResults.Where(r => !r.IsSuccessful))
                    {
                        resultsText += $"   • Port {result.Port}: {result.Message}\n";
                    }
                }

                // If we found successful ports, ask if user wants to use one
                var successfulPorts = scanResults.Where(r => r.IsSuccessful).ToList();
                if (successfulPorts.Any())
                {
                    resultsText += $"\n🔧 RECOMMENDATION:\n";
                    
                    // Prioritize: 1) HIKVISION detected ports, 2) Port 8000 (Hikvision standard), 3) Port 80 (web interface), 4) First successful port
                    var bestPort = successfulPorts.FirstOrDefault(p => p.IsHikvisionDetected) ??
                                   successfulPorts.FirstOrDefault(p => p.Port == 8000) ??
                                   successfulPorts.FirstOrDefault(p => p.Port == 80) ??
                                   successfulPorts.First();
                    
                    if (bestPort.IsHikvisionDetected)
                    {
                        resultsText += $"🎯 Port {bestPort.Port} is recommended (HIKVISION detected).\n\n";
                    }
                    else if (bestPort.Port == 8000)
                    {
                        resultsText += $"🎯 Port {bestPort.Port} is recommended (Hikvision standard ISAPI port).\n\n";
                    }
                    else if (bestPort.Port == 80)
                    {
                        resultsText += $"🌐 Port {bestPort.Port} is recommended (standard web interface).\n\n";
                    }
                    else
                    {
                        resultsText += $"✅ Port {bestPort.Port} is recommended (first available port).\n\n";
                    }
                    
                    resultsText += "Would you like to update the port field with the recommended port?";

                    var dialogResult = MessageBox.Show(resultsText, "Device Scan Results", 
                        MessageBoxButton.YesNo, MessageBoxImage.Information);
                    
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        txtPort.Text = bestPort.Port.ToString();
                        // Also update the test status to show the port was updated
                        txtTestStatus.Text = $"✅ Port updated to {bestPort.Port}. Click 'Test Connection' to verify.";
                        txtTestStatus.Foreground = System.Windows.Media.Brushes.Green;
                    }
                }
                else
                {
                    resultsText += "\n💡 TROUBLESHOOTING TIPS:\n";
                    resultsText += "• Verify the device is powered on and connected to network\n";
                    resultsText += "• Check if the IP address is correct\n";
                    resultsText += "• Ensure your computer and device are on the same network\n";
                    resultsText += "• Try pinging the device: ping " + txtIPAddress.Text.Trim();

                    MessageBox.Show(resultsText, "Device Scan Results", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                hikvisionService.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during device scan: {ex.Message}", "Scan Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnScan.IsEnabled = true;
                btnScan.Content = "SCAN DEVICE";
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
