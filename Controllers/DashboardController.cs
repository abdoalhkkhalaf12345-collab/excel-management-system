using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyFirstWebsite.Controllers
{
    [Authorize] // حماية الصفحة
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
