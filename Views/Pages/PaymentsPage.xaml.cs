using System;
using System.Linq;
using System.Threading.Tasks;
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

        private System.Threading.CancellationTokenSource _searchCts;
        private System.Threading.CancellationTokenSource _loadCts;

        private async void LoadPayments(string searchTerm = "", string statusFilter = "All")
        {
            // Cancel previous loading operation
            _loadCts?.Cancel();
            _loadCts = new System.Threading.CancellationTokenSource();
            var token = _loadCts.Token;

            try
            {
                // Run heavy DB and processing logic on background thread
                var result = await Task.Run(async () =>
                {
                    if (token.IsCancellationRequested) return null;

                    using (var context = new GymDbContext())
                    {
                        var query = context.Payments
                            .Include(p => p.Member)
                            .Include(p => p.MembershipPackage)
                            .AsQueryable();

                        // Search filter
                        if (!string.IsNullOrEmpty(searchTerm))
                        {
                            var lowerTerm = searchTerm.ToLower();
                            query = query.Where(p => p.Member.FullName.ToLower().Contains(lowerTerm));
                        }

                        // Execute query asynchronously
                        var rawPayments = await query
                            .OrderByDescending(p => p.PaymentDate)
                            .AsNoTracking()
                            .ToListAsync(token);

                        if (token.IsCancellationRequested) return null;

                        // Transform data in memory
                        var projectedPayments = rawPayments.Select(p => new
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
                        }).ToList();

                        // Status filter (in memory)
                        if (statusFilter != "All" && statusFilter != "All Status")
                        {
                            projectedPayments = projectedPayments.Where(p => p.Status == statusFilter).ToList();
                        }

                        return projectedPayments;
                    }
                }, token);

                if (token.IsCancellationRequested || result == null) return;

                if (dgPayments != null)
                {
                    dgPayments.ItemsSource = result;
                }

                // Load statistics (can be done in parallel or subsequent)
                LoadStatistics();
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                // Ignore Oracle cancellation error
                if (ex.Message.Contains("ORA-01013") || ex.Message.Contains("user requested cancel"))
                {
                    return;
                }

                var innerMsg = ex.InnerException != null ? $"\n\nInner: {ex.InnerException.Message}" : "";
                MessageBox.Show($"Error loading payments: {ex.Message}{innerMsg}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadStatistics()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var today = DateTime.UtcNow.Date;
                    var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                    var nextWeek = today.AddDays(7);

                    // Execute lightweight aggregate queries
                    var stats = await Task.Run(async () =>
                    {
                        var totalRevenue = await context.Payments
                            .Where(p => p.PaymentDate >= firstDayOfMonth)
                            .SumAsync(p => p.Amount);

                        var txCount = await context.Payments.CountAsync();

                        // For Overdue/DueSoon, we need to inspect the latest payment status for active members
                        // This might be expensive, so for now let's approximate based on Payments table "NextDueDate"
                        // ideally we should query Members, but sticking to Payments page context:
                        
                        var overdueCount = await context.Payments
                            .Where(p => p.NextDueDate < today && p.NextDueDate > today.AddDays(-365)) // optimization: only look back 1 year
                            .CountAsync(); // This is a rough proxy since it counts payments, not members. 
                                           // But for "Payments Page" statistics, counting "Overdue Payments" is acceptable.

                        var dueSoonCount = await context.Payments
                            .Where(p => p.NextDueDate >= today && p.NextDueDate <= nextWeek)
                            .CountAsync();

                        return new { totalRevenue, txCount, overdueCount, dueSoonCount };
                    });

                    // Update UI
                    txtTotalRevenue.Text = $"LKR {stats.totalRevenue:N2}";
                    txtTransactionCount.Text = stats.txCount.ToString();
                    txtOverdueCount.Text = stats.overdueCount.ToString();
                    txtDueSoonCount.Text = stats.dueSoonCount.ToString();
                }
            }
            catch (Exception)
            {
                // Silently fail statistics to not block main UI
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
                return "Paid";
            else if (nextDueDate <= today.AddDays(3))
                return "Overdue"; // Tightened overload logic
            else if (nextDueDate <= today.AddDays(7))
                return "Due Soon";
            else
                return "Active";
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (cmbStatusFilter != null)
            {
                // Debounce search
                _searchCts?.Cancel();
                _searchCts = new System.Threading.CancellationTokenSource();
                var token = _searchCts.Token;

                try
                {
                    await Task.Delay(300, token);
                    
                    string status = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Status";
                    LoadPayments(txtSearch.Text.Trim(), status);
                }
                catch (TaskCanceledException)
                {
                    // Ignore
                }
            }
        }

        private void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtSearch != null)
            {
                string status = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Status";
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
