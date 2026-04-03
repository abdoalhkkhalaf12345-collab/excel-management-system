using System;

namespace Abdullhak_Khalaf.Models
{
    public class ExcelFileViewModel
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public string OwnerEmail { get; set; } = string.Empty;
    }
}