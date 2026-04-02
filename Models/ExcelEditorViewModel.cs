using System.Collections.Generic;

namespace Abdullhak_Khalaf.Models
{
    public class ExcelEditorViewModel
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public List<string> SheetNames { get; set; } = new();
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
        public bool IsAdminView { get; set; }
    }
}