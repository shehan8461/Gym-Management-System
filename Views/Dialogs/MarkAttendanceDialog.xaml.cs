using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using GymManagementSystem.Services;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class MarkAttendanceDialog : Window
    {
        public MarkAttendanceDialog()
        {
            InitializeComponent();
            LoadMembers();
            dpAttendanceDate.SelectedDate = DateTime.Now.Date;
            tpCheckInTime.SelectedTime = DateTime.Now; // Display in local time for UI
        }

        private void LoadMembers()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var members = context.Members
                        .Where(m => m.IsActive)
                        .OrderBy(m => m.FullName)
                        .Select(m => new
                        {
                            m.MemberId,
                            DisplayText = $"{m.FullName} ({m.PhoneNumber})"
                        })
                        .ToList();

                    cmbMember.ItemsSource = members;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            try
            {
                int memberId = (int)cmbMember.SelectedValue;
                DateTime attendanceDate = dpAttendanceDate.SelectedDate.Value.ToUniversalTime();

                using (var context = new GymDbContext())
                {
                    // Check if already marked attendance today
                    var utcDate = attendanceDate.Date;
                    var existingAttendance = context.Attendances
                        .FirstOrDefault(a => a.MemberId == memberId && 
                                           a.CheckInDate.Date == utcDate);

                    if (existingAttendance != null)
                    {
                        var result = MessageBox.Show(
                            "This member has already marked attendance today. Do you want to update the check-out time?",
                            "Duplicate Attendance",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            existingAttendance.CheckOutDate = attendanceDate;
                            existingAttendance.CheckOutTime = tpCheckInTime.SelectedTime.Value.TimeOfDay;
                            context.SaveChanges();
                            MessageBox.Show("Check-out time updated successfully!", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        return;
                    }

                    var attendance = new Attendance
                    {
                        MemberId = memberId,
                        CheckInDate = attendanceDate,
                        CheckInTime = tpCheckInTime.SelectedTime.Value.TimeOfDay,
                        AttendanceType = "Manual",
                        RecordedByUserId = SessionManager.CurrentUserId,
                        Remarks = txtRemarks.Text.Trim()
                    };

                    context.Attendances.Add(attendance);
                    context.SaveChanges();

                    MessageBox.Show("Attendance marked successfully!", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error marking attendance: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            if (cmbMember.SelectedValue == null)
            {
                MessageBox.Show("Please select a member.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!dpAttendanceDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select attendance date.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!tpCheckInTime.SelectedTime.HasValue)
            {
                MessageBox.Show("Please select check-in time.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
