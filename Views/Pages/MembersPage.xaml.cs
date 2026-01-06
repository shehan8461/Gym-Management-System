using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;

namespace GymManagementSystem.Views.Pages
{
    public partial class MembersPage : Page
    {
        public MembersPage()
        {
            InitializeComponent();
            LoadMembers();
        }

        private void LoadMembers(string searchTerm = "")
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var query = context.Members.AsQueryable();

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query = query.Where(m => 
                            m.FullName.Contains(searchTerm) || 
                            m.PhoneNumber.Contains(searchTerm) || 
                            m.NIC.Contains(searchTerm));
                    }

                    var members = query.OrderByDescending(m => m.RegistrationDate).ToList();
                    dgMembers.ItemsSource = members;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddMember_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditMemberDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadMembers();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new AddEditMemberDialog(memberId);
                if (dialog.ShowDialog() == true)
                {
                    LoadMembers();
                }
            }
        }

        private void btnPayment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new AddPaymentDialog(memberId);
                if (dialog.ShowDialog() == true)
                {
                    LoadMembers();
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadMembers();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadMembers(txtSearch.Text.Trim());
        }

        private void dgMembers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgMembers.SelectedItem != null)
            {
                var member = dgMembers.SelectedItem as dynamic;
                if (member != null)
                {
                    var dialog = new AddEditMemberDialog(member.MemberId);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadMembers();
                    }
                }
            }
        }
    }
}
