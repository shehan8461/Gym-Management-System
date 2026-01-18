using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using GymManagementSystem.Views.Dialogs;

namespace GymManagementSystem.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            
            // Set current date
            txtCurrentDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");
            
            // Setup Month Selection ComboBox (Last 12 Months)
            SetupMonthSelection();

            // Load main dashboard stats
            _ = LoadDashboardDataAsync();
        }

        private void SetupMonthSelection()
        {
            var today = DateTime.Now;
            var months = Enumerable.Range(0, 12)
                .Select(i => new { 
                    Date = new DateTime(today.Year, today.Month, 1).AddMonths(-i),
                    Display = new DateTime(today.Year, today.Month, 1).AddMonths(-i).ToString("MMMM yyyy") 
                })
                .ToList();

            cbMonthSelection.ItemsSource = months;
            cbMonthSelection.DisplayMemberPath = "Display";
            cbMonthSelection.SelectedValuePath = "Date";
            cbMonthSelection.SelectedIndex = 0; // Select current month by default
        }

        private async void CbMonthSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbMonthSelection.SelectedValue == null) return;

            DateTime selectedMonth = (DateTime)cbMonthSelection.SelectedValue;
            await LoadMonthlyHistoryAsync(selectedMonth);
        }

        private async Task LoadMonthlyHistoryAsync(DateTime monthStart)
        {
            try
            {
                // Ensure text is reset while loading
                txtSelectedMonthTotal.Text = "Loading...";

                var monthEnd = monthStart.AddMonths(1);
                var monthStartUtc = DateTime.SpecifyKind(monthStart, DateTimeKind.Utc);
                var monthEndUtc = DateTime.SpecifyKind(monthEnd, DateTimeKind.Utc);

                var result = await Task.Run(async () =>
                {
                    using (var context = new GymDbContext())
                    {
                        var payments = await context.Payments
                            .Where(p => p.PaymentDate >= monthStartUtc && p.PaymentDate < monthEndUtc)
                            .Include(p => p.Member)
                            .OrderByDescending(p => p.PaymentDate)
                            .Select(p => new
                            {
                                Date = p.PaymentDate, // Keep as DateTime for sorting/formatting
                                MemberName = p.Member.FullName,
                                Amount = p.Amount
                            })
                            .ToListAsync();

                        return new
                        {
                            Total = payments.Sum(p => p.Amount),
                            List = payments
                        };
                    }
                });

                // Update UI
                txtSelectedMonthTotal.Text = $"LKR {result.Total:N2}";

                var gridSource = result.List.Select(p => new
                {
                    Date = p.Date.ToString("MMM dd"),
                    p.MemberName,
                    Amount = $"LKR {p.Amount:N0}"
                }).ToList();

                dgMonthlyHistory.ItemsSource = gridSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading monthly history: {ex.Message}");
                txtSelectedMonthTotal.Text = "Error";
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // Run heavy DB operations on background thread
                var dashboardData = await Task.Run(async () =>
                {
                    using (var context = new GymDbContext())
                    {
                        var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
                        var tomorrow = today.AddDays(1);
                        var nextWeek = today.AddDays(7);
                        var currentMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        var nextMonthStart = currentMonthStart.AddMonths(1);

                        // Execute queries SEQUENTIALLY to avoid DbContext concurrency issues
                        
                        // Active Members count
                        var activeMembers = await context.Payments
                            .Where(p => p.EndDate >= today)
                            .Select(p => p.MemberId)
                            .Distinct()
                            .CountAsync();

                        // Total Members count
                        var totalMembers = await context.Members.CountAsync(m => m.IsActive);

                        // Today's Payments (Sum & Count)
                        var todaysPayments = await context.Payments
                            .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow)
                            .ToListAsync();

                        var todayCollection = todaysPayments.Sum(p => p.Amount);
                        var todayCollectionCount = todaysPayments.Count;

                        // Monthly Payments (Sum & Count) - New
                        var monthlyPayments = await context.Payments
                            .Where(p => p.PaymentDate >= currentMonthStart && p.PaymentDate < nextMonthStart)
                            .ToListAsync();

                        var monthlyCollection = monthlyPayments.Sum(p => p.Amount);
                        var monthlyCollectionCount = monthlyPayments.Count;

                        // Payments Due Soon Count
                        var paymentsDueCount = await context.Payments
                            .CountAsync(p => p.NextDueDate <= nextWeek && p.NextDueDate >= today);

                        // Today's Attendance (Grid)
                        var attendanceListTc = await context.Attendances
                            .Where(a => a.CheckInDate.Date == today)
                            .Include(a => a.Member)
                            .OrderByDescending(a => a.CheckInTime)
                            .Select(a => new
                            {
                                MemberName = a.Member.FullName,
                                CheckInTime = a.CheckInTime,
                                AttendanceType = a.AttendanceType
                            })
                            .ToListAsync();

                        // Payments Due Grid
                        var paymentsDueListTc = await context.Payments
                            .Where(p => p.NextDueDate <= nextWeek && p.NextDueDate >= today)
                            .Include(p => p.Member)
                            .Include(p => p.MembershipPackage)
                            .OrderBy(p => p.NextDueDate)
                            .Select(p => new
                            {
                                MemberName = p.Member.FullName,
                                PackageName = p.MembershipPackage.PackageName,
                                NextDueDate = p.NextDueDate
                            })
                            .ToListAsync();

                        return new
                        {
                            ActiveMembers = activeMembers,
                            TotalMembers = totalMembers,
                            PaymentsDueCount = paymentsDueCount,
                            TodaysCollection = todayCollection,
                            TodaysCollectionCount = todayCollectionCount,
                            MonthlyCollection = monthlyCollection,
                            MonthlyCollectionCount = monthlyCollectionCount,
                            AttendanceList = attendanceListTc,
                            PaymentsDueList = paymentsDueListTc
                        };
                    }
                });

                // Update UI on main thread
                txtTotalMembers.Text = dashboardData.TotalMembers.ToString();
                txtTodayAttendance.Text = dashboardData.AttendanceList.Count.ToString();
                txtTodayCollection.Text = $"LKR {dashboardData.TodaysCollection:N2}";
                txtTodayCollectionCount.Text = $"{dashboardData.TodaysCollectionCount} payments";
                
                // Update Monthly Collection Card
                txtMonthlyCollection.Text = $"LKR {dashboardData.MonthlyCollection:N2}";
                txtMonthlyCollectionCount.Text = $"{dashboardData.MonthlyCollectionCount} payments";

                // REMOVED txtRevenueToday as its card was removed in XAML update
                // txtRevenueToday.Text = $"LKR {dashboardData.TodaysCollection:N2}";
                
                txtActiveMembers.Text = dashboardData.ActiveMembers.ToString();

                // Transform raw data for Grids (formatting times/dates)
                var attendanceGridSource = dashboardData.AttendanceList.Select(a => new 
                {
                    a.MemberName,
                    CheckInTime = a.CheckInTime.ToString(@"hh\:mm\:ss"),
                    a.AttendanceType
                }).ToList();
                dgTodayAttendance.ItemsSource = attendanceGridSource;

                var paymentsDueGridSource = dashboardData.PaymentsDueList.Select(p => new
                {
                    p.MemberName,
                    p.PackageName,
                    DueDate = p.NextDueDate.ToString("dd/MM/yyyy"),
                    Status = p.NextDueDate <= DateTime.UtcNow.Date.AddDays(3) ? "🔴 Urgent" : "🟡 Due Soon"
                }).ToList();
                dgPaymentsDue.ItemsSource = paymentsDueGridSource;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddMember_Click(object sender, RoutedEventArgs e)
        {
            // Open Add Member Dialog directly
            var dialog = new AddEditMemberDialog();
            if (dialog.ShowDialog() == true)
            {
                // Refresh dashboard data if a new member was added
                _ = LoadDashboardDataAsync();
            }
        }
    }
}
