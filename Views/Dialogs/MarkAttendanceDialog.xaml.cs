using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using GymManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class MarkAttendanceDialog : Window
    {
        private Attendance? _existingAttendance = null;
        private bool _isCheckOutMode = false;

        public MarkAttendanceDialog()
        {
            InitializeComponent();
            LoadMembers();
            
            // Set default dates - DO NOT convert to UTC here, just display local date
            var today = DateTime.Now.Date;
            dpCheckInDate.SelectedDate = today;
            dpCheckInDate.DisplayDateEnd = today; // Cannot select future dates
            tpCheckInTime.SelectedTime = DateTime.Now;
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

        private void cmbMember_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbMember.SelectedValue == null)
                return;

            CheckExistingAttendance();
        }

        private void CheckExistingAttendance()
        {
            try
            {
                int memberId = (int)cmbMember.SelectedValue;
                var selectedDate = dpCheckInDate.SelectedDate ?? DateTime.Now.Date;
                
                // Convert selected date to UTC for comparison
                var utcDate = DateTime.SpecifyKind(selectedDate.Date, DateTimeKind.Utc);

                using (var context = new GymDbContext())
                {
                    _existingAttendance = context.Attendances
                        .FirstOrDefault(a => a.MemberId == memberId && 
                                           a.CheckInDate.Date == utcDate);

                    if (_existingAttendance != null && !_existingAttendance.CheckOutTime.HasValue)
                    {
                        // Member has checked in but not checked out
                        _isCheckOutMode = true;
                        checkOutSection.Visibility = Visibility.Visible;
                        existingAttendanceCard.Visibility = Visibility.Visible;
                        
                        txtTitle.Text = "Record Check-Out";
                        btnSave.Content = "RECORD CHECK-OUT";
                        
                        // Show existing check-in info
                        txtExistingCheckIn.Text = $"Checked in at {_existingAttendance.CheckInTime:hh\\:mm\\:ss} on {_existingAttendance.CheckInDate.ToLocalTime():dd/MM/yyyy}";
                        
                        // Set check-out date defaults
                        dpCheckOutDate.SelectedDate = DateTime.Now.Date;
                        dpCheckOutDate.DisplayDateStart = _existingAttendance.CheckInDate.ToLocalTime().Date;
                        dpCheckOutDate.DisplayDateEnd = DateTime.Now.Date;
                        tpCheckOutTime.SelectedTime = DateTime.Now;
                        
                        // Disable check-in fields
                        dpCheckInDate.IsEnabled = false;
                        tpCheckInTime.IsEnabled = false;
                    }
                    else
                    {
                        // Normal check-in mode
                        _isCheckOutMode = false;
                        checkOutSection.Visibility = Visibility.Collapsed;
                        existingAttendanceCard.Visibility = Visibility.Collapsed;
                        
                        txtTitle.Text = "Mark Attendance";
                        btnSave.Content = "MARK ATTENDANCE";
                        
                        dpCheckInDate.IsEnabled = true;
                        tpCheckInTime.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking attendance: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dpCheckInDate_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbMember.SelectedValue != null)
            {
                CheckExistingAttendance();
            }
        }

        private void tpCheckInTime_SelectedTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            ValidateCheckOutTime();
        }

        private void dpCheckOutDate_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ValidateCheckOutTime();
        }

        private void ValidateCheckOutTime()
        {
            if (!_isCheckOutMode || _existingAttendance == null)
                return;

            if (dpCheckOutDate.SelectedDate.HasValue && tpCheckOutTime.SelectedTime.HasValue)
            {
                var checkInDateTime = _existingAttendance.CheckInDate.ToLocalTime().Add(_existingAttendance.CheckInTime);
                var checkOutDateTime = dpCheckOutDate.SelectedDate.Value.Add(tpCheckOutTime.SelectedTime.Value.TimeOfDay);

                if (checkOutDateTime < checkInDateTime)
                {
                    txtWarning.Text = "⚠ Check-out time cannot be earlier than check-in time!";
                    txtWarning.Visibility = Visibility.Visible;
                    btnSave.IsEnabled = false;
                }
                else
                {
                    txtWarning.Visibility = Visibility.Collapsed;
                    btnSave.IsEnabled = true;
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            try
            {
                int memberId = (int)cmbMember.SelectedValue;

                using (var context = new GymDbContext())
                {
                    if (_isCheckOutMode && _existingAttendance != null)
                    {
                        // Update existing attendance with check-out time
                        var attendance = context.Attendances.AsTracking()
                            .FirstOrDefault(a => a.AttendanceId == _existingAttendance.AttendanceId);
                        
                        if (attendance != null)
                        {
                            // Store dates in UTC
                            attendance.CheckOutDate = DateTime.SpecifyKind(dpCheckOutDate.SelectedDate.Value.Date, DateTimeKind.Utc);
                            attendance.CheckOutTime = tpCheckOutTime.SelectedTime.Value.TimeOfDay;
                            
                            if (!string.IsNullOrWhiteSpace(txtRemarks.Text))
                            {
                                attendance.Remarks = (attendance.Remarks ?? "") + " | Check-out: " + txtRemarks.Text.Trim();
                            }
                            
                            context.SaveChanges();
                            
                            MessageBox.Show("Check-out recorded successfully!", 
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            DialogResult = true;
                            Close();
                        }
                    }
                    else
                    {
                        // Create new attendance record - Store date in UTC without time component
                        var localDate = dpCheckInDate.SelectedDate.Value.Date;
                        var utcDate = DateTime.SpecifyKind(localDate, DateTimeKind.Utc);
                        
                        var attendance = new Attendance
                        {
                            MemberId = memberId,
                            CheckInDate = utcDate,  // Store as UTC date
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

            if (_isCheckOutMode)
            {
                if (!dpCheckOutDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Please select check-out date.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!tpCheckOutTime.SelectedTime.HasValue)
                {
                    MessageBox.Show("Please select check-out time.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            else
            {
                if (!dpCheckInDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Please select check-in date.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!tpCheckInTime.SelectedTime.HasValue)
                {
                    MessageBox.Show("Please select check-in time.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
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
