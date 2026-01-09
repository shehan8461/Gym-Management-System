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
                // Apply database migrations first
                // Apply database migrations first
                // DatabaseMigration.ApplyMigrations(); // Removed for Oracle migration
                
                // Initialize database
                using (var context = new GymDbContext())
                {
                    // Check if tables exist, only create if they don't
                    bool tablesExist = false;
                    try
                    {
                        // Try to query Users table to check if database is initialized
                        context.Database.ExecuteSqlRaw("SELECT 1 FROM \"Users\" WHERE ROWNUM = 1");
                        tablesExist = true;
                    }
                    catch
                    {
                        tablesExist = false;
                    }

                    // Only create tables if they don't exist
                    if (!tablesExist)
                    {
                        // Drop all existing tables first to ensure clean schema (only on first run)
                        try { context.Database.ExecuteSqlRaw("DROP TABLE \"Attendances\" CASCADE CONSTRAINTS"); } catch { }
                        try { context.Database.ExecuteSqlRaw("DROP TABLE \"Payments\" CASCADE CONSTRAINTS"); } catch { }
                        try { context.Database.ExecuteSqlRaw("DROP TABLE \"Members\" CASCADE CONSTRAINTS"); } catch { }
                        try { context.Database.ExecuteSqlRaw("DROP TABLE \"BiometricDevices\" CASCADE CONSTRAINTS"); } catch { }
                        try { context.Database.ExecuteSqlRaw("DROP TABLE \"MembershipPackages\" CASCADE CONSTRAINTS"); } catch { }
                        try { context.Database.ExecuteSqlRaw("DROP TABLE \"Users\" CASCADE CONSTRAINTS"); } catch { }
                    
                        // Now create tables with proper Oracle syntax
                    try
                    {
                        context.Database.ExecuteSqlRaw(@"
                            CREATE TABLE ""Users"" (
                                ""UserId"" NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                                ""Username"" NVARCHAR2(50) NOT NULL,
                                ""PasswordHash"" NVARCHAR2(255) NOT NULL,
                                ""FullName"" NVARCHAR2(100) NOT NULL,
                                ""Role"" NVARCHAR2(50) DEFAULT 'Staff' NOT NULL,
                                ""IsActive"" NUMBER(1) DEFAULT 1 NOT NULL,
                                ""CreatedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                                ""LastLoginDate"" TIMESTAMP NULL
                            )");
                    }
                    catch { } // Table might already exist
                    
                    try
                    {
                        context.Database.ExecuteSqlRaw(@"
                            CREATE TABLE ""MembershipPackages"" (
                                ""PackageId"" NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                                ""PackageName"" NVARCHAR2(100) NOT NULL,
                                ""DurationMonths"" NUMBER(10) NOT NULL,
                                ""Price"" NUMBER(18,2) NOT NULL,
                                ""Description"" NVARCHAR2(500) NULL,
                                ""IsActive"" NUMBER(1) DEFAULT 1 NOT NULL,
                                ""CreatedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
                            )");
                    }
                    catch { }
                    
                    try
                    {
                        context.Database.ExecuteSqlRaw(@"
                            CREATE TABLE ""Members"" (
                                ""MemberId"" NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                                ""FullName"" NVARCHAR2(100) NOT NULL,
                                ""PhoneNumber"" NVARCHAR2(20) NOT NULL,
                                ""NIC"" NVARCHAR2(20) NOT NULL,
                                ""PhotoPath"" NVARCHAR2(500) NULL,
                                ""DateOfBirth"" TIMESTAMP NOT NULL,
                                ""Address"" NVARCHAR2(500) NULL,
                                ""Email"" NVARCHAR2(100) NULL,
                                ""Gender"" NVARCHAR2(10) NULL,
                                ""RegistrationDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                                ""IsActive"" NUMBER(1) DEFAULT 1 NOT NULL,
                                ""FingerprintTemplate"" BLOB NULL,
                                ""BiometricDeviceId"" NUMBER(10) NULL,
                                ""AssignedPackageId"" NUMBER(10) NULL,
                                ""CustomPackageAmount"" NUMBER(18,2) NULL
                            )");
                    }
                    catch { }
                    
                    try
                    {
                        context.Database.ExecuteSqlRaw(@"
                            CREATE TABLE ""Payments"" (
                                ""PaymentId"" NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                                ""MemberId"" NUMBER(10) NOT NULL,
                                ""PackageId"" NUMBER(10) NOT NULL,
                                ""Amount"" NUMBER(18,2) NOT NULL,
                                ""PaymentDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                                ""StartDate"" TIMESTAMP NOT NULL,
                                ""EndDate"" TIMESTAMP NOT NULL,
                                ""NextDueDate"" TIMESTAMP NOT NULL,
                                ""PaymentStatus"" NVARCHAR2(50) DEFAULT 'Paid' NOT NULL,
                                ""PaymentMethod"" NVARCHAR2(50) DEFAULT 'Cash' NOT NULL,
                                ""Remarks"" NVARCHAR2(500) NULL,
                                ""ProcessedByUserId"" NUMBER(10) NULL
                            )");
                    }
                    catch { }
                    
                    try
                    {
                        context.Database.ExecuteSqlRaw(@"
                            CREATE TABLE ""Attendances"" (
                                ""AttendanceId"" NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                                ""MemberId"" NUMBER(10) NOT NULL,
                                ""CheckInDate"" TIMESTAMP NOT NULL,
                                ""CheckInTime"" NUMBER(19) NOT NULL,
                                ""CheckOutDate"" TIMESTAMP NULL,
                                ""CheckOutTime"" NUMBER(19) NULL,
                                ""AttendanceType"" NVARCHAR2(50) DEFAULT 'Manual' NOT NULL,
                                ""RecordedByUserId"" NUMBER(10) NULL,
                                ""Remarks"" NVARCHAR2(500) NULL
                            )");
                    }
                    catch { }
                    
                    try
                    {
                        context.Database.ExecuteSqlRaw(@"
                            CREATE TABLE ""BiometricDevices"" (
                                ""DeviceId"" NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                                ""DeviceName"" NVARCHAR2(100) NOT NULL,
                                ""IPAddress"" NVARCHAR2(50) NOT NULL,
                                ""Port"" NUMBER(10) DEFAULT 8000 NOT NULL,
                                ""Username"" NVARCHAR2(100) NOT NULL,
                                ""Password"" NVARCHAR2(2000) NOT NULL,
                                ""DeviceType"" NVARCHAR2(50) DEFAULT 'Hikvision' NOT NULL,
                                ""IsActive"" NUMBER(1) DEFAULT 1 NOT NULL,
                                ""IsConnected"" NUMBER(1) DEFAULT 0 NOT NULL,
                                ""LastConnectedDate"" TIMESTAMP NULL,
                                ""CreatedDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL
                            )");
                    }
                    catch { }
                    
                    } // End of if (!tablesExist)
                    
                    // Seed admin user using raw SQL - check with raw SQL to avoid ANY() generating TRUE/FALSE
                    try
                    {
                        var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                        context.Database.ExecuteSqlRaw(
                            "INSERT INTO \"Users\" (\"Username\", \"PasswordHash\", \"FullName\", \"Role\", \"IsActive\", \"CreatedDate\") " +
                            "SELECT {0}, {1}, {2}, {3}, 1, CURRENT_TIMESTAMP FROM DUAL " +
                            "WHERE NOT EXISTS (SELECT 1 FROM \"Users\" WHERE \"Username\" = {0})",
                            "admin", passwordHash, "System Administrator", "Admin");
                    }
                    catch { }
                    
                    // Seed membership packages using raw SQL - use INSERT ALL with NOT EXISTS check
                    try
                    {
                        context.Database.ExecuteSqlRaw(@"
                            INSERT ALL
                                WHEN NOT EXISTS (SELECT 1 FROM ""MembershipPackages"" WHERE ""PackageName"" = 'Monthly') THEN
                                    INTO ""MembershipPackages"" (""PackageName"", ""DurationMonths"", ""Price"", ""Description"", ""IsActive"", ""CreatedDate"")
                                    VALUES ('Monthly', 1, 3000, '1 Month Membership Package', 1, CURRENT_TIMESTAMP)
                                WHEN NOT EXISTS (SELECT 1 FROM ""MembershipPackages"" WHERE ""PackageName"" = 'Quarterly (3 Months)') THEN
                                    INTO ""MembershipPackages"" (""PackageName"", ""DurationMonths"", ""Price"", ""Description"", ""IsActive"", ""CreatedDate"")
                                    VALUES ('Quarterly (3 Months)', 3, 8000, '3 Months Membership Package with discount', 1, CURRENT_TIMESTAMP)
                                WHEN NOT EXISTS (SELECT 1 FROM ""MembershipPackages"" WHERE ""PackageName"" = 'Half-Yearly (6 Months)') THEN
                                    INTO ""MembershipPackages"" (""PackageName"", ""DurationMonths"", ""Price"", ""Description"", ""IsActive"", ""CreatedDate"")
                                    VALUES ('Half-Yearly (6 Months)', 6, 15000, '6 Months Membership Package with better discount', 1, CURRENT_TIMESTAMP)
                                WHEN NOT EXISTS (SELECT 1 FROM ""MembershipPackages"" WHERE ""PackageName"" = 'Yearly (12 Months)') THEN
                                    INTO ""MembershipPackages"" (""PackageName"", ""DurationMonths"", ""Price"", ""Description"", ""IsActive"", ""CreatedDate"")
                                    VALUES ('Yearly (12 Months)', 12, 28000, '12 Months Membership Package with best discount', 1, CURRENT_TIMESTAMP)
                            SELECT 1 FROM DUAL");
                    }
                    catch { }
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
