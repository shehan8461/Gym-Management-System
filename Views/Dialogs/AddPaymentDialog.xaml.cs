using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using GymManagementSystem.Services;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class AddPaymentDialog : Window
    {
        private int _memberId;
        private Member? _member;

        public AddPaymentDialog(int memberId)
        {
            InitializeComponent();
            _memberId = memberId;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Load member info
                    _member = context.Members.FirstOrDefault(m => m.MemberId == _memberId);
                    if (_member != null)
                    {
                        txtMemberInfo.Text = $"Member: {_member.FullName}";
                        txtMemberDetails.Text = $"Phone: {_member.PhoneNumber} | NIC: {_member.NIC}";
                    }

                    // Load packages
                    var packages = context.MembershipPackages
                        .Where(p => p.IsActive)
                        .Select(p => new
                        {
                            p.PackageId,
                            DisplayText = $"{p.PackageName} - LKR {p.Price:N2} ({p.DurationMonths} month{(p.DurationMonths > 1 ? "s" : "")})",
                            p.Price,
                            p.DurationMonths
                        })
                        .ToList();

                    cmbPackage.ItemsSource = packages;
                }

                // Set default dates
                dpPaymentDate.SelectedDate = DateTime.Now.Date;
                dpStartDate.SelectedDate = DateTime.Now.Date;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbPackage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPackage.SelectedItem != null)
            {
                dynamic package = cmbPackage.SelectedItem;
                txtAmount.Text = package.Price.ToString("N2");
                CalculateEndDate();
            }
        }

        private void dpStartDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateEndDate();
        }

        private void CalculateEndDate()
        {
            if (cmbPackage.SelectedItem != null && dpStartDate.SelectedDate.HasValue)
            {
                dynamic package = cmbPackage.SelectedItem;
                int months = package.DurationMonths;
                DateTime endDate = dpStartDate.SelectedDate.Value.AddMonths(months).AddDays(-1);
                txtEndDate.Text = endDate.ToString("dd/MM/yyyy");
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
                    dynamic package = cmbPackage.SelectedItem;
                    DateTime startDate = dpStartDate.SelectedDate.Value;
                    DateTime endDate = startDate.AddMonths(package.DurationMonths).AddDays(-1);

                    var payment = new Payment
                    {
                        MemberId = _memberId,
                        PackageId = package.PackageId,
                        Amount = package.Price,
                        PaymentDate = (dpPaymentDate.SelectedDate ?? DateTime.UtcNow.Date).ToUniversalTime(),
                        StartDate = startDate.ToUniversalTime(),
                        EndDate = endDate.ToUniversalTime(),
                        NextDueDate = endDate.AddDays(1).ToUniversalTime(),
                        PaymentStatus = "Paid",
                        PaymentMethod = (cmbPaymentMethod.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Cash",
                        Remarks = txtRemarks.Text.Trim(),
                        ProcessedByUserId = SessionManager.CurrentUserId
                    };

                    context.Payments.Add(payment);
                    context.SaveChanges();

                    MessageBox.Show("Payment added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? $"\n\nInner Error: {ex.InnerException.Message}" : "";
                MessageBox.Show($"Error saving payment: {ex.Message}{innerMsg}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            if (cmbPackage.SelectedItem == null)
            {
                MessageBox.Show("Please select a membership package.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!dpStartDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select start date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
