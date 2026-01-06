using System.Windows;
using GymManagementSystem.Services;
using GymManagementSystem.Views.Pages;

namespace GymManagementSystem.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Set welcome message
            txtWelcome.Text = $"Welcome, {SessionManager.CurrentUserFullName} ({SessionManager.CurrentUserRole})";
            
            // Load Dashboard by default
            MainFrame.Navigate(new DashboardPage());
        }

        private void btnDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DashboardPage());
        }

        private void btnMembers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MembersPage());
        }

        private void btnPayments_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PaymentsPage());
        }

        private void btnAttendance_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AttendancePage());
        }

        private void btnPackages_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PackagesPage());
        }

        private void btnBiometric_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BiometricPage());
        }

        private void btnUsers_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UsersPage());
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", 
                                        "Confirm Logout", 
                                        MessageBoxButton.YesNo, 
                                        MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                SessionManager.Logout();
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
