using System.Collections.Generic;

namespace Abdullhak_Khalaf.Models
{
    public class ExcelEditorViewModel
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public bool IsCsv { get; set; }
        public bool IsAdminView { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }
}