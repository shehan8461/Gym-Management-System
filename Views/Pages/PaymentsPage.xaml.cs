using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Pages
{
    public partial class PaymentsPage : Page
    {
        public PaymentsPage()
        {
            InitializeComponent();
            LoadPayments();
        }

        private void LoadPayments(string searchTerm = "", string statusFilter = "All")
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var query = context.Payments
                        .Include(p => p.Member)
                        .Include(p => p.MembershipPackage)
                        .AsQueryable();

                    // Search filter
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query = query.Where(p => p.Member != null && p.Member.FullName.Contains(searchTerm));
                    }

                    var payments = query
                        .OrderByDescending(p => p.PaymentDate)
                        .Select(p => new
                        {
                            p.PaymentId,
                            p.MemberId,
                            MemberName = p.Member != null ? p.Member.FullName : "Unknown",
                            PackageName = p.MembershipPackage != null ? p.MembershipPackage.PackageName : "Unknown",
                            p.Amount,
                            p.PaymentDate,
                            p.EndDate,
                            p.NextDueDate,
                            p.PaymentMethod,
                            Status = GetPaymentStatus(p.NextDueDate, p.EndDate),
                            StatusDisplay = GetPaymentStatusDisplay(p.NextDueDate, p.EndDate)
                        })
                        .ToList();

                    // Status filter
                    if (statusFilter != "All")
                    {
                        payments = payments.Where(p => p.Status == statusFilter).ToList();
                    }

                    if (dgPayments != null)
                    {
                        dgPayments.ItemsSource = payments;
                    }
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? $"\n\nInner: {ex.InnerException.Message}" : "";
                MessageBox.Show($"Error loading payments: {ex.Message}{innerMsg}\n\nStack: {ex.StackTrace}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetPaymentStatus(DateTime nextDueDate, DateTime endDate)
        {
            var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
            if (today <= endDate)
                return "Paid";
            else if (nextDueDate <= today.AddDays(7))
                return "Due Soon";
            else
                return "Overdue";
        }

        private static string GetPaymentStatusDisplay(DateTime nextDueDate, DateTime endDate)
        {
            var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
            if (today <= endDate)
                return "🟢 Paid";
            else if (nextDueDate <= today.AddDays(3))
                return "🔴 Overdue";
            else if (nextDueDate <= today.AddDays(7))
                return "🟡 Due Soon";
            else
                return "🟢 Active";
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (cmbStatusFilter != null)
            {
                string status = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All";
                LoadPayments(txtSearch.Text.Trim(), status);
            }
        }

        private void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtSearch != null)
            {
                string status = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All";
                LoadPayments(txtSearch.Text.Trim(), status);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (txtSearch != null)
                txtSearch.Text = "";
            if (cmbStatusFilter != null)
                cmbStatusFilter.SelectedIndex = 0;
            LoadPayments();
        }
    }
}
