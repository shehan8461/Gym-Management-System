using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GymManagementSystem.Data;
using GymManagementSystem.Views.Dialogs;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Views.Pages
{
    public partial class PackagesPage : Page
    {
        public PackagesPage()
        {
            InitializeComponent();
            LoadPackages();
        }

        private async void LoadPackages()
        {
            try
            {
                var result = await Task.Run(async () =>
                {
                    using (var context = new GymDbContext())
                    {
                        // 1. Fetch Packages
                        var packages = await context.MembershipPackages
                            .OrderBy(p => p.DurationMonths)
                            .AsNoTracking()
                            .ToListAsync();

                        // 2. Fetch Member Counts (Optimized: Single GroupBy Query)
                        var memberCounts = await context.Members
                            .Where(m => m.AssignedPackageId != null)
                            .GroupBy(m => m.AssignedPackageId)
                            .Select(g => new { PackageId = g.Key, Count = g.Count() })
                            .ToListAsync();

                        // 3. Map counts to packages
                        foreach (var p in packages)
                        {
                            var countObj = memberCounts.FirstOrDefault(c => c.PackageId == p.PackageId);
                            p.MemberCount = countObj?.Count ?? 0;
                        }

                        // 4. Calculate Stats
                        var totalPackages = packages.Count(p => p.IsActive);
                        var mostPopular = packages.OrderByDescending(p => p.MemberCount).FirstOrDefault();

                        return new 
                        { 
                            Packages = packages, 
                            Stats = new { Total = totalPackages, Popular = mostPopular?.PackageName ?? "None" } 
                        };
                    }
                });

                // Update UI
                dgPackages.ItemsSource = result.Packages;
                
                if (txtTotalPackages != null) txtTotalPackages.Text = result.Stats.Total.ToString();
                if (txtMostPopular != null) txtMostPopular.Text = result.Stats.Popular;
            }
            catch (Exception ex)
            {
                // Ignore Oracle cancellation
                if (ex.Message.Contains("ORA-01013")) return;

                MessageBox.Show($"Error loading packages: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddPackage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditPackageDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadPackages();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int packageId)
            {
                var dialog = new AddEditPackageDialog(packageId);
                if (dialog.ShowDialog() == true)
                {
                    LoadPackages();
                }
            }
        }
        
        private void btnViewMembers_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int packageId)
            {
                var dialog = new PackageMembersDialog(packageId);
                dialog.ShowDialog();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPackages();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int packageId)
            {
                await HandleDeletePackage(packageId);
            }
        }

        private async Task HandleDeletePackage(int packageId)
        {
            try
            {
                using (var context = new GymDbContext())
                {
                    // Check if package is in use
                    var activeMembersCount = await context.Members.CountAsync(m => m.AssignedPackageId == packageId);
                    
                    if (activeMembersCount > 0)
                    {
                        MessageBox.Show(
                            $"Cannot delete this package because it is currently assigned to {activeMembersCount} member(s).\n\n" +
                            "Please reassign these members to a different package before deleting.",
                            "Cannot Delete Package",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Check for past payments linked to this package (optional: block or cascade?)
                    // Usually safer to not delete if historical data exists, but user might want to anyway.
                    // For now, let's warn if history exists.
                    var historyCount = await context.Payments.CountAsync(p => p.PackageId == packageId);

                    string prompt = "Are you sure you want to delete this package?";
                    if (historyCount > 0)
                    {
                        prompt += $"\n\nNote: There are {historyCount} past payment records linked to this package. " +
                                  "Deleting it might affect historical reports.";
                    }

                    var result = MessageBox.Show(prompt, "Confirm Delete", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var package = await context.MembershipPackages.FindAsync(packageId);
                        if (package != null)
                        {
                            context.MembershipPackages.Remove(package);
                            await context.SaveChangesAsync();
                            
                            MessageBox.Show("Package deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadPackages();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting package: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
