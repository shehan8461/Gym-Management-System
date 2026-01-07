using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;
using GymManagementSystem.Models;

namespace GymManagementSystem
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                // Apply database migrations first
                DatabaseMigration.ApplyMigrations();
                
                // Initialize database
                using (var context = new GymDbContext())
                {
                    context.Database.EnsureCreated();
                    
                    // Seed admin user if not exists
                    if (!context.Users.Any())
                    {
                        context.Users.Add(new User
                        {
                            Username = "admin",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                            FullName = "System Administrator",
                            Role = "Admin",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        });
                        context.SaveChanges();
                    }
                    
                    // Seed membership packages if not exists
                    if (!context.MembershipPackages.Any())
                    {
                        context.MembershipPackages.AddRange(
                            new MembershipPackage
                            {
                                PackageName = "Monthly",
                                DurationMonths = 1,
                                Price = 3000m,
                                Description = "1 Month Membership Package",
                                IsActive = true,
                                CreatedDate = DateTime.UtcNow
                            },
                            new MembershipPackage
                            {
                                PackageName = "Quarterly (3 Months)",
                                DurationMonths = 3,
                                Price = 8000m,
                                Description = "3 Months Membership Package with discount",
                                IsActive = true,
                                CreatedDate = DateTime.UtcNow
                            },
                            new MembershipPackage
                            {
                                PackageName = "Half-Yearly (6 Months)",
                                DurationMonths = 6,
                                Price = 15000m,
                                Description = "6 Months Membership Package with better discount",
                                IsActive = true,
                                CreatedDate = DateTime.UtcNow
                            },
                            new MembershipPackage
                            {
                                PackageName = "Yearly (12 Months)",
                                DurationMonths = 12,
                                Price = 28000m,
                                Description = "12 Months Membership Package with best discount",
                                IsActive = true,
                                CreatedDate = DateTime.UtcNow
                            }
                        );
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize database:\n\n{ex.Message}\n\nPlease contact support.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Shutdown();
            }
        }
        
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will now close.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            e.Handled = true;
            Shutdown();
        }
    }
}
