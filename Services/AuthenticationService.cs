using System;
using System.Linq;
using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Services
{
    public class AuthenticationService
    {
        private static bool _dbInitialized = false;
        private static readonly object _initLock = new object();

        private void EnsureDatabaseInitialized()
        {
            if (_dbInitialized) return;

            lock (_initLock)
            {
                if (_dbInitialized) return;

                try
                {
                    using (var context = new GymDbContext())
                    {
                        // Create database and tables if they don't exist
                        context.Database.EnsureCreated();
                        
                        // Check if admin user exists
                        bool hasAdmin = context.Users.Any(u => u.Username == "admin");
                        
                        if (!hasAdmin)
                        {
                            // Seed default data
                            InitializeDatabase(context);
                        }

                        _dbInitialized = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                    // Don't throw - allow retry on next login attempt
                }
            }
        }

        private void InitializeDatabase(GymDbContext context)
        {
            // Seed admin user (password: admin123)
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FullName = "Administrator",
                Role = "Admin",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Users.Add(adminUser);

            // Seed membership packages
            var packages = new[]
            {
                new MembershipPackage { PackageName = "Monthly", DurationMonths = 1, Price = 3000, Description = "1 Month Membership Package", IsActive = true, CreatedDate = DateTime.Now },
                new MembershipPackage { PackageName = "Quarterly (3 Months)", DurationMonths = 3, Price = 8000, Description = "3 Months Membership Package", IsActive = true, CreatedDate = DateTime.Now },
                new MembershipPackage { PackageName = "Half-Yearly (6 Months)", DurationMonths = 6, Price = 15000, Description = "6 Months Membership Package with discount", IsActive = true, CreatedDate = DateTime.Now },
                new MembershipPackage { PackageName = "Yearly (12 Months)", DurationMonths = 12, Price = 28000, Description = "12 Months Membership Package with best discount", IsActive = true, CreatedDate = DateTime.Now }
            };
            context.MembershipPackages.AddRange(packages);

            context.SaveChanges();
        }

        public bool ValidateUser(string username, string password, out User? user)
        {
            user = null;
            
            // Ensure database is initialized
            EnsureDatabaseInitialized();
            
            try
            {
                using (var context = new GymDbContext())
                {
                    user = context.Users
                        .AsNoTracking()
                        .FirstOrDefault(u => u.Username == username);
                    
                    if (user == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"User not found: {username}");
                        return false;
                    }
                    
                    // Check if user is active
                    if (!user.IsActive)
                    {
                        System.Diagnostics.Debug.WriteLine($"User inactive: {username}");
                        return false;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Found user: {username}");
                    System.Diagnostics.Debug.WriteLine($"Stored hash: {user.PasswordHash}");
                    
                    // Verify password
                    bool isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    
                    System.Diagnostics.Debug.WriteLine($"Password valid: {isValidPassword}");
                    
                    if (isValidPassword)
                    {
                        // Update last login date in a separate tracked context
                        using (var updateContext = new GymDbContext())
                        {
                            var userToUpdate = updateContext.Users.Find(user.UserId);
                            if (userToUpdate != null)
                            {
                                userToUpdate.LastLoginDate = DateTime.UtcNow;
                                updateContext.SaveChanges();
                            }
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                System.Windows.MessageBox.Show($"Authentication error: {ex.Message}\n\n{ex.StackTrace}", "Debug Info");
            }
            
            return false;
        }

        public bool CreateUser(string username, string password, string fullName, string role)
        {
            using (var context = new GymDbContext())
            {
                // Check if username already exists (avoid Any() to prevent boolean literal generation)
                if (context.Users.FirstOrDefault(u => u.Username == username) != null)
                    return false;
                
                var newUser = new User
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    FullName = fullName,
                    Role = role,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                
                context.Users.Add(newUser);
                context.SaveChanges();
                return true;
            }
        }
    }
}
