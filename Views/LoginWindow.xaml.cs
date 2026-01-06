using System.Windows;
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

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

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

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}
