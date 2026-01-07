using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class PackageMembersDialog : Window
    {
        private int _packageId;

        public PackageMembersDialog(int packageId)
        {
            InitializeComponent();
            _packageId = packageId;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Load package info
                    var package = context.MembershipPackages.FirstOrDefault(p => p.PackageId == _packageId);
                    if (package != null)
                    {
                        txtPackageName.Text = $"{package.PackageName} - Members";
                        txtPackageInfo.Text = $"Duration: {package.DurationMonths} months | Price: LKR {package.Price:N2}";
                    }

                    // Load members
                    var members = context.Members
                        .Where(m => m.AssignedPackageId == _packageId)
                        .OrderBy(m => m.FullName)
                        .ToList();

                    // Calculate payment status for each member
                    var today = DateTime.UtcNow.Date;
                    foreach (var member in members)
                    {
                        var lastPayment = context.Payments
                            .Where(p => p.MemberId == member.MemberId)
                            .OrderByDescending(p => p.PaymentDate)
                            .FirstOrDefault();

                        if (lastPayment != null)
                        {
                            var daysUntilDue = (lastPayment.NextDueDate.Date - today).Days;

                            if (daysUntilDue < 0)
                            {
                                member.PaymentStatus = "Overdue";
                            }
                            else if (daysUntilDue <= 7)
                            {
                                member.PaymentStatus = "Due Soon";
                            }
                            else
                            {
                                member.PaymentStatus = "Paid";
                            }
                        }
                        else
                        {
                            member.PaymentStatus = "No Payment";
                        }
                    }

                    dgMembers.ItemsSource = members;
                    txtMemberCount.Text = $"Total Members: {members.Count}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
