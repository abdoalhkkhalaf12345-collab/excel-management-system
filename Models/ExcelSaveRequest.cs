using System.Collections.Generic;

namespace Abdullhak_Khalaf.Models
{
    public class ExcelSaveRequest
    {
        public int FileId { get; set; }
        public string SheetName { get; set; } = string.Empty;
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }
}