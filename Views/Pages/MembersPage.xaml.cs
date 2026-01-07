using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Pages
{
    public partial class MembersPage : Page
    {
        public MembersPage()
        {
            InitializeComponent();
            LoadMembers();
        }

        private void LoadMembers(string searchTerm = "")
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    var today = DateTime.UtcNow.Date;
                    
                    // Use eager loading to prevent N+1 query problem
                    var query = context.Members
                        .Include(m => m.AssignedPackage)  // Load package data
                        .AsQueryable();

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query = query.Where(m => 
                            m.FullName.Contains(searchTerm) || 
                            m.PhoneNumber.Contains(searchTerm) || 
                            m.NIC.Contains(searchTerm));
                    }

                    var members = query.OrderByDescending(m => m.RegistrationDate).ToList();
                    
                    // Get all member IDs for batch payment query
                    var memberIds = members.Select(m => m.MemberId).ToList();
                    
                    // Load all payments in one query (prevents N+1)
                    var lastPayments = context.Payments
                        .Where(p => memberIds.Contains(p.MemberId))
                        .GroupBy(p => new { p.MemberId, p.PackageId })
                        .Select(g => g.OrderByDescending(p => p.PaymentDate).FirstOrDefault())
                        .ToList();
                    
                    // Calculate payment status for each member
                    foreach (var member in members)
                    {
                        // Get assigned package
                        if (member.AssignedPackageId.HasValue)
                        {
                            member.AssignedPackageName = member.AssignedPackage?.PackageName ?? "Unknown";
                            
                            // Get last payment for the CURRENT assigned package from preloaded data
                            var lastPaymentForCurrentPackage = lastPayments
                                .FirstOrDefault(p => p.MemberId == member.MemberId && p.PackageId == member.AssignedPackageId.Value);
                            
                            if (lastPaymentForCurrentPackage != null)
                            {
                                // Show dates from the payment for the current package
                                member.LastPaymentDate = lastPaymentForCurrentPackage.PaymentDate;
                                member.NextDueDate = lastPaymentForCurrentPackage.NextDueDate;
                                
                                // Calculate payment status
                                var daysUntilDue = (lastPaymentForCurrentPackage.NextDueDate.Date - today).Days;
                                
                                if (daysUntilDue < 0)
                                {
                                    member.PaymentStatus = "Overdue";
                                }
                                else if (daysUntilDue <= 7)
                                {
                                    member.PaymentStatus = "Due Soon";
                                }
                                else
                                {
                                    member.PaymentStatus = "Paid";
                                }
                            }
                            else
                            {
                                // Package assigned but no payment yet
                                member.PaymentStatus = "Payment Required";
                                member.LastPaymentDate = null;
                                member.NextDueDate = null;
                            }
                        }
                        else
                        {
                            member.AssignedPackageName = "No Package";
                            member.PaymentStatus = "No Package";
                            member.LastPaymentDate = null;
                            member.NextDueDate = null;
                        }
                    }
                    
                    dgMembers.ItemsSource = members;
                    
                    // Update statistics cards
                    UpdateStatistics(members);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics(System.Collections.Generic.List<Models.Member> members)
        {
            txtTotalMembers.Text = members.Count.ToString();
            txtActiveMembers.Text = members.Count(m => m.IsActive).ToString();
            txtExpiringSoon.Text = members.Count(m => m.PaymentStatus == "Due Soon").ToString();
            txtOverdue.Text = members.Count(m => m.PaymentStatus == "Overdue").ToString();
        }

        private void btnAddMember_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditMemberDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadMembers();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new AddEditMemberDialog(memberId);
                if (dialog.ShowDialog() == true)
                {
                    LoadMembers();
                }
            }
        }

        private void btnPayment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                // Check if current package payment is already completed
                using (var context = new GymDbContext())
                {
                    var member = context.Members.FirstOrDefault(m => m.MemberId == memberId);
                    
                    if (member == null)
                    {
                        MessageBox.Show("Member not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Check if member has an assigned package
                    if (member.AssignedPackageId.HasValue)
                    {
                        // Get the last payment for this member and the assigned package
                        var lastPayment = context.Payments
                            .Where(p => p.MemberId == memberId && p.PackageId == member.AssignedPackageId.Value)
                            .OrderByDescending(p => p.PaymentDate)
                            .FirstOrDefault();

                        if (lastPayment != null)
                        {
                            var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
                            
                            // Check if the package period is still active (not expired)
                            var daysUntilEnd = (lastPayment.EndDate.Date - today).Days;
                            var daysUntilDue = (lastPayment.NextDueDate.Date - today).Days;
                            
                            // Payment is considered complete if:
                            // 1. Package hasn't expired (end date is in the future)
                            // 2. Payment is not due soon (more than 7 days until due)
                            if (daysUntilEnd >= 0 && daysUntilDue > 7)
                            {
                                var packageName = context.MembershipPackages
                                    .FirstOrDefault(p => p.PackageId == member.AssignedPackageId.Value)?.PackageName ?? "Unknown";
                                
                                MessageBox.Show(
                                    "Payment is already completed for the current package.\n\n" +
                                    $"Package: {packageName}\n" +
                                    $"Payment Date: {lastPayment.PaymentDate:dd/MM/yyyy}\n" +
                                    $"Next Due Date: {lastPayment.NextDueDate:dd/MM/yyyy}\n" +
                                    $"Package End Date: {lastPayment.EndDate:dd/MM/yyyy}\n" +
                                    $"Days Until Due: {daysUntilDue}\n\n" +
                                    "You can only make a payment when:\n" +
                                    "• The payment is due soon (within 7 days) or overdue\n" +
                                    "• The package has been changed to a different one\n" +
                                    "• The package period has expired",
                                    "Payment Already Completed",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                                return;
                            }
                        }
                    }
                }

                // If checks pass, open payment dialog
                var dialog = new AddPaymentDialog(memberId);
                if (dialog.ShowDialog() == true)
                {
                    LoadMembers();
                }
            }
        }
        
        private void btnAssignPackage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new AssignPackageDialog(memberId);
                if (dialog.ShowDialog() == true)
                {
                    LoadMembers();
                }
            }
        }
        
        private void btnBiometric_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                MessageBox.Show("Biometric enrollment feature.\nThis will open the biometric device enrollment dialog for fingerprint registration.",
                    "Biometric Enrollment", MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: Implement biometric enrollment dialog
                // var dialog = new BiometricEnrollDialog(memberId);
                // dialog.ShowDialog();
            }
        }
        
        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new MemberHistoryDialog(memberId);
                dialog.ShowDialog();
            }
        }

        private void btnAttendance_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int memberId)
            {
                var dialog = new MemberAttendanceHistoryDialog(memberId);
                dialog.ShowDialog();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadMembers();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadMembers(txtSearch.Text.Trim());
        }

        private void dgMembers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgMembers.SelectedItem != null)
            {
                var member = dgMembers.SelectedItem as dynamic;
                if (member != null)
                {
                    var dialog = new AddEditMemberDialog(member.MemberId);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadMembers();
                    }
                }
            }
        }
    }
}
