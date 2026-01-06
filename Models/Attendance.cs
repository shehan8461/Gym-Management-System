using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementSystem.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }
        
        [ForeignKey("Member")]
        public int MemberId { get; set; }
        
        public DateTime CheckInDate { get; set; } = DateTime.UtcNow;
        
        public TimeSpan CheckInTime { get; set; }
        
        public DateTime? CheckOutDate { get; set; }
        
        public TimeSpan? CheckOutTime { get; set; }
        
        [MaxLength(50)]
        public string AttendanceType { get; set; } = "Manual"; // Manual, Biometric
        
        public int? RecordedByUserId { get; set; }
        
        [MaxLength(500)]
        public string? Remarks { get; set; }
        
        // Navigation property
        public virtual Member Member { get; set; } = null!;
    }
}
