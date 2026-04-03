using System;
using System.ComponentModel.DataAnnotations;

namespace Abdullhak_Khalaf.Models
{
    public class AppFile
    {
        public int Id { get; set; }

        [Required]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        public string OwnerUserId { get; set; } = string.Empty;

        [Required]
        public string OwnerEmail { get; set; } = string.Empty;

        [Required]
        public string OwnerUserName { get; set; } = string.Empty;

        [Required]
        public string OwnerFullName { get; set; } = string.Empty;

        [Required]
        public string FileType { get; set; } = string.Empty; // xlsx / csv

        public long FileSizeBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedAt { get; set; }
    }
}