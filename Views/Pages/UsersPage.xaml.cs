using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;

namespace GymManagementSystem.Views.Pages
{
    public partial class UsersPage : Page
    {
        public UsersPage()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var users = context.Users
                        .OrderByDescending(u => u.CreatedDate)
                        .ToList();

                    dgUsers.ItemsSource = users;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddUserDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadUsers();
            }
        }
    }
}
