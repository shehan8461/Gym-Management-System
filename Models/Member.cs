using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementSystem.Models
{
    public class Member
    {
        [Key]
        public int MemberId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string NIC { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? PhotoPath { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        
        [MaxLength(500)]
        public string? Address { get; set; }
        
        [MaxLength(100)]
        public string? Email { get; set; }
        
        [MaxLength(10)]
        public string? Gender { get; set; }
        
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Fingerprint data for biometric
        public byte[]? FingerprintTemplate { get; set; }
        
        public int? BiometricDeviceId { get; set; }
        
        // Navigation properties
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
