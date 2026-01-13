using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GymManagementSystem.Data;
using GymManagementSystem.Services;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class EnrollFingerprintDialog : Window
    {
        private int _deviceId;
        private int? _preselectedMemberId;
        private CancellationTokenSource? _pollingCts;
        private HikvisionService? _hikvisionService;

        // Constructor for when called from Members page with specific member
        public EnrollFingerprintDialog(int memberId)
        {
            InitializeComponent();
            _preselectedMemberId = memberId;
            LoadData();
        }

        // Constructor for when called from Biometric page with device selection
        public EnrollFingerprintDialog(int deviceId, bool isDeviceId)
        {
            InitializeComponent();
            _deviceId = deviceId;
            LoadData();
        }
        
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            _pollingCts?.Cancel();
            _hikvisionService?.Dispose();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // If device ID not set, get the first connected device
                    if (_deviceId == 0)
                    {
                        var firstDevice = context.BiometricDevices
                            .Where(d => d.IsActive)
                            .OrderBy(d => d.DeviceId)
                            .FirstOrDefault();
                        
                        if (firstDevice != null)
                        {
                            _deviceId = firstDevice.DeviceId;
                        }
                    }

                    // Load device info
                    var device = context.BiometricDevices.FirstOrDefault(d => d.DeviceId == _deviceId);
                    if (device != null)
                    {
                        txtDeviceInfo.Text = $"Device: {device.DeviceName} ({device.IPAddress}:{device.Port})";
                    }
                    else
                    {
                        txtDeviceInfo.Text = "No device found. Please configure a biometric device first.";
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

                    // If a specific member was preselected, select it
                    if (_preselectedMemberId.HasValue)
                    {
                        cmbMember.SelectedValue = _preselectedMemberId.Value;
                        txtStatus.Text = $"Ready to enroll fingerprint for selected member...";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnEnroll_Click(object sender, RoutedEventArgs e)
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
                btnClose.IsEnabled = false;

                using (var context = new GymDbContext())
                {
                    var device = context.BiometricDevices.FirstOrDefault(d => d.DeviceId == _deviceId);
                    if (device == null)
                    {
                        MessageBox.Show("Device not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        await SaveEnrollmentHistory(memberId, _deviceId, false, "Device Not Found", "Biometric device record not found in database.");
                        return;
                    }

                    var member = context.Members.FirstOrDefault(m => m.MemberId == memberId);
                    if (member == null)
                    {
                        MessageBox.Show("Member not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        await SaveEnrollmentHistory(memberId, _deviceId, false, "Member Not Found", "Member record not found in database.");
                        return;
                    }

                    _hikvisionService = new HikvisionService();
                    
                    // Test connection first
                    txtStatus.Text = "Connecting to device...";
                    var connectionResult = await _hikvisionService.ConnectAsync(device.IPAddress, device.Port, device.Username, device.Password);
                    if (!connectionResult.success)
                    {
                        txtStatus.Text = "Failed to connect to device!";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                        MessageBox.Show("Cannot connect to device. Please check device settings and network connectivity.", 
                            "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        await SaveEnrollmentHistory(memberId, _deviceId, false, "Connection Failed", connectionResult.message);
                        btnEnroll.IsEnabled = true;
                        btnClose.IsEnabled = true;
                        return;
                    }

                    // Step 1: Create user record on device first
                    txtStatus.Text = $"Creating user on device (Employee ID: {memberId})...";
                    bool userCreated = false;
                    string? errorMessage = null;
                    
                    try
                    {
                        userCreated = await _hikvisionService.EnrollMemberAsync(memberId, member.FullName);
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.Message;
                        Debug.WriteLine($"EnrollMemberAsync exception: {ex}");
                        userCreated = false;
                    }
                    
                    if (!userCreated)
                    {
                        txtStatus.Text = "❌ Failed to create user on device";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                        
                        var errorDetails = errorMessage != null ? $"\n\nError details: {errorMessage}" : "";
                        var retry = MessageBox.Show(
                            $"Failed to create user on device.{errorDetails}\n\n" +
                            $"Member: {member.FullName}\n" +
                            $"Employee ID: {memberId}\n\n" +
                            "This ID is needed to find the user on device.\n\n" +
                            "Would you like to:\n" +
                            "• Check if user already exists on device?\n" +
                            "• See device user list?\n\n" +
                            "Check the Output window (View → Output) for detailed error messages.",
                            "User Creation Failed",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Error);
                        
                        if (retry == MessageBoxResult.Yes)
                        {
                            await ShowDeviceUsers();
                        }
                        
                        await SaveEnrollmentHistory(memberId, _deviceId, false, "User Create Failed", errorDetails ?? "Failed to create user on device.");
                        btnEnroll.IsEnabled = true;
                        btnClose.IsEnabled = true;
                        return;
                    }
                    else
                    {
                        txtStatus.Text = $"✅ User created! Look for Employee ID: {memberId}";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                        await Task.Delay(1500); // Give time to read the message
                    }

                    // Small delay to ensure device is ready
                    await Task.Delay(1000);

                    // Small delay to ensure device is ready
                    await Task.Delay(1000);

                    // Step 2: Use CompleteEnrollmentAsync which handles Capture -> Save (via Re-creation)
                    txtStatus.Text = "Initiating fingerprint capture...";
                    
                    // Note: CompleteEnrollmentAsync now handles user creation/verification internally too
                    // But we already created user above to ensure ID exists. That's fine.
                    
                    var enrollmentResult = await _hikvisionService.CompleteEnrollmentAsync(memberId, member.FullName);
                    
                    if (!enrollmentResult.success)
                    {
                        txtStatus.Text = "Enrollment Failed!";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                        MessageBox.Show(enrollmentResult.message, "Device Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        await SaveEnrollmentHistory(memberId, _deviceId, false, "Capture Failed", enrollmentResult.message);
                        btnEnroll.IsEnabled = true;
                        btnClose.IsEnabled = true;
                        return;
                    }

                    // Since CompleteEnrollmentAsync now waits for capture and saves it, 
                    // we don't strictly need to poll if it returns success. 
                    // However, we can still verify.
                    
                    txtStatus.Text = "✅ Fingerprint enrolled successfully!";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    MessageBox.Show(enrollmentResult.message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await SaveEnrollmentHistory(memberId, _deviceId, true, "Success", "Fingerprint enrolled successfully on device.");
                    
                    // Wait a moment then close
                    await Task.Delay(1000);
                    DialogResult = true;
                    Close();
                    
                    /* POLLING IS NO LONGER NEEDED IF WE USE THE SWAP METHOD */
                    /*
                    // Step 3: Show instructions and start polling
                    txtStatus.Text = "Please place your finger on the sensor now...";
                    // ... polling logic ...
                    */
                    return;
                    

                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Enrollment failed!";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Error during enrollment: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnEnroll.IsEnabled = true;
                btnClose.IsEnabled = true;
                _hikvisionService?.Dispose();
            }
        }

        private async Task<bool> PollForEnrollmentCompletion(int memberId, string memberName, CancellationToken cancellationToken)
        {
            try
            {
                // Poll for up to 60 seconds
                var startTime = DateTime.Now;
                var timeout = TimeSpan.FromSeconds(60);
                var pollInterval = TimeSpan.FromSeconds(2);
                int pollCount = 0;

                while (DateTime.Now - startTime < timeout && !cancellationToken.IsCancellationRequested)
                {
                    pollCount++;
                    await Task.Delay(pollInterval, cancellationToken);

                    // Method 1: Direct check for fingerprint enrollment
                    bool isEnrolled = await _hikvisionService.CheckFingerprintEnrolledAsync(memberId);
                    if (isEnrolled)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Fingerprint detected for member {memberId} (method 1)");
                        return true;
                    }

                    // Method 2: Check user list for changes
                    var currentUsers = await _hikvisionService.GetAllUsersAsync();
                    if (currentUsers != null)
                    {
                        var enrolledUser = currentUsers.FirstOrDefault(u => 
                            u.EmployeeNo == memberId.ToString() && u.Valid?.Enable == true);

                        if (enrolledUser != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ User {memberId} found in user list with Valid=true (method 2)");
                            return true;
                        }
                    }

                    // Method 3: Check for recent access control events
                    // (Some devices log enrollment attempts as events)
                    if (pollCount % 3 == 0) // Check events less frequently
                    {
                        var recentEvents = await _hikvisionService.GetRecentEventsAsync(startTime.AddSeconds(-10));
                        if (recentEvents != null && recentEvents.Any())
                        {
                            var matchingEvent = recentEvents.FirstOrDefault(e => 
                                e.employeeNoString == memberId.ToString() || 
                                (e.name != null && e.name.Contains(memberName)));
                                
                            if (matchingEvent != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"✅ Event detected for member {memberId}: {matchingEvent.name} (method 3)");
                                // Give a bit more time for enrollment to complete
                                await Task.Delay(2000, cancellationToken);
                                
                                // Verify enrollment is complete
                                bool finalCheck = await _hikvisionService.CheckFingerprintEnrolledAsync(memberId);
                                if (finalCheck)
                                {
                                    return true;
                                }
                            }
                        }
                    }

                    // Update UI with countdown and attempt info
                    var elapsed = (int)(DateTime.Now - startTime).TotalSeconds;
                    Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = $"Waiting for fingerprint... ({elapsed}/60 seconds) - Poll #{pollCount}";
                    });
                }

                System.Diagnostics.Debug.WriteLine($"⏱️ Enrollment timeout after {pollCount} polls");
                return false; // Timeout
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Polling cancelled by user");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Polling error: {ex.Message}");
                return false;
            }
        }

        private async Task VerifyManualEnrollment(int memberId, string memberName)
        {
            try
            {
                txtStatus.Text = "Verifying enrollment...";
                txtStatus.Foreground = System.Windows.Media.Brushes.Blue;
                
                // Check if fingerprint is enrolled
                bool isEnrolled = await _hikvisionService.CheckFingerprintEnrolledAsync(memberId);
                
                if (isEnrolled)
                {
                    txtStatus.Text = "✅ Fingerprint verified successfully!";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    MessageBox.Show($"Great! Fingerprint found for {memberName}.\n\n" +
                        "The member can now use fingerprint for attendance.\n" +
                        "Test it by scanning on the device - attendance will be recorded automatically!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    await SaveEnrollmentHistory(memberId, _deviceId, true, "Manual Enroll Verified", "Fingerprint found on device after manual enrollment.");
                    
                    await Task.Delay(1000);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    txtStatus.Text = "⚠️ Fingerprint not found";
                    txtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    MessageBox.Show("Fingerprint not found on device yet.\n\n" +
                        "Please complete the enrollment on the device screen, then click VERIFY again.",
                        "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await SaveEnrollmentHistory(memberId, _deviceId, false, "Manual Enroll Not Found", "Fingerprint not found on device after manual verification.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verifying enrollment: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async Task SaveEnrollmentHistory(int memberId, int deviceId, bool isSuccess, string status, string? message)
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var entry = new Models.FingerprintEnrollmentHistory
                    {
                        MemberId = memberId,
                        DeviceId = deviceId,
                        EnrollmentTimeUtc = DateTime.UtcNow,
                        IsSuccess = isSuccess,
                        Status = status,
                        Message = message
                    };

                    context.FingerprintEnrollmentHistories.Add(entry);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save FingerprintEnrollmentHistory: {ex.Message}");
            }
        }
        
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            // Cancel any ongoing polling
            _pollingCts?.Cancel();
            _hikvisionService?.Dispose();
            Close();
        }
        
        private async Task ShowDeviceUsers()
        {
            try
            {
                txtStatus.Text = "Loading users from device...";
                txtStatus.Foreground = System.Windows.Media.Brushes.Blue;
                
                List<UserInfoResponse> users;
                try
                {
                    var usersResult = await _hikvisionService.GetAllUsersAsync();
                    if (usersResult == null)
                    {
                        MessageBox.Show("Error retrieving users from device.\n\n" +
                            "The device returned null response.\n\n" +
                            "Possible issues:\n" +
                            "• Device is not responding\n" +
                            "• Network connection lost\n" +
                            "• Invalid API endpoint\n\n" +
                            "Check the Output window (View → Output) for detailed error messages.",
                            "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtStatus.Text = "Failed to load users";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                        return;
                    }
                    users = usersResult;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error retrieving users from device.\n\n" +
                        $"Error: {ex.Message}\n\n" +
                        "Possible issues:\n" +
                        "• Device is not responding\n" +
                        "• Network connection lost\n" +
                        "• Invalid credentials (username/password)\n" +
                        "• Invalid API endpoint\n\n" +
                        "Check the Output window (View → Output) for detailed error messages.",
                        "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Failed to load users";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }
                
                if (users.Count == 0)
                {
                    var result = MessageBox.Show(
                        "No users found on device.\n\n" +
                        "This could mean:\n" +
                        "• The device is empty (no users enrolled yet)\n" +
                        "• User creation is failing silently\n" +
                        "• The API endpoint is not returning data\n\n" +
                        "Would you like to try creating a test user now?\n\n" +
                        "Click YES to create a test user with Employee ID 9999\n" +
                        "Click NO to cancel",
                        "No Users Found", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Warning);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // Try creating a test user
                        txtStatus.Text = "Creating test user...";
                        bool created = await _hikvisionService.EnrollMemberAsync(9999, "Test User");
                        
                        if (created)
                        {
                            MessageBox.Show("Test user created successfully!\n\n" +
                                "Click SHOW DEVICE USERS again to verify it appears.",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            txtStatus.Text = "Test user created - click button again to verify";
                        }
                        else
                        {
                            MessageBox.Show("Failed to create test user.\n\n" +
                                "Check:\n" +
                                "• Device connection is active\n" +
                                "• Device credentials are correct\n" +
                                "• ISAPI is enabled on device\n" +
                                "• Device has storage space\n\n" +
                                "See Output window for detailed errors.",
                                "Creation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            txtStatus.Text = "Test user creation failed";
                            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                        }
                    }
                    return;
                }
                
                // Build user list message
                var userList = new System.Text.StringBuilder();
                userList.AppendLine($"✅ Found {users.Count} user{(users.Count == 1 ? "" : "s")} on device:\n");
                userList.AppendLine("Employee ID | Name");
                userList.AppendLine("------------|----------------");
                
                foreach (var user in users.OrderBy(u => int.TryParse(u.EmployeeNo, out int id) ? id : 999999))
                {
                    userList.AppendLine($"{user.EmployeeNo,-12}| {user.Name}");
                }
                
                userList.AppendLine($"\n💡 Tip: Your Member ID should match an Employee ID above");
                userList.AppendLine($"📌 To enroll: Go to device → Menu → User Management → Search by Employee ID");
                
                MessageBox.Show(userList.ToString(), "Device User List", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                txtStatus.Text = $"User list loaded - {users.Count} users found";
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading device users: {ex.Message}\n\n" +
                    "Check the Output window (View → Output) for detailed error information.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Error loading users";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        
        private async void btnCheckDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var device = context.BiometricDevices.FirstOrDefault(d => d.DeviceId == _deviceId);
                    if (device == null)
                    {
                        MessageBox.Show("Device not found.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    if (_hikvisionService == null || !_hikvisionService.IsConnected)
                    {
                        _hikvisionService = new HikvisionService();
                        txtStatus.Text = "Connecting to device...";
                        await _hikvisionService.ConnectAsync(device.IPAddress, device.Port, device.Username, device.Password);
                    }
                    
                    await ShowDeviceUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            if (cmbMember.SelectedValue == null)
            {
                MessageBox.Show("Please select a member first.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                int memberId = (int)cmbMember.SelectedValue;
                using (var context = new GymDbContext())
                {
                    var member = context.Members.FirstOrDefault(m => m.MemberId == memberId);
                    if (member == null) return;
                    
                    var device = context.BiometricDevices.FirstOrDefault(d => d.DeviceId == _deviceId);
                    if (device == null) return;
                    
                    btnVerify.IsEnabled = false;
                    btnEnroll.IsEnabled = false;
                    
                    if (_hikvisionService == null || !_hikvisionService.IsConnected)
                    {
                        _hikvisionService = new HikvisionService();
                        txtStatus.Text = "Connecting to device...";
                        await _hikvisionService.ConnectAsync(device.IPAddress, device.Port, device.Username, device.Password);
                    }
                    
                    await VerifyManualEnrollment(memberId, member.FullName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnVerify.IsEnabled = true;
                btnEnroll.IsEnabled = true;
            }
        }
    }
}
