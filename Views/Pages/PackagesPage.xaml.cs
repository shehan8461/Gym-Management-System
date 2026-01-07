using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;

namespace GymManagementSystem.Views.Pages
{
    public partial class PackagesPage : Page
    {
        public PackagesPage()
        {
            InitializeComponent();
            LoadPackages();
        }

        private void LoadPackages()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var packages = context.MembershipPackages
                        .OrderBy(p => p.DurationMonths)
                        .ToList();
                    
                    // Count members for each package
                    foreach (var package in packages)
                    {
                        package.MemberCount = context.Members
                            .Count(m => m.AssignedPackageId == package.PackageId);
                    }

                    dgPackages.ItemsSource = packages;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading packages: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddPackage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditPackageDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadPackages();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int packageId)
            {
                var dialog = new AddEditPackageDialog(packageId);
                if (dialog.ShowDialog() == true)
                {
                    LoadPackages();
                }
            }
        }
        
        private void btnViewMembers_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int packageId)
            {
                var dialog = new PackageMembersDialog(packageId);
                dialog.ShowDialog();
            }
        }
    }
}
