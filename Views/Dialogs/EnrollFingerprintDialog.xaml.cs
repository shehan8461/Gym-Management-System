using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;
using GymManagementSystem.Services;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class EnrollFingerprintDialog : Window
    {
        private int _deviceId;

        public EnrollFingerprintDialog(int deviceId)
        {
            InitializeComponent();
            _deviceId = deviceId;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Load device info
                    var device = context.BiometricDevices.FirstOrDefault(d => d.DeviceId == _deviceId);
                    if (device != null)
                    {
                        txtDeviceInfo.Text = $"Device: {device.DeviceName} ({device.IPAddress}:{device.Port})";
                    }

                    // Load members
                    var members = context.Members
                        .Where(m => m.IsActive)
                        .OrderBy(m => m.FullName)
                        .Select(m => new
                        {
                            m.MemberId,
                            DisplayText = $"{m.FullName} ({m.PhoneNumber})"
                        })
                        .ToList();

                    cmbMember.ItemsSource = members;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEnroll_Click(object sender, RoutedEventArgs e)
        {
            if (cmbMember.SelectedValue == null)
            {
                MessageBox.Show("Please select a member.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int memberId = (int)cmbMember.SelectedValue;
                
                txtStatus.Text = "Connecting to device...";
                btnEnroll.IsEnabled = false;

                using (var context = new GymDbContext())
                {
                    var device = context.BiometricDevices.FirstOrDefault(d => d.DeviceId == _deviceId);
                    if (device == null)
                    {
                        MessageBox.Show("Device not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var hikvisionService = new HikvisionService();
                    
                    // Test connection first
                    if (!hikvisionService.TestConnection(device.IPAddress, device.Port, device.Username, device.Password))
                    {
                        txtStatus.Text = "Failed to connect to device!";
                        MessageBox.Show("Cannot connect to device. Please check device settings and network connectivity.", 
                            "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        btnEnroll.IsEnabled = true;
                        return;
                    }

                    txtStatus.Text = "Place finger on sensor...";

                    // Simulate fingerprint enrollment
                    // Note: Actual Hikvision SDK integration would go here
                    // This is a placeholder that demonstrates the workflow
                    
                    var result = MessageBox.Show(
                        "This feature requires the Hikvision HCNetSDK.dll library.\n\n" +
                        "The SDK integration is prepared and ready. To complete the setup:\n" +
                        "1. Download Hikvision Device Network SDK\n" +
                        "2. Copy HCNetSDK.dll to the application directory\n" +
                        "3. Implement the fingerprint capture callbacks\n\n" +
                        "Would you like to simulate a successful enrollment for testing?",
                        "SDK Required",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Simulate successful enrollment
                        var member = context.Members.FirstOrDefault(m => m.MemberId == memberId);
                        if (member != null)
                        {
                            member.BiometricDeviceId = _deviceId;
                            member.FingerprintTemplate = new byte[] { 1, 2, 3, 4, 5 }; // Placeholder
                            context.SaveChanges();

                            txtStatus.Text = "Enrollment successful! ✓";
                            MessageBox.Show("Fingerprint enrolled successfully!", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        txtStatus.Text = "Enrollment cancelled.";
                    }
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Enrollment failed!";
                MessageBox.Show($"Error during enrollment: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnEnroll.IsEnabled = true;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
