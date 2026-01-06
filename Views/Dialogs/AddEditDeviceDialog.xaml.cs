using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;
using GymManagementSystem.Models;

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
                        device = context.BiometricDevices.FirstOrDefault(d => d.DeviceId == _deviceId);
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

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
