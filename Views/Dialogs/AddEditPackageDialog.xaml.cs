using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;
using GymManagementSystem.Models;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class AddEditPackageDialog : Window
    {
        private int? _packageId;

        public AddEditPackageDialog(int? packageId = null)
        {
            InitializeComponent();
            _packageId = packageId;

            if (_packageId.HasValue)
            {
                txtTitle.Text = "Edit Membership Package";
                LoadPackageData();
            }
        }

        private void LoadPackageData()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var package = context.MembershipPackages.FirstOrDefault(p => p.PackageId == _packageId);
                    if (package != null)
                    {
                        txtPackageName.Text = package.PackageName;
                        txtDurationMonths.Text = package.DurationMonths.ToString();
                        txtPrice.Text = package.Price.ToString("F2");
                        txtDescription.Text = package.Description;
                        chkIsActive.IsChecked = package.IsActive;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading package data: {ex.Message}", 
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
                    MembershipPackage package;

                    if (_packageId.HasValue)
                    {
                        package = context.MembershipPackages.FirstOrDefault(p => p.PackageId == _packageId);
                        if (package == null)
                        {
                            MessageBox.Show("Package not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        package = new MembershipPackage();
                        context.MembershipPackages.Add(package);
                    }

                    package.PackageName = txtPackageName.Text.Trim();
                    package.DurationMonths = int.Parse(txtDurationMonths.Text.Trim());
                    package.Price = decimal.Parse(txtPrice.Text.Trim());
                    package.Description = txtDescription.Text.Trim();
                    package.IsActive = chkIsActive.IsChecked ?? true;

                    context.SaveChanges();
                    MessageBox.Show("Package saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving package: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtPackageName.Text))
            {
                MessageBox.Show("Please enter package name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(txtDurationMonths.Text, out int months) || months <= 0)
            {
                MessageBox.Show("Please enter valid duration in months.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Please enter valid price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
