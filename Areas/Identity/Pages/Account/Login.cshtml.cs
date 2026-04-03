using System.ComponentModel.DataAnnotations;
using Abdullhak_Khalaf.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Abdullhak_Khalaf.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string Login { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; } = true;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            ApplicationUser? user = null;

            if (Input.Login.Contains("@"))
            {
                var normalizedEmail = _userManager.NormalizeEmail(Input.Login);

                var usersByEmail = await _userManager.Users
                    .Where(u => u.NormalizedEmail == normalizedEmail)
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                if (usersByEmail.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة.");
                    return Page();
                }

                if (usersByEmail.Count > 1)
                {
                    ModelState.AddModelError(string.Empty, "يوجد أكثر من حساب بنفس البريد الإلكتروني. استخدم اسم المستخدم أو اطلب من المدير حل المشكلة.");
                    return Page();
                }

                user = usersByEmail.First();
            }
            else
            {
                user = await _userManager.FindByNameAsync(Input.Login);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة.");
                    return Page();
                }
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة.");
            return Page();
        }
    }
}