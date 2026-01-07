using Microsoft.EntityFrameworkCore;
using GymManagementSystem.Models;
using System;

namespace GymManagementSystem.Data
{
    public class GymDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MembershipPackage> MembershipPackages { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<BiometricDevice> BiometricDevices { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Using Neon PostgreSQL Database with optimized settings
                optionsBuilder.UseNpgsql(
                    "Host=ep-spring-hill-a4gxrjyd-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_biCL6PqYxl3Q;SSL Mode=Require;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;Connection Idle Lifetime=300;Connection Pruning Interval=10;Command Timeout=30",
                    options => options.CommandTimeout(30));
                
                // Disable change tracking for read-only queries (improves performance)
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed default admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    FullName = "System Administrator",
                    Role = "Admin",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Seed default membership packages
            modelBuilder.Entity<MembershipPackage>().HasData(
                new MembershipPackage
                {
                    PackageId = 1,
                    PackageName = "Monthly",
                    DurationMonths = 1,
                    Price = 3000m,
                    Description = "1 Month Membership Package",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new MembershipPackage
                {
                    PackageId = 2,
                    PackageName = "Quarterly (3 Months)",
                    DurationMonths = 3,
                    Price = 8000m,
                    Description = "3 Months Membership Package with discount",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new MembershipPackage
                {
                    PackageId = 3,
                    PackageName = "Half-Yearly (6 Months)",
                    DurationMonths = 6,
                    Price = 15000m,
                    Description = "6 Months Membership Package with better discount",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new MembershipPackage
                {
                    PackageId = 4,
                    PackageName = "Yearly (12 Months)",
                    DurationMonths = 12,
                    Price = 28000m,
                    Description = "12 Months Membership Package with best discount",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Configure relationships
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Member)
                .WithMany(m => m.Payments)
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Member)
                .WithMany(m => m.Attendances)
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Member>()
                .HasOne(m => m.AssignedPackage)
                .WithMany()
                .HasForeignKey(m => m.AssignedPackageId)
                .OnDelete(DeleteBehavior.SetNull);

            // Create indexes for performance optimization
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.FullName)
                .HasDatabaseName("IX_Members_FullName");
            
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.PhoneNumber)
                .HasDatabaseName("IX_Members_PhoneNumber");
            
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.NIC)
                .HasDatabaseName("IX_Members_NIC");
            
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.AssignedPackageId)
                .HasDatabaseName("IX_Members_AssignedPackageId");
            
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.IsActive)
                .HasDatabaseName("IX_Members_IsActive");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.MemberId, p.PackageId })
                .HasDatabaseName("IX_Payments_MemberId_PackageId");
            
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.NextDueDate)
                .HasDatabaseName("IX_Payments_NextDueDate");
            
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.EndDate)
                .HasDatabaseName("IX_Payments_EndDate");

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => a.CheckInDate)
                .HasDatabaseName("IX_Attendance_CheckInDate");
            
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => a.MemberId)
                .HasDatabaseName("IX_Attendance_MemberId");
        }
    }
}
