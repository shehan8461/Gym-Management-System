using System;

namespace GymManagementSystem.Services
{
    public static class SessionManager
    {
        public static int? CurrentUserId { get; private set; }
        public static string? CurrentUsername { get; private set; }
        public static string? CurrentUserRole { get; private set; }
        public static string? CurrentUserFullName { get; private set; }
        public static DateTime? LoginTime { get; private set; }

        public static bool IsLoggedIn => CurrentUserId.HasValue;
        public static bool IsAdmin => CurrentUserRole == "Admin";

        public static void Login(int userId, string username, string role, string fullName)
        {
            CurrentUserId = userId;
            CurrentUsername = username;
            CurrentUserRole = role;
            CurrentUserFullName = fullName;
            LoginTime = DateTime.UtcNow;
        }

        public static void Logout()
        {
            CurrentUserId = null;
            CurrentUsername = null;
            CurrentUserRole = null;
            CurrentUserFullName = null;
            LoginTime = null;
        }
    }
}
