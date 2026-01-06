using System;
using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Models
{
    public class BiometricDevice
    {
        [Key]
        public int DeviceId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string IPAddress { get; set; } = string.Empty;
        
        public int Port { get; set; } = 8000;
        
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string DeviceType { get; set; } = "Hikvision"; // Hikvision, ZKTeco, etc.
        
        public bool IsActive { get; set; } = true;
        
        public bool IsConnected { get; set; } = false;
        
        public DateTime? LastConnectedDate { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
