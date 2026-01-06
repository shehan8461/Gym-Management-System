using System;
using System.Linq;
using GymManagementSystem.Data;
using GymManagementSystem.Models;

namespace GymManagementSystem.Services
{
    public class AuthenticationService
    {
        public bool ValidateUser(string username, string password, out User? user)
        {
            user = null;
            
            try
            {
                using (var context = new GymDbContext())
                {
                    user = context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);
                    
                    if (user == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"User not found: {username}");
                        return false;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Found user: {username}");
                    System.Diagnostics.Debug.WriteLine($"Stored hash: {user.PasswordHash}");
                    
                    // Verify password
                    bool isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    
                    System.Diagnostics.Debug.WriteLine($"Password valid: {isValidPassword}");
                    
                    if (isValidPassword)
                    {
                        // Update last login date
                        user.LastLoginDate = DateTime.UtcNow;
                        context.SaveChanges();
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
            try
            {
                using (var context = new GymDbContext())
                {
                    // Check if username already exists
                    if (context.Users.Any(u => u.Username == username))
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
            catch (Exception ex)
            {
                Console.WriteLine($"User creation error: {ex.Message}");
                return false;
            }
        }
    }
}
