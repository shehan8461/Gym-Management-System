using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        
        [ForeignKey("Member")]
        public int MemberId { get; set; }
        
        [ForeignKey("MembershipPackage")]
        public int PackageId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public DateTime NextDueDate { get; set; }
        
        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Paid"; // Paid, DueSoon, Overdue
        
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Online
        
        [MaxLength(500)]
        public string? Remarks { get; set; }
        
        public int? ProcessedByUserId { get; set; }
        
        // Navigation properties
        public virtual Member Member { get; set; } = null!;
        public virtual MembershipPackage MembershipPackage { get; set; } = null!;
    }
}
