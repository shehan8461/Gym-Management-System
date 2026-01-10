using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GymManagementSystem.Views.Pages
{
    public partial class AttendancePage : Page
    {
        public AttendancePage()
        {
            InitializeComponent();
            dpFilterDate.SelectedDate = DateTime.Now.Date;
            LoadAttendance();
            LoadStatistics();
        }

        private System.Threading.CancellationTokenSource _searchCts;
        private System.Threading.CancellationTokenSource _loadCts;

        private async void LoadAttendance(string searchTerm = "", DateTime? filterDate = null)
        {
            _loadCts?.Cancel();
            _loadCts = new System.Threading.CancellationTokenSource();
            var token = _loadCts.Token;

            try
            {
                var result = await Task.Run(async () =>
                {
                    if (token.IsCancellationRequested) return null;

                    using (var context = new GymDbContext())
                    {
                        var query = context.Attendances
                            .Include(a => a.Member)
                            .AsQueryable();

                        // Date filter
                        if (filterDate.HasValue)
                        {
                            var utcDate = DateTime.SpecifyKind(filterDate.Value.Date, DateTimeKind.Utc);
                            query = query.Where(a => a.CheckInDate.Date == utcDate);
                        }

                        // Search filter
                        if (!string.IsNullOrEmpty(searchTerm))
                        {
                            var lowerTerm = searchTerm.ToLower();
                            query = query.Where(a => a.Member.FullName.ToLower().Contains(lowerTerm));
                        }

                        var attendances = await query
                            .OrderByDescending(a => a.CheckInDate)
                            .ThenByDescending(a => a.CheckInTime)
                            .AsNoTracking()
                            .ToListAsync(token);

                        if (token.IsCancellationRequested) return null;

                        var projected = attendances.Select(a => new
                        {
                            a.AttendanceId,
                            a.MemberId,
                            MemberName = a.Member.FullName,
                            a.CheckInDate,
                            CheckInTimeDisplay = a.CheckInTime.ToString(@"hh\:mm\:ss"),
                            CheckOutTimeDisplay = a.CheckOutTime.HasValue ? a.CheckOutTime.Value.ToString(@"hh\:mm\:ss") : "-",
                            a.AttendanceType,
                            a.Remarks
                        }).ToList();

                        return projected;
                    }
                }, token);

                if (token.IsCancellationRequested || result == null) return;

                dgAttendance.ItemsSource = result;
                
                // Reload stats if date changes or first load
                LoadStatistics(filterDate);
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                // Ignore Oracle cancellation
                if (ex.Message.Contains("ORA-01013")) return;

                MessageBox.Show($"Error loading attendance: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadStatistics(DateTime? filterDate = null)
        {
            try
            {
                var targetDate = filterDate ?? DateTime.Now.Date;
                var utcTargetDate = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc);

                var stats = await Task.Run(() =>
                {
                    using (var context = new GymDbContext())
                    {
                        var todayCount = context.Attendances
                            .Count(a => a.CheckInDate.Date == utcTargetDate);
                        
                        // Active = Checked in today but NOT checked out
                        var activeCount = context.Attendances
                            .Count(a => a.CheckInDate.Date == utcTargetDate && a.CheckOutTime == null);

                        return new { todayCount, activeCount };
                    }
                });

                txtPresentCount.Text = stats.todayCount.ToString();
                txtActiveCount.Text = stats.activeCount.ToString();
            }
            catch (Exception)
            {
                // Fail silently
            }
        }

        private void btnMarkAttendance_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MarkAttendanceDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadAttendance(txtSearch.Text.Trim(), dpFilterDate.SelectedDate);
            }
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchCts?.Cancel();
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token);
                LoadAttendance(txtSearch.Text.Trim(), dpFilterDate.SelectedDate);
            }
            catch (TaskCanceledException) { }
        }

        private void dpFilterDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAttendance(txtSearch.Text.Trim(), dpFilterDate.SelectedDate);
        }

        private void btnToday_Click(object sender, RoutedEventArgs e)
        {
            dpFilterDate.SelectedDate = DateTime.Now.Date;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            dpFilterDate.SelectedDate = DateTime.Now.Date;
            LoadAttendance();
        }
    }
}
