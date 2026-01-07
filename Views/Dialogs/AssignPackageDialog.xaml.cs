using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class AssignPackageDialog : Window
    {
        private int _memberId;
        private Member? _member;

        public AssignPackageDialog(int memberId)
        {
            InitializeComponent();
            _memberId = memberId;
            LoadData();
            
            cmbPackage.SelectionChanged += CmbPackage_SelectionChanged;
        }

        private void LoadData()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Load member
                    _member = context.Members.FirstOrDefault(m => m.MemberId == _memberId);
                    if (_member == null)
                    {
                        MessageBox.Show("Member not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    txtMemberInfo.Text = $"Member: {_member.FullName}";
                    
                    // Load current package
                    if (_member.AssignedPackageId.HasValue)
                    {
                        var currentPackage = context.MembershipPackages
                            .FirstOrDefault(p => p.PackageId == _member.AssignedPackageId.Value);
                        txtCurrentPackage.Text = $"Current Package: {currentPackage?.PackageName ?? "None"}";
                    }
                    else
                    {
                        txtCurrentPackage.Text = "Current Package: None";
                    }

                    // Load all packages
                    var packages = context.MembershipPackages
                        .Where(p => p.IsActive)
                        .OrderBy(p => p.PackageName)
                        .ToList();
                    cmbPackage.ItemsSource = packages;
                    
                    // Select current package if exists
                    if (_member.AssignedPackageId.HasValue)
                    {
                        cmbPackage.SelectedValue = _member.AssignedPackageId.Value;
                        
                        // Load custom amount if set
                        if (_member.CustomPackageAmount.HasValue)
                        {
                            txtCustomAmount.Text = _member.CustomPackageAmount.Value.ToString("N2");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbPackage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbPackage.SelectedItem is MembershipPackage package)
            {
                packageDetails.Visibility = Visibility.Visible;
                txtPackagePrice.Text = $"${package.Price:F2}";
                txtPackageDuration.Text = $"{package.DurationMonths} months";
                txtPackageDescription.Text = package.Description ?? "No description available";
            }
            else
            {
                packageDetails.Visibility = Visibility.Collapsed;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbPackage.SelectedValue == null)
                {
                    MessageBox.Show("Please select a package!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var packageId = (int)cmbPackage.SelectedValue;
                
                // Parse custom amount if provided
                decimal? customAmount = null;
                if (!string.IsNullOrWhiteSpace(txtCustomAmount.Text))
                {
                    if (decimal.TryParse(txtCustomAmount.Text.Replace(",", ""), out decimal amount))
                    {
                        customAmount = amount;
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid amount or leave empty for default price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                using (var context = new GymDbContext())
                {
                    // Use AsTracking() to enable change tracking for update operations
                    var member = context.Members.AsTracking().FirstOrDefault(m => m.MemberId == _memberId);
                    if (member != null)
                    {
                        member.AssignedPackageId = packageId;
                        member.CustomPackageAmount = customAmount;
                        context.SaveChanges();
                        
                        var message = customAmount.HasValue 
                            ? $"Package assigned successfully with custom amount: LKR {customAmount.Value:N2}!" 
                            : "Package assigned successfully with default price!";
                        
                        MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.DialogResult = true;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error assigning package: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtCustomAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numbers and decimal point
            Regex regex = new Regex(@"^[0-9.]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
