using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            
            // Set current date
            txtCurrentDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");
            
            // Fire and forget (it handles its own exceptions)
            _ = LoadDashboardDataAsync();
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
                txtRevenueToday.Text = $"LKR {dashboardData.TodaysCollection:N2}";
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
    }
}
