using System;
using System.Linq;
using System.Windows;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                // Initialize database on application startup
                using (var context = new GymDbContext())
                {
                    // Create database and tables if they don't exist
                    context.Database.EnsureCreated();
                    
                    // Check if admin user exists and seed if needed
                    if (!context.Users.Any(u => u.Username == "admin"))
                    {
                        // Seed admin user (password: admin123)
                        var adminUser = new User
                        {
                            Username = "admin",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                            FullName = "Administrator",
                            Role = "Admin",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        };
                        context.Users.Add(adminUser);

                        // Seed membership packages
                        var packages = new[]
                        {
                            new MembershipPackage { PackageName = "Monthly", DurationMonths = 1, Price = 3000, Description = "1 Month Membership Package", IsActive = true, CreatedDate = DateTime.UtcNow },
                            new MembershipPackage { PackageName = "Quarterly (3 Months)", DurationMonths = 3, Price = 8000, Description = "3 Months Membership Package", IsActive = true, CreatedDate = DateTime.UtcNow },
                            new MembershipPackage { PackageName = "Half-Yearly (6 Months)", DurationMonths = 6, Price = 15000, Description = "6 Months Membership Package with discount", IsActive = true, CreatedDate = DateTime.UtcNow },
                            new MembershipPackage { PackageName = "Yearly (12 Months)", DurationMonths = 12, Price = 28000, Description = "12 Months Membership Package with best discount", IsActive = true, CreatedDate = DateTime.UtcNow }
                        };
                        context.MembershipPackages.AddRange(packages);

                        context.SaveChanges();
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("Database initialized successfully");
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? $"\n\nInner Exception: {ex.InnerException.Message}" : "";
                MessageBox.Show(
                    $"Database initialization error:\n\n{ex.Message}{innerMessage}\n\nStack: {ex.StackTrace}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
