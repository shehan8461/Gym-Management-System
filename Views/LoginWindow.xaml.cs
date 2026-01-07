using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using GymManagementSystem.Services;

namespace GymManagementSystem.Views
{
    public partial class LoginWindow : Window
    {
        private AuthenticationService _authService;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = new AuthenticationService();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            // Hide error message
            HideError();

            // Show loading state
            SetLoadingState(true);

            try
            {
                // Simulate async authentication with delay for better UX
                await Task.Delay(1000);

                if (_authService.ValidateUser(username, password, out var user))
                {
                    if (user != null)
                    {
                        // Set session
                        SessionManager.Login(user.UserId, user.Username, user.Role, user.FullName);

                        // Open main window
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close();
                    }
                }
                else
                {
                    ShowError("Invalid username or password. Please try again.");
                }
            }
            catch (Exception ex)
            {
                ShowError("An error occurred during login. Please try again.");
            }
            finally
            {
                // Hide loading state
                SetLoadingState(false);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            if (isLoading)
            {
                btnLogin.Visibility = Visibility.Collapsed;
                loadingState.Visibility = Visibility.Visible;
                txtUsername.IsEnabled = false;
                txtPassword.IsEnabled = false;
            }
            else
            {
                btnLogin.Visibility = Visibility.Visible;
                loadingState.Visibility = Visibility.Collapsed;
                txtUsername.IsEnabled = true;
                txtPassword.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
            errorBorder.Visibility = Visibility.Visible;
            
            // Animate error appearance
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            errorBorder.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void HideError()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            
            fadeOut.Completed += (s, e) =>
            {
                errorBorder.Visibility = Visibility.Collapsed;
            };
            
            errorBorder.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
