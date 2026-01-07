using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class MemberAttendanceHistoryDialog : Window
    {
        private int _memberId;

        public MemberAttendanceHistoryDialog(int memberId)
        {
            InitializeComponent();
            _memberId = memberId;
            LoadAttendanceHistory();
        }

        private void LoadAttendanceHistory()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Load member details
                    var member = context.Members.FirstOrDefault(m => m.MemberId == _memberId);
                    if (member != null)
                    {
                        txtMemberName.Text = member.FullName;
                        txtMemberInfo.Text = $"{member.PhoneNumber} | {member.NIC}";
                    }

                    // Load attendance history
                    var attendances = context.Attendances
                        .Where(a => a.MemberId == _memberId)
                        .OrderByDescending(a => a.CheckInDate)
                        .ThenByDescending(a => a.CheckInTime)
                        .Select(a => new
                        {
                            a.CheckInDate,
                            a.CheckInTime,
                            a.CheckOutTime,
                            a.AttendanceType,
                            a.Remarks,
                            DateDisplay = a.CheckInDate.ToLocalTime().ToString("dd/MM/yyyy"),
                            CheckInDisplay = a.CheckInTime.ToString(@"hh\:mm\:ss"),
                            CheckOutDisplay = a.CheckOutTime.HasValue ? a.CheckOutTime.Value.ToString(@"hh\:mm\:ss") : "-",
                            DurationDisplay = a.CheckOutTime.HasValue 
                                ? CalculateDuration(a.CheckInTime, a.CheckOutTime.Value)
                                : "-"
                        })
                        .ToList();

                    dgAttendanceHistory.ItemsSource = attendances;
                    txtTotalAttendance.Text = $"Total Attendance: {attendances.Count} days";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance history: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string CalculateDuration(TimeSpan checkIn, TimeSpan checkOut)
        {
            var duration = checkOut - checkIn;
            if (duration.TotalMinutes < 0)
                return "-";
            
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }
            else
            {
                return $"{duration.Minutes}m";
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
