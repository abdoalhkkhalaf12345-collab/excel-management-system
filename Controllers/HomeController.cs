using Abdullhak_Khalaf.Data;
using Abdullhak_Khalaf.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Abdullhak_Khalaf.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly string[] _allowedExtensions = new[] { ".xls", ".xlsx", ".csv" };

        public HomeController(
            IWebHostEnvironment env,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _env = env;
            _context = context;
            _userManager = userManager;
        }

        private string GetUploadsPath()
        {
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            return uploadPath;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        private async Task<AppFile?> GetAccessibleFileAsync(int id)
        {
            var userId = GetCurrentUserId();

            if (IsAdmin())
            {
                return await _context.AppFiles.FirstOrDefaultAsync(f => f.Id == id);
            }

            return await _context.AppFiles.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == userId);
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "لوحة التحكم";

            List<AppFile> files;

            if (IsAdmin())
            {
                files = await _context.AppFiles
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();

                ViewBag.IsAdmin = true;
            }
            else
            {
                var userId = GetCurrentUserId();

                files = await _context.AppFiles
                    .Where(f => f.OwnerUserId == userId)
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();

                ViewBag.IsAdmin = false;
            }

            return View(files);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile UploadFile)
        {
            if (UploadFile == null || UploadFile.Length == 0)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "الرجاء اختيار ملف صالح.";
                return RedirectToAction("Index");
            }

            var originalFileName = Path.GetFileName(UploadFile.FileName).Trim();
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (!_allowedExtensions.Contains(extension))
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "يسمح فقط بملفات Excel و CSV.";
                return RedirectToAction("Index");
            }

            var uploadsPath = GetUploadsPath();
            string storedFileName;
            string fileType;
            long fileSize = UploadFile.Length;

            if (extension == ".csv")
            {
                storedFileName = $"{Guid.NewGuid()}.xlsx";
                fileType = "csv";

                var filePath = Path.Combine(uploadsPath, storedFileName);

                using var stream = UploadFile.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();

                var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                                   .Where(x => !string.IsNullOrWhiteSpace(x))
                                   .ToList();

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Sheet1");

                for (int rowIndex = 0; rowIndex < lines.Count; rowIndex++)
                {
                    var columns = ParseCsvLine(lines[rowIndex]);

                    for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                    {
                        worksheet.Cell(rowIndex + 1, colIndex + 1).Value = columns[colIndex];
                    }
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
            else
            {
                storedFileName = $"{Guid.NewGuid()}{extension}";
                fileType = extension.Replace(".", "");
                var filePath = Path.Combine(uploadsPath, storedFileName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                await UploadFile.CopyToAsync(fileStream);
            }

            var appFile = new AppFile
            {
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                OwnerUserId = GetCurrentUserId(),
                FileType = fileType,
                FileSizeBytes = fileSize,
                UploadedAt = DateTime.UtcNow
            };

            _context.AppFiles.Add(appFile);
            await _context.SaveChangesAsync();

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "تم رفع الملف بنجاح.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> EditFile(int id, string? sheetName = null)
        {
            var fileRecord = await GetAccessibleFileAsync(id);

            if (fileRecord == null)
                return NotFound("الملف غير موجود أو ليس لديك صلاحية الوصول إليه.");

            var filePath = Path.Combine(GetUploadsPath(), fileRecord.StoredFileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("الملف غير موجود على الخادم.");

            using var workbook = new XLWorkbook(filePath);

            var allSheetNames = workbook.Worksheets.Select(w => w.Name).ToList();
            var selectedSheetName = string.IsNullOrWhiteSpace(sheetName)
                ? workbook.Worksheet(1).Name
                : sheetName;

            if (!allSheetNames.Contains(selectedSheetName))
                selectedSheetName = workbook.Worksheet(1).Name;

            var worksheet = workbook.Worksheet(selectedSheetName);
            var usedRange = worksheet.RangeUsed();

            var model = new ExcelEditorViewModel
            {
                FileId = fileRecord.Id,
                FileName = fileRecord.OriginalFileName,
                SheetName = selectedSheetName,
                SheetNames = allSheetNames,
                IsAdminView = IsAdmin()
            };

            if (usedRange == null)
                return View(model);

            int lastRow = usedRange.RowCount();
            int lastColumn = usedRange.ColumnCount();

            for (int col = 1; col <= lastColumn; col++)
            {
                model.Headers.Add(worksheet.Cell(1, col).GetValue<string>());
            }

            for (int row = 2; row <= lastRow; row++)
            {
                var rowData = new List<string>();

                for (int col = 1; col <= lastColumn; col++)
                {
                    rowData.Add(worksheet.Cell(row, col).GetValue<string>());
                }

                model.Rows.Add(rowData);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEditedExcel([FromBody] ExcelSaveRequest request)
        {
            if (request == null || request.FileId <= 0 || string.IsNullOrWhiteSpace(request.SheetName))
            {
                return BadRequest(new { success = false, message = "البيانات غير مكتملة." });
            }

            var fileRecord = await GetAccessibleFileAsync(request.FileId);

            if (fileRecord == null)
            {
                return NotFound(new { success = false, message = "الملف غير موجود أو لا تملك صلاحية الوصول إليه." });
            }

            var filePath = Path.Combine(GetUploadsPath(), fileRecord.StoredFileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { success = false, message = "الملف غير موجود على الخادم." });
            }

            using var workbook = new XLWorkbook(filePath);

            if (!workbook.Worksheets.Any(w => w.Name == request.SheetName))
            {
                return NotFound(new { success = false, message = "الورقة المحددة غير موجودة." });
            }

            var worksheet = workbook.Worksheet(request.SheetName);
            worksheet.Clear();

            for (int col = 0; col < request.Headers.Count; col++)
            {
                worksheet.Cell(1, col + 1).Value = request.Headers[col];
                worksheet.Cell(1, col + 1).Style.Font.Bold = true;
                worksheet.Cell(1, col + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1D4ED8");
                worksheet.Cell(1, col + 1).Style.Font.FontColor = XLColor.White;
            }

            for (int row = 0; row < request.Rows.Count; row++)
            {
                for (int col = 0; col < request.Rows[row].Count; col++)
                {
                    worksheet.Cell(row + 2, col + 1).Value = request.Rows[row][col];
                }
            }

            worksheet.Columns().AdjustToContents();
            workbook.Save();

            return Json(new { success = true, message = "تم حفظ التعديلات بنجاح." });
        }

        public async Task<IActionResult> DownloadFile(int id)
        {
            var fileRecord = await GetAccessibleFileAsync(id);

            if (fileRecord == null)
                return NotFound("الملف غير موجود أو لا تملك صلاحية الوصول إليه.");

            var filePath = Path.Combine(GetUploadsPath(), fileRecord.StoredFileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("الملف غير موجود على الخادم.");

            var extension = Path.GetExtension(fileRecord.StoredFileName).ToLowerInvariant();
            var contentType = extension == ".xls"
                ? "application/vnd.ms-excel"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return PhysicalFile(filePath, contentType, fileRecord.OriginalFileName);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var fileRecord = await GetAccessibleFileAsync(id);

            if (fileRecord == null)
                return NotFound(new { success = false, message = "الملف غير موجود أو لا تملك صلاحية الوصول إليه." });

            var filePath = Path.Combine(GetUploadsPath(), fileRecord.StoredFileName);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.AppFiles.Remove(fileRecord);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "تم حذف الملف بنجاح." });
        }

        public async Task<IActionResult> ExportPdf(int id, string? sheetName = null)
        {
            var fileRecord = await GetAccessibleFileAsync(id);

            if (fileRecord == null)
                return NotFound("الملف غير موجود أو لا تملك صلاحية الوصول إليه.");

            var filePath = Path.Combine(GetUploadsPath(), fileRecord.StoredFileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("الملف غير موجود على الخادم.");

            using var workbook = new XLWorkbook(filePath);

            var selectedSheetName = string.IsNullOrWhiteSpace(sheetName)
                ? workbook.Worksheet(1).Name
                : sheetName;

            if (!workbook.Worksheets.Any(w => w.Name == selectedSheetName))
                selectedSheetName = workbook.Worksheet(1).Name;

            var worksheet = workbook.Worksheet(selectedSheetName);
            var usedRange = worksheet.RangeUsed();

            var headers = new List<string>();
            var rows = new List<List<string>>();

            if (usedRange != null)
            {
                int lastRow = usedRange.RowCount();
                int lastColumn = usedRange.ColumnCount();

                for (int col = 1; col <= lastColumn; col++)
                {
                    headers.Add(worksheet.Cell(1, col).GetValue<string>());
                }

                for (int row = 2; row <= lastRow; row++)
                {
                    var rowData = new List<string>();
                    for (int col = 1; col <= lastColumn; col++)
                    {
                        rowData.Add(worksheet.Cell(row, col).GetValue<string>());
                    }
                    rows.Add(rowData);
                }
            }

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"اسم الملف: {fileRecord.OriginalFileName}").Bold().FontSize(16);
                        col.Item().Text($"اسم الورقة: {selectedSheetName}");
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        int columnsCount = Math.Max(headers.Count, 1);

                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < columnsCount; i++)
                                columns.RelativeColumn();
                        });

                        if (headers.Any())
                        {
                            foreach (var header in headers)
                            {
                                table.Cell().Background("#1D4ED8").Padding(6).Text(header).FontColor(Colors.White).Bold();
                            }

                            foreach (var row in rows)
                            {
                                foreach (var cell in row)
                                {
                                    table.Cell().Border(1).BorderColor("#D1D5DB").Padding(5).Text(cell ?? string.Empty);
                                }
                            }
                        }
                        else
                        {
                            table.Cell().Border(1).Padding(6).Text("لا توجد بيانات داخل هذه الورقة.");
                        }
                    });

                    page.Footer().AlignCenter().Text("تم إنشاء الملف من النظام");
                });
            }).GeneratePdf();

            var pdfName = Path.GetFileNameWithoutExtension(fileRecord.OriginalFileName) + ".pdf";
            return File(pdfBytes, "application/pdf", pdfName);
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line))
            {
                result.Add(string.Empty);
                return result;
            }

            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result;
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}