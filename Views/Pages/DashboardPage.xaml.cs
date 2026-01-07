using System;
using System.Linq;
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
            
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Total Members
                    txtTotalMembers.Text = context.Members.Count(m => m.IsActive).ToString();

                    // Today's Attendance (use local date, stored as UTC date-key)
                    var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
                    var todayAttendance = context.Attendances
                        .Where(a => a.CheckInDate.Date == today)
                        .ToList();
                    txtTodayAttendance.Text = todayAttendance.Count.ToString();

                    // Today's Collection
                    var tomorrow = today.AddDays(1);
                    var todaysPaymentsQuery = context.Payments
                        .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow);

                    var todaysCollection = todaysPaymentsQuery.Sum(p => (decimal?)p.Amount) ?? 0m;
                    var todaysPaymentsCount = todaysPaymentsQuery.Count();

                    txtTodayCollection.Text = $"LKR {todaysCollection:N2}";
                    txtTodayCollectionCount.Text = $"{todaysPaymentsCount} payments";
                    txtRevenueToday.Text = $"LKR {todaysCollection:N2}";

                    // Active Memberships (have valid payment)
                    var activeMembers = context.Payments
                        .Where(p => p.EndDate >= today)
                        .Select(p => p.MemberId)
                        .Distinct()
                        .Count();
                    txtActiveMembers.Text = activeMembers.ToString();

                    // Payments Due (next 7 days)
                    var dueDate = today.AddDays(7);
                    var paymentsDue = context.Payments
                        .Where(p => p.NextDueDate <= dueDate && p.NextDueDate >= today)
                        .Count();

                    // Load Today's Attendance Grid
                    var attendanceList = context.Attendances
                        .Where(a => a.CheckInDate.Date == today)
                        .Include(a => a.Member)
                        .OrderByDescending(a => a.CheckInTime)
                        .Select(a => new
                        {
                            MemberName = a.Member.FullName,
                            CheckInTime = a.CheckInTime.ToString(@"hh\:mm\:ss"),
                            AttendanceType = a.AttendanceType
                        })
                        .ToList();
                    dgTodayAttendance.ItemsSource = attendanceList;

                    // Load Payments Due Grid
                    var paymentsDueList = context.Payments
                        .Where(p => p.NextDueDate <= dueDate && p.NextDueDate >= today)
                        .Include(p => p.Member)
                        .Include(p => p.MembershipPackage)
                        .OrderBy(p => p.NextDueDate)
                        .Select(p => new
                        {
                            MemberName = p.Member.FullName,
                            PackageName = p.MembershipPackage.PackageName,
                            DueDate = p.NextDueDate.ToString("dd/MM/yyyy"),
                            Status = p.NextDueDate <= today.AddDays(3) ? "🔴 Urgent" : "🟡 Due Soon"
                        })
                        .ToList();
                    dgPaymentsDue.ItemsSource = paymentsDueList;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading dashboard: {ex.Message}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
