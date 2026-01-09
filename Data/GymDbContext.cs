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
                // Using Oracle Autonomous Database
                optionsBuilder.UseOracle(
                    "User Id=ADMIN;Password=Shehan19999@;Data Source=(description= (retry_count=20)(retry_delay=3)(address=(protocol=tcps)(port=1522)(host=adb.ap-tokyo-1.oraclecloud.com))(connect_data=(service_name=gb5de3f0b70bf26_gymdb01_high.adb.oraclecloud.com))(security=(ssl_server_dn_match=yes)))",
                    options => 
                    {
                        options.CommandTimeout(30);
                        options.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19); // Enforce 19c compatibility
                    });
                
                // Disable change tracking for read-only queries (improves performance)
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            // Oracle specific type mappings to fix ORA-00902
            
            // Map Decimals to NUMBER(18,2)
            modelBuilder.Entity<Member>()
                .Property(m => m.CustomPackageAmount)
                .HasColumnType("NUMBER(18,2)");

            modelBuilder.Entity<MembershipPackage>()
                .Property(m => m.Price)
                .HasColumnType("NUMBER(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("NUMBER(18,2)");

            // Map TimeSpan to Int64 (Ticks) to avoid INTERVAL data type issues
            modelBuilder.Entity<Attendance>()
                .Property(a => a.CheckInTime)
                .HasConversion(
                    v => v.Ticks,
                    v => TimeSpan.FromTicks(v))
                .HasColumnType("NUMBER(19)"); // Long

            modelBuilder.Entity<Attendance>()
                .Property(a => a.CheckOutTime)
                .HasConversion(
                    v => v != null ? v.Value.Ticks : (long?)null,
                    v => v.HasValue ? TimeSpan.FromTicks(v.Value) : (TimeSpan?)null)
                .HasColumnType("NUMBER(19)"); // Long

            // Map byte[] to BLOB explicitly
            modelBuilder.Entity<Member>()
                .Property(m => m.FingerprintTemplate)
                .HasColumnType("BLOB");

            // Map Booleans to NUMBER(1) with conversion - Oracle compatible
            modelBuilder.Entity<User>().Property(u => u.IsActive)
                .HasColumnType("NUMBER(1)");
                
            modelBuilder.Entity<Member>().Property(m => m.IsActive)
                .HasColumnType("NUMBER(1)");
                
            modelBuilder.Entity<MembershipPackage>().Property(m => m.IsActive)
                .HasColumnType("NUMBER(1)");
                
            modelBuilder.Entity<BiometricDevice>().Property(b => b.IsActive)
                .HasColumnType("NUMBER(1)");
                
            modelBuilder.Entity<BiometricDevice>().Property(b => b.IsConnected)
                .HasColumnType("NUMBER(1)");
        }
    }
}
