using Abdullhak_Khalaf.Models;
using ClosedXML.Excel;
using System.Text;

namespace Abdullhak_Khalaf.Services
{
    public static class FileParsingService
    {
        public static ExcelEditorViewModel ReadFile(string filePath, string fileName, int fileId, string fileType)
        {
            return fileType.ToLower() switch
            {
                "csv" => ReadCsv(filePath, fileName, fileId),
                "xlsx" => ReadExcel(filePath, fileName, fileId),
                "xls" => ReadExcel(filePath, fileName, fileId),
                _ => throw new Exception("نوع الملف غير مدعوم")
            };
        }

        public static ExcelEditorViewModel ReadCsv(string filePath, string fileName, int fileId)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();

            var model = new ExcelEditorViewModel
            {
                FileId = fileId,
                FileName = fileName,
                SheetName = "CSV",
                IsCsv = true
            };

            if (!lines.Any())
                return model;

            model.Headers = lines.First().Split(',').Select(x => x.Trim()).ToList();

            foreach (var line in lines.Skip(1))
            {
                model.Rows.Add(line.Split(',').Select(x => x.Trim()).ToList());
            }

            return model;
        }

        public static ExcelEditorViewModel ReadExcel(string filePath, string fileName, int fileId)
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.First();

            var model = new ExcelEditorViewModel
            {
                FileId = fileId,
                FileName = fileName,
                SheetName = ws.Name,
                IsCsv = false
            };

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

            if (lastRow == 0 || lastCol == 0)
                return model;

            for (int c = 1; c <= lastCol; c++)
            {
                model.Headers.Add(ws.Cell(1, c).GetValue<string>());
            }

            for (int r = 2; r <= lastRow; r++)
            {
                var row = new List<string>();
                for (int c = 1; c <= lastCol; c++)
                {
                    row.Add(ws.Cell(r, c).GetValue<string>());
                }
                model.Rows.Add(row);
            }

            return model;
        }

        public static void SaveCsv(string filePath, ExcelSaveRequest request)
        {
            var lines = new List<string>();

            lines.Add(string.Join(",", request.Headers.Select(EscapeCsv)));

            foreach (var row in request.Rows)
            {
                lines.Add(string.Join(",", row.Select(EscapeCsv)));
            }

            File.WriteAllLines(filePath, lines, Encoding.UTF8);
        }

        public static void SaveExcel(string filePath, ExcelSaveRequest request)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(request.SheetName) ? "Sheet1" : request.SheetName);

            for (int c = 0; c < request.Headers.Count; c++)
            {
                ws.Cell(1, c + 1).Value = request.Headers[c];
                ws.Cell(1, c + 1).Style.Font.Bold = true;
            }

            for (int r = 0; r < request.Rows.Count; r++)
            {
                for (int c = 0; c < request.Rows[r].Count; c++)
                {
                    ws.Cell(r + 2, c + 1).Value = request.Rows[r][c];
                }
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private static string EscapeCsv(string value)
        {
            value ??= string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }
    }
}