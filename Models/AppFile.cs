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
        public string FileType { get; set; } = string.Empty; // xlsx / xls / csv

        public long FileSizeBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}