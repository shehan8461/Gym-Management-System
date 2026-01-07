using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using GymManagementSystem.Data;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class MemberHistoryDialog : Window
    {
        private readonly int _memberId;

        public MemberHistoryDialog(int memberId)
        {
            InitializeComponent();
            _memberId = memberId;
            LoadMemberHistory();
        }

        private void LoadMemberHistory()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Load member details
                    var member = context.Members.FirstOrDefault(m => m.MemberId == _memberId);
                    
                    if (member == null)
                    {
                        MessageBox.Show("Member not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    // Display member info
                    txtMemberName.Text = member.FullName;
                    txtMemberId.Text = member.MemberId.ToString();
                    txtPhone.Text = member.PhoneNumber;
                    txtRegistrationDate.Text = member.RegistrationDate.ToString("dd/MM/yyyy");

                    // Load current package info
                    if (member.AssignedPackageId.HasValue)
                    {
                        var package = context.MembershipPackages
                            .FirstOrDefault(p => p.PackageId == member.AssignedPackageId.Value);
                        txtCurrentPackage.Text = package?.PackageName ?? "Unknown";
                    }
                    else
                    {
                        txtCurrentPackage.Text = "No Package";
                    }

                    // Load payment history with package details
                    var payments = context.Payments
                        .Include(p => p.MembershipPackage)
                        .Where(p => p.MemberId == _memberId)
                        .OrderByDescending(p => p.PaymentDate)
                        .ToList();

                    dgPaymentHistory.ItemsSource = payments;

                    // Calculate and display summary
                    var today = DateTime.UtcNow.Date;
                    
                    if (payments.Any())
                    {
                        var lastPayment = payments.First();
                        var totalAmount = payments.Sum(p => p.Amount);

                        txtTotalPayments.Text = payments.Count.ToString();
                        txtLastPaymentDate.Text = lastPayment.PaymentDate.ToString("dd/MM/yyyy");
                        txtTotalAmount.Text = $"Rs. {totalAmount:N2}";

                        // Check if the last payment is for the current assigned package
                        bool packageChanged = member.AssignedPackageId.HasValue && 
                                            lastPayment.PackageId != member.AssignedPackageId.Value;

                        if (packageChanged)
                        {
                            // Package changed but no payment yet for new package
                            // Calculate projected dates based on new package
                            var newPackage = context.MembershipPackages
                                .FirstOrDefault(p => p.PackageId == member.AssignedPackageId.Value);
                            
                            if (newPackage != null)
                            {
                                var projectedEndDate = today.AddMonths(newPackage.DurationMonths);
                                var projectedNextDue = today.AddMonths(newPackage.DurationMonths);
                                
                                txtPaymentStatus.Text = "PAYMENT REQUIRED";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Text = projectedNextDue.ToString("dd/MM/yyyy") + " (After Payment)";
                                txtNextDueDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                                txtPackageEndDate.Text = projectedEndDate.ToString("dd/MM/yyyy") + " (After Payment)";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                            }
                            else
                            {
                                txtPaymentStatus.Text = "PAYMENT REQUIRED";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Text = "Payment Required";
                                txtNextDueDate.Foreground = new SolidColorBrush(Colors.Red);
                                txtPackageEndDate.Text = "Payment Required";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Colors.Red);
                            }
                        }
                        else
                        {
                            // Display next due date
                            txtNextDueDate.Text = lastPayment.NextDueDate.ToString("dd/MM/yyyy");

                            // Display package end date
                            txtPackageEndDate.Text = lastPayment.EndDate.ToString("dd/MM/yyyy");

                            // Calculate payment status with color coding
                            var daysUntilDue = (lastPayment.NextDueDate.Date - today).Days;
                            var daysUntilEnd = (lastPayment.EndDate.Date - today).Days;

                            if (daysUntilDue < 0)
                            {
                                txtPaymentStatus.Text = "OVERDUE";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Foreground = new SolidColorBrush(Colors.Red);
                            }
                            else if (daysUntilDue <= 7)
                            {
                                txtPaymentStatus.Text = "DUE SOON";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                                txtNextDueDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                            }
                            else
                            {
                                txtPaymentStatus.Text = "PAID";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Green);
                                txtNextDueDate.Foreground = new SolidColorBrush(Colors.Green);
                            }

                            // Package end date color
                            if (daysUntilEnd < 0)
                            {
                                txtPackageEndDate.Text += " (EXPIRED)";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Colors.Red);
                            }
                            else if (daysUntilEnd <= 7)
                            {
                                txtPackageEndDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                            }
                            else
                            {
                                txtPackageEndDate.Foreground = new SolidColorBrush(Colors.Green);
                            }
                        }
                    }
                    else
                    {
                        // No payments at all
                        if (member.AssignedPackageId.HasValue)
                        {
                            // Calculate projected dates based on assigned package
                            var assignedPackage = context.MembershipPackages
                                .FirstOrDefault(p => p.PackageId == member.AssignedPackageId.Value);
                            
                            if (assignedPackage != null)
                            {
                                var projectedEndDate = today.AddMonths(assignedPackage.DurationMonths);
                                var projectedNextDue = today.AddMonths(assignedPackage.DurationMonths);
                                
                                txtPaymentStatus.Text = "PAYMENT REQUIRED";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Text = projectedNextDue.ToString("dd/MM/yyyy") + " (After Payment)";
                                txtNextDueDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                                txtPackageEndDate.Text = projectedEndDate.ToString("dd/MM/yyyy") + " (After Payment)";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                            }
                            else
                            {
                                txtPaymentStatus.Text = "PAYMENT REQUIRED";
                                txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Red);
                                txtNextDueDate.Text = "Payment Required";
                                txtNextDueDate.Foreground = new SolidColorBrush(Colors.Red);
                                txtPackageEndDate.Text = "Payment Required";
                                txtPackageEndDate.Foreground = new SolidColorBrush(Colors.Red);
                            }
                        }
                        else
                        {
                            txtPaymentStatus.Text = "NO PACKAGE";
                            txtPaymentStatus.Foreground = new SolidColorBrush(Colors.Gray);
                            txtNextDueDate.Text = "N/A";
                            txtPackageEndDate.Text = "N/A";
                        }
                        
                        txtTotalPayments.Text = "0";
                        txtLastPaymentDate.Text = "N/A";
                        txtTotalAmount.Text = "Rs. 0.00";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading member history: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
