using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementSystem.Models
{
    public class MembershipPackage
    {
        [Key]
        public int PackageId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string PackageName { get; set; } = string.Empty;
        
        public int DurationMonths { get; set; } // 1, 3, 6, 12
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
