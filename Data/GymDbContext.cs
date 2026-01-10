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
                // Neon PostgreSQL Database - Works from ANY PC without IP restrictions
                optionsBuilder.UseNpgsql(
                    "Host=ep-spring-hill-a4gxrjyd-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_biCL6PqYxl3Q;SSL Mode=Require;Trust Server Certificate=true");
                
                // Disable change tracking for read-only queries (improves performance)
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure PostgreSQL table names (lowercase)
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Member>().ToTable("members");
            modelBuilder.Entity<MembershipPackage>().ToTable("membershippackages");
            modelBuilder.Entity<Payment>().ToTable("payments");
            modelBuilder.Entity<Attendance>().ToTable("attendances");
            modelBuilder.Entity<BiometricDevice>().ToTable("biometricdevices");

            // Seed default admin user - Removed (handled in App.xaml.cs)
            // Seed default membership packages - Removed (handled in App.xaml.cs)

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

            // PostgreSQL uses native data types - no custom mappings needed
            // Decimals → numeric/decimal (automatic)
            // TimeSpan → interval (automatic)
            // bool → boolean (automatic)
            // byte[] → bytea (automatic)
        }
    }
}
