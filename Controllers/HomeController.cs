using Abdullhak_Khalaf.Data;
using Abdullhak_Khalaf.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Abdullhak_Khalaf.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        private const long MaxFileSize = 10 * 1024 * 1024;
        private readonly string[] AllowedExtensions = { ".xlsx", ".csv" };

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.AppFiles.AsQueryable();

            if (!isAdmin)
                query = query.Where(f => f.OwnerUserId == userId);

            var files = await query
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                var filtered = new List<AppFile>();

                foreach (var file in files)
                {
                    var matchedMeta =
                        file.OriginalFileName.ToLower().Contains(search) ||
                        file.FileType.ToLower().Contains(search) ||
                        file.OwnerFullName.ToLower().Contains(search) ||
                        file.OwnerUserName.ToLower().Contains(search) ||
                        file.OwnerEmail.ToLower().Contains(search);

                    if (matchedMeta)
                    {
                        filtered.Add(file);
                        continue;
                    }

                    var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.StoredFileName);
                    if (!System.IO.File.Exists(filePath))
                        continue;

                    bool contentMatched = file.FileType.ToLower() == "csv"
                        ? CsvContainsText(filePath, search)
                        : ExcelContainsText(filePath, search);

                    if (contentMatched)
                        filtered.Add(file);
                }

                files = filtered.OrderByDescending(f => f.UploadedAt).ToList();
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.Search = search ?? "";

            return View("Index", files);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> uploadFiles)
        {
            if (uploadFiles == null || uploadFiles.Count == 0)
            {
                TempData["Error"] = "يرجى اختيار ملف واحد على الأقل.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            int uploadedCount = 0;
            var errors = new List<string>();

            foreach (var uploadFile in uploadFiles)
            {
                if (uploadFile == null || uploadFile.Length == 0)
                {
                    errors.Add("تم تجاهل ملف فارغ.");
                    continue;
                }

                if (uploadFile.Length > MaxFileSize)
                {
                    errors.Add($"الملف {uploadFile.FileName} أكبر من 10MB.");
                    continue;
                }

                var extension = Path.GetExtension(uploadFile.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    errors.Add($"الملف {uploadFile.FileName} نوعه غير مدعوم.");
                    continue;
                }

                var storedFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, storedFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadFile.CopyToAsync(stream);
                }

                var appFile = new AppFile
                {
                    OriginalFileName = uploadFile.FileName,
                    StoredFileName = storedFileName,
                    OwnerUserId = user.Id,
                    OwnerEmail = user.Email ?? "",
                    OwnerUserName = user.UserName ?? "",
                    OwnerFullName = user.FullName ?? "",
                    FileType = extension.Replace(".", ""),
                    FileSizeBytes = uploadFile.Length,
                    UploadedAt = DateTime.UtcNow
                };

                _context.AppFiles.Add(appFile);
                uploadedCount++;
            }

            await _context.SaveChangesAsync();

            if (uploadedCount > 0)
                TempData["Success"] = $"تم رفع {uploadedCount} ملف بنجاح.";

            if (errors.Any())
                TempData["Error"] = string.Join(" | ", errors);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmptyExcel(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "NewFile.xlsx";

            if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                fileName += ".xlsx";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var storedFileName = $"{Guid.NewGuid()}.xlsx";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = "العمود 1";
                sheet.Cell(1, 2).Value = "العمود 2";
                sheet.Cell(1, 3).Value = "العمود 3";
                sheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }

            var fileInfo = new FileInfo(filePath);

            var appFile = new AppFile
            {
                OriginalFileName = fileName,
                StoredFileName = storedFileName,
                OwnerUserId = user.Id,
                OwnerEmail = user.Email ?? "",
                OwnerUserName = user.UserName ?? "",
                OwnerFullName = user.FullName ?? "",
                FileType = "xlsx",
                FileSizeBytes = fileInfo.Length,
                UploadedAt = DateTime.UtcNow
            };

            _context.AppFiles.Add(appFile);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إنشاء ملف Excel جديد.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await GetAllowedFile(id);
            if (file == null)
                return NotFound();

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.StoredFileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("الملف غير موجود.");

            if (file.FileType.ToLower() == "csv")
                return PhysicalFile(filePath, "text/csv", file.OriginalFileName);

            var model = ReadExcelFile(file, filePath);
            var bytes = BuildCleanExcelBytes(model);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                EnsureXlsxName(file.OriginalFileName));
        }

        public async Task<IActionResult> EditFile(int id)
        {
            var file = await GetAllowedFile(id);
            if (file == null)
                return NotFound();

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.StoredFileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("الملف غير موجود.");

            ExcelEditorViewModel model = file.FileType.ToLower() == "csv"
                ? ReadCsvFile(file, filePath)
                : ReadExcelFile(file, filePath);

            model.IsAdminView = User.IsInRole("Admin");

            return View("EditFile", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEditedFile([FromBody] ExcelSaveRequest request)
        {
            if (request == null)
                return Json(new { success = false, message = "البيانات المرسلة غير صحيحة." });

            var file = await GetAllowedFile(request.FileId);
            if (file == null)
                return Json(new { success = false, message = "الملف غير موجود أو ليس لديك صلاحية." });

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.StoredFileName);
            if (!System.IO.File.Exists(filePath))
                return Json(new { success = false, message = "الملف غير موجود على السيرفر." });

            try
            {
                await CreateVersionBackupAsync(file);

                if (file.FileType.ToLower() == "csv")
                    SaveCsvFile(filePath, request);
                else
                    SaveExcelFile(filePath, request);

                var fileInfo = new FileInfo(filePath);
                file.FileSizeBytes = fileInfo.Length;
                file.LastModifiedAt = DateTime.UtcNow;

                _context.AppFiles.Update(file);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم حفظ التعديلات بنجاح." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطأ أثناء الحفظ: {ex.Message}" });
            }
        }

        public async Task<IActionResult> ExportPdf(int id)
        {
            var file = await GetAllowedFile(id);
            if (file == null)
                return NotFound();

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.StoredFileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("الملف غير موجود.");

            var data = file.FileType.ToLower() == "csv"
                ? ReadCsvFile(file, filePath)
                : ReadExcelFile(file, filePath);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A3.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignRight().Text($"تصدير الملف: {file.OriginalFileName}").SemiBold().FontSize(18);
                        col.Item().AlignRight().Text($"النوع: {file.FileType.ToUpper()}");
                        col.Item().AlignRight().Text($"المالك: {file.OwnerFullName} ({file.OwnerUserName})");
                        col.Item().AlignRight().Text($"تاريخ الرفع: {file.UploadedAt:yyyy-MM-dd HH:mm}");
                        if (file.LastModifiedAt.HasValue)
                            col.Item().AlignRight().Text($"آخر تعديل: {file.LastModifiedAt.Value:yyyy-MM-dd HH:mm}");
                    });

                    page.Content().PaddingTop(15).Table(table =>
                    {
                        int colCount = Math.Max(1, data.Headers.Count);

                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < colCount; i++)
                                columns.RelativeColumn();
                        });

                        foreach (var header in data.Headers)
                        {
                            table.Cell()
                                .Background("#E2E8F0")
                                .Border(1)
                                .BorderColor("#CBD5E1")
                                .PaddingVertical(6)
                                .PaddingHorizontal(4)
                                .AlignCenter()
                                .Text(header ?? "")
                                .SemiBold();
                        }

                        foreach (var row in data.Rows)
                        {
                            for (int i = 0; i < colCount; i++)
                            {
                                var cellValue = i < row.Count ? row[i] : "";
                                table.Cell()
                                    .Border(1)
                                    .BorderColor("#E2E8F0")
                                    .PaddingVertical(5)
                                    .PaddingHorizontal(4)
                                    .AlignCenter()
                                    .Text(cellValue ?? "");
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text($"تم الإنشاء بتاريخ {DateTime.Now:yyyy-MM-dd HH:mm}");
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", Path.GetFileNameWithoutExtension(file.OriginalFileName) + ".pdf");
        }

        public async Task<IActionResult> Versions(int id)
        {
            var file = await GetAllowedFile(id);
            if (file == null)
                return NotFound();

            var versions = await _context.FileVersions
                .Where(v => v.AppFileId == id)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            ViewBag.File = file;
            return View("Versions", versions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreVersion(int versionId)
        {
            var version = await _context.FileVersions.FirstOrDefaultAsync(v => v.Id == versionId);
            if (version == null)
                return Json(new { success = false, message = "النسخة غير موجودة." });

            var file = await GetAllowedFile(version.AppFileId);
            if (file == null)
                return Json(new { success = false, message = "الملف غير موجود أو ليس لديك صلاحية." });

            var currentPath = Path.Combine(_environment.WebRootPath, "uploads", file.StoredFileName);
            var versionPath = Path.Combine(_environment.WebRootPath, "versions", version.StoredFileName);

            if (!System.IO.File.Exists(versionPath))
                return Json(new { success = false, message = "ملف النسخة غير موجود على السيرفر." });

            await CreateVersionBackupAsync(file);

            System.IO.File.Copy(versionPath, currentPath, true);

            var fileInfo = new FileInfo(currentPath);
            file.FileSizeBytes = fileInfo.Length;
            file.LastModifiedAt = DateTime.UtcNow;

            _context.AppFiles.Update(file);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم استرجاع النسخة بنجاح." });
        }

        [HttpGet]
        public IActionResult DeleteMyAccount()
        {
            return View("DeleteMyAccount", new DeleteAccountViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMyAccount(DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid)
                return View("DeleteMyAccount", model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction(nameof(Index));

            var passwordOk = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordOk)
            {
                ModelState.AddModelError(string.Empty, "كلمة المرور غير صحيحة.");
                return View("DeleteMyAccount", model);
            }

            var userFiles = await _context.AppFiles
                .Where(f => f.OwnerUserId == user.Id)
                .ToListAsync();

            var userFileIds = userFiles.Select(f => f.Id).ToList();

            var userVersions = await _context.FileVersions
                .Where(v => userFileIds.Contains(v.AppFileId))
                .ToListAsync();

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            var versionsFolder = Path.Combine(_environment.WebRootPath, "versions");

            foreach (var file in userFiles)
            {
                var filePath = Path.Combine(uploadsFolder, file.StoredFileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            foreach (var version in userVersions)
            {
                var versionPath = Path.Combine(versionsFolder, version.StoredFileName);
                if (System.IO.File.Exists(versionPath))
                    System.IO.File.Delete(versionPath);
            }

            _context.FileVersions.RemoveRange(userVersions);
            _context.AppFiles.RemoveRange(userFiles);
            await _context.SaveChangesAsync();

            await _signInManager.SignOutAsync();

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                foreach (var error in deleteResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View("DeleteMyAccount", model);
            }

            TempData["Success"] = "تم حذف الحساب وجميع ملفاتك بنجاح.";
            return Redirect("/Identity/Account/Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await GetAllowedFile(id);
            if (file == null)
                return Json(new { success = false, message = "الملف غير موجود أو لا تملك صلاحية." });

            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.StoredFileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                var versions = await _context.FileVersions.Where(v => v.AppFileId == file.Id).ToListAsync();
                foreach (var version in versions)
                {
                    var versionPath = Path.Combine(_environment.WebRootPath, "versions", version.StoredFileName);
                    if (System.IO.File.Exists(versionPath))
                        System.IO.File.Delete(versionPath);
                }

                _context.FileVersions.RemoveRange(versions);
                _context.AppFiles.Remove(file);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم حذف الملف بنجاح." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"فشل حذف الملف: {ex.Message}" });
            }
        }

        public IActionResult Privacy() => View();

        public IActionResult Error() => View();

        private async Task<AppFile?> GetAllowedFile(int id)
        {
            var file = await _context.AppFiles.FirstOrDefaultAsync(f => f.Id == id);
            if (file == null)
                return null;

            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && file.OwnerUserId != userId)
                return null;

            return file;
        }

        private async Task CreateVersionBackupAsync(AppFile file)
        {
            var currentPath = Path.Combine(_environment.WebRootPath, "uploads", file.StoredFileName);
            if (!System.IO.File.Exists(currentPath))
                return;

            var versionsFolder = Path.Combine(_environment.WebRootPath, "versions");
            if (!Directory.Exists(versionsFolder))
                Directory.CreateDirectory(versionsFolder);

            var ext = Path.GetExtension(file.StoredFileName);
            var versionStoredFileName = $"{Guid.NewGuid()}{ext}";
            var versionPath = Path.Combine(versionsFolder, versionStoredFileName);

            System.IO.File.Copy(currentPath, versionPath, true);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return;

            var versionCount = await _context.FileVersions.CountAsync(v => v.AppFileId == file.Id);

            var version = new FileVersion
            {
                AppFileId = file.Id,
                StoredFileName = versionStoredFileName,
                VersionLabel = $"نسخة {versionCount + 1}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user.Id,
                CreatedByUserName = user.UserName ?? "",
                CreatedByFullName = user.FullName ?? "",
                FileSizeBytes = new FileInfo(versionPath).Length
            };

            _context.FileVersions.Add(version);
            await _context.SaveChangesAsync();
        }

        private bool ExcelContainsText(string filePath, string search)
        {
            using var workbook = new XLWorkbook(filePath);
            var sheet = workbook.Worksheet(1);
            var range = sheet.RangeUsed();
            if (range == null)
                return false;

            foreach (var cell in range.Cells())
            {
                var text = cell.GetValue<string>() ?? "";
                if (text.ToLower().Contains(search))
                    return true;
            }

            return false;
        }

        private bool CsvContainsText(string filePath, string search)
        {
            foreach (var line in System.IO.File.ReadLines(filePath, Encoding.UTF8))
            {
                if ((line ?? "").ToLower().Contains(search))
                    return true;
            }

            return false;
        }

        private ExcelEditorViewModel ReadExcelFile(AppFile file, string filePath)
        {
            using var workbook = new XLWorkbook(filePath);
            var sheet = workbook.Worksheet(1);
            var range = sheet.RangeUsed();

            var headers = new List<string>();
            var rows = new List<List<string>>();

            if (range != null)
            {
                int firstRow = range.FirstRow().RowNumber();
                int lastRow = range.LastRow().RowNumber();
                int firstCol = range.FirstColumn().ColumnNumber();
                int lastCol = range.LastColumn().ColumnNumber();

                for (int col = firstCol; col <= lastCol; col++)
                    headers.Add(sheet.Cell(firstRow, col).GetValue<string>() ?? "");

                for (int row = firstRow + 1; row <= lastRow; row++)
                {
                    var rowData = new List<string>();
                    bool hasAnyValue = false;

                    for (int col = firstCol; col <= lastCol; col++)
                    {
                        var value = sheet.Cell(row, col).GetValue<string>() ?? "";
                        if (!string.IsNullOrWhiteSpace(value))
                            hasAnyValue = true;

                        rowData.Add(value);
                    }

                    if (hasAnyValue)
                        rows.Add(rowData);
                }
            }

            return new ExcelEditorViewModel
            {
                FileId = file.Id,
                FileName = file.OriginalFileName,
                OriginalFileName = file.OriginalFileName,
                SheetName = sheet.Name,
                FileType = file.FileType,
                IsCsv = false,
                Headers = headers,
                Rows = rows
            };
        }

        private ExcelEditorViewModel ReadCsvFile(AppFile file, string filePath)
        {
            var lines = System.IO.File.ReadAllLines(filePath, Encoding.UTF8).ToList();

            var headers = new List<string>();
            var rows = new List<List<string>>();

            if (lines.Count > 0)
            {
                headers = SplitCsvLine(lines[0]);
                for (int i = 1; i < lines.Count; i++)
                {
                    var row = SplitCsvLine(lines[i]);
                    if (row.All(string.IsNullOrWhiteSpace))
                        continue;

                    rows.Add(row);
                }
            }

            return new ExcelEditorViewModel
            {
                FileId = file.Id,
                FileName = file.OriginalFileName,
                OriginalFileName = file.OriginalFileName,
                SheetName = "CSV",
                FileType = file.FileType,
                IsCsv = true,
                Headers = headers,
                Rows = rows
            };
        }

        private void SaveExcelFile(string filePath, ExcelSaveRequest request)
        {
            using var workbook = new XLWorkbook();
            var sheetName = string.IsNullOrWhiteSpace(request.SheetName) ? "Sheet1" : request.SheetName;
            var sheet = workbook.Worksheets.Add(sheetName);

            for (int c = 0; c < request.Headers.Count; c++)
                sheet.Cell(1, c + 1).Value = request.Headers[c] ?? "";

            for (int r = 0; r < request.Rows.Count; r++)
            {
                for (int c = 0; c < request.Rows[r].Count; c++)
                    sheet.Cell(r + 2, c + 1).Value = request.Rows[r][c] ?? "";
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private void SaveCsvFile(string filePath, ExcelSaveRequest request)
        {
            var lines = new List<string>
            {
                string.Join(",", request.Headers.Select(EscapeCsv))
            };

            foreach (var row in request.Rows)
                lines.Add(string.Join(",", row.Select(EscapeCsv)));

            System.IO.File.WriteAllLines(filePath, lines, Encoding.UTF8);
        }

        private byte[] BuildCleanExcelBytes(ExcelEditorViewModel model)
        {
            using var workbook = new XLWorkbook();
            var sheetName = string.IsNullOrWhiteSpace(model.SheetName) ? "Sheet1" : model.SheetName;
            var sheet = workbook.Worksheets.Add(sheetName);

            for (int c = 0; c < model.Headers.Count; c++)
                sheet.Cell(1, c + 1).Value = model.Headers[c] ?? "";

            for (int r = 0; r < model.Rows.Count; r++)
            {
                for (int c = 0; c < model.Rows[r].Count; c++)
                    sheet.Cell(r + 2, c + 1).Value = model.Rows[r][c] ?? "";
            }

            sheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private List<string> SplitCsvLine(string line)
        {
            return line.Split(',').Select(x => x.Trim()).ToList();
        }

        private string EscapeCsv(string value)
        {
            value ??= string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }

        private string EnsureXlsxName(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            if (string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
                return fileName;

            return Path.GetFileNameWithoutExtension(fileName) + ".xlsx";
        }
    }
}