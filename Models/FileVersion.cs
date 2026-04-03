using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abdullhak_Khalaf.Models
{
    public class FileVersion
    {
        public int Id { get; set; }

        [Required]
        public int AppFileId { get; set; }

        [ForeignKey(nameof(AppFileId))]
        public AppFile? AppFile { get; set; }

        [Required]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        public string VersionLabel { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        [Required]
        public string CreatedByUserName { get; set; } = string.Empty;

        [Required]
        public string CreatedByFullName { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }
    }
}