using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementSystem.Models
{
    /// <summary>
    /// Tracks each attempt to enroll a member's fingerprint on a biometric device.
    /// Used for summaries in the Members page and detailed history dialogs.
    /// </summary>
    public class FingerprintEnrollmentHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gym member ID (matches EmployeeNo on the Hikvision device).
        /// </summary>
        [Column("memberid")]
        public int MemberId { get; set; }

        /// <summary>
        /// Biometric device primary key from BiometricDevice.
        /// </summary>
        [Column("deviceid")]
        public int DeviceId { get; set; }

        /// <summary>
        /// UTC time when the attempt was made.
        /// </summary>
        [Column("enrollmenttimeutc")]
        public DateTime EnrollmentTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// High level result for UI: e.g. "Success", "Connection Failed", "User Create Failed".
        /// </summary>
        [MaxLength(100)]
        [Column("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Whether the attempt ultimately succeeded.
        /// </summary>
        [Column("issuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Optional detailed message or error returned from the device.
        /// </summary>
        [MaxLength(1000)]
        [Column("message")]
        public string? Message { get; set; }
    }
}

