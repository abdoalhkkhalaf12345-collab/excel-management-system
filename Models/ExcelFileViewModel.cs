using System.Collections.Generic;

namespace Abdullhak_Khalaf.Models
{
    public class ExcelFileViewModel
    {
        public string FileName { get; set; } = string.Empty;
        public List<List<string>> Rows { get; set; } = new List<List<string>>();
    }
}