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
    }
}
