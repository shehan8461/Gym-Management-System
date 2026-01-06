using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using GymManagementSystem.Data;
using GymManagementSystem.Models;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class AddEditMemberDialog : Window
    {
        private int? _memberId;
        private string? _photoPath;

        public AddEditMemberDialog(int? memberId = null)
        {
            InitializeComponent();
            _memberId = memberId;

            if (_memberId.HasValue)
            {
                txtTitle.Text = "Edit Member";
                LoadMemberData();
            }
            else
            {
                dpDateOfBirth.SelectedDate = DateTime.Now.AddYears(-18);
            }
        }

        private void LoadMemberData()
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var member = context.Members.FirstOrDefault(m => m.MemberId == _memberId);
                    if (member != null)
                    {
                        txtFullName.Text = member.FullName;
                        txtPhoneNumber.Text = member.PhoneNumber;
                        txtNIC.Text = member.NIC;
                        dpDateOfBirth.SelectedDate = member.DateOfBirth;
                        txtEmail.Text = member.Email;
                        txtAddress.Text = member.Address;
                        chkIsActive.IsChecked = member.IsActive;
                        
                        if (!string.IsNullOrEmpty(member.Gender))
                        {
                            cmbGender.Text = member.Gender;
                        }

                        if (!string.IsNullOrEmpty(member.PhotoPath) && File.Exists(member.PhotoPath))
                        {
                            _photoPath = member.PhotoPath;
                            imgPhoto.Source = new BitmapImage(new Uri(member.PhotoPath));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading member data: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                Title = "Select Member Photo"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _photoPath = openFileDialog.FileName;
                imgPhoto.Source = new BitmapImage(new Uri(_photoPath));
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            try
            {
                using (var context = new GymDbContext())
                {
                    Member member;

                    if (_memberId.HasValue)
                    {
                        member = context.Members.FirstOrDefault(m => m.MemberId == _memberId);
                        if (member == null)
                        {
                            MessageBox.Show("Member not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        member = new Member();
                        context.Members.Add(member);
                    }

                    member.FullName = txtFullName.Text.Trim();
                    member.PhoneNumber = txtPhoneNumber.Text.Trim();
                    member.NIC = txtNIC.Text.Trim();
                    member.DateOfBirth = (dpDateOfBirth.SelectedDate ?? DateTime.UtcNow.Date.AddYears(-18)).ToUniversalTime();
                    member.Gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content.ToString();
                    member.Email = txtEmail.Text.Trim();
                    member.Address = txtAddress.Text.Trim();
                    member.IsActive = chkIsActive.IsChecked ?? true;

                    // Save photo
                    if (!string.IsNullOrEmpty(_photoPath))
                    {
                        string photoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "MemberPhotos");
                        Directory.CreateDirectory(photoDir);
                        
                        string fileName = $"member_{member.MemberId}_{DateTime.UtcNow.Ticks}{Path.GetExtension(_photoPath)}";
                        string destPath = Path.Combine(photoDir, fileName);
                        
                        if (_photoPath != destPath)
                        {
                            File.Copy(_photoPath, destPath, true);
                        }
                        
                        member.PhotoPath = destPath;
                    }

                    context.SaveChanges();
                    MessageBox.Show("Member saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? $"\n\nInner Error: {ex.InnerException.Message}" : "";
                MessageBox.Show($"Error saving member: {ex.Message}{innerMsg}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Please enter full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPhoneNumber.Text))
            {
                MessageBox.Show("Please enter phone number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtNIC.Text))
            {
                MessageBox.Show("Please enter NIC.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
