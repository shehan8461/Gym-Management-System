using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Pages
{
    public partial class AttendancePage : Page
    {
        public AttendancePage()
        {
            InitializeComponent();
            dpFilterDate.SelectedDate = DateTime.Now.Date;
            LoadAttendance();
        }

        private void LoadAttendance(string searchTerm = "", DateTime? filterDate = null)
        {
            try
            {
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
                        query = query.Where(a => a.Member.FullName.Contains(searchTerm));
                    }

                    var attendances = query
                        .OrderByDescending(a => a.CheckInDate)
                        .ThenByDescending(a => a.CheckInTime)
                        .Select(a => new
                        {
                            a.AttendanceId,
                            a.MemberId,
                            MemberName = a.Member.FullName,
                            a.CheckInDate,
                            CheckInTimeDisplay = a.CheckInTime.ToString(@"hh\:mm\:ss"),
                            CheckOutTimeDisplay = a.CheckOutTime.HasValue ? a.CheckOutTime.Value.ToString(@"hh\:mm\:ss") : "-",
                            a.AttendanceType,
                            a.Remarks
                        })
                        .ToList();

                    dgAttendance.ItemsSource = attendances;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadAttendance(txtSearch.Text.Trim(), dpFilterDate.SelectedDate);
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
