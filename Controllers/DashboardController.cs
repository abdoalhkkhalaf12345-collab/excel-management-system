using Abdullhak_Khalaf.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Abdullhak_Khalaf.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.AppFiles.AsQueryable();

            if (!isAdmin && user != null)
            {
                query = query.Where(x => x.OwnerUserId == user.Id);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.OriginalFileName.Contains(search) ||
                    x.FileType.Contains(search) ||
                    x.OwnerEmail.Contains(search));
            }

            var files = await query
                .OrderByDescending(x => x.UploadedAt)
                .ToListAsync();

            ViewBag.IsAdmin = isAdmin;
            ViewBag.Search = search;

            return View(files);
        }
    }
}