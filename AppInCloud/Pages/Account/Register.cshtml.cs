#nullable disable
using AppInCloud.Data;
using AppInCloud.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AppInCloud.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private static string REGISTRATION_ENABLED_CACHE_KEY = "registration_enabled";

        public RegisterModel(ILogger<LoginModel> logger, IMemoryCache cache, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
            _cache = cache;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password")]
            public string PasswordRepeat { get; set; }
        }

        private bool isRegistrationEnabled() {
            return _cache.TryGetValue(REGISTRATION_ENABLED_CACHE_KEY, out Boolean registrationEnabled) ? registrationEnabled : true;
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if(!isRegistrationEnabled()) return LocalRedirect("/");
            ReturnUrl = returnUrl;
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            // Clear the existing external cookie
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if(!isRegistrationEnabled()) return LocalRedirect("/");

            ReturnUrl = returnUrl;

            if (ModelState.IsValid)
            {
                // Use Input.Email and Input.Password to authenticate the user
                // with your custom authentication logic.
                //
                // For demonstration purposes, the sample validates the user
                // on the email address maria.rodriguez@contoso.com with 
                // any password that passes model validation.

                var user = new ApplicationUser {
                    Email = Input.Email,
                    UserName = Input.Email,
                    PasswordHash = ApplicationUser.HashPassword(Input.Password),
                    IsAdmin = false
                };

                if(_db.Users.Where(u => u.Email == Input.Email).Count() > 0){
                    ModelState.AddModelError(string.Empty, "Email has already been taken.");
                    return Page();
                }

                _db.Users.Add(user);
                _db.SaveChanges();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim("FullName", user.UserName),
                    new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "Member"),
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    //AllowRefresh = <bool>,
                    // Refreshing the authentication session should be allowed.

                    //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                    // The time at which the authentication ticket expires. A 
                    // value set here overrides the ExpireTimeSpan option of 
                    // CookieAuthenticationOptions set with AddCookie.

                    //IsPersistent = true,
                    // Whether the authentication session is persisted across 
                    // multiple requests. When used with cookies, controls
                    // whether the cookie's lifetime is absolute (matching the
                    // lifetime of the authentication ticket) or session-based.

                    //IssuedUtc = <DateTimeOffset>,
                    // The time at which the authentication ticket was issued.

                    //RedirectUri = <string>
                    // The full path or absolute URI to be used as an http 
                    // redirect response value.
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), 
                    authProperties);

                _logger.LogInformation("User {Email} registered in at {Time}.", 
                    user.Email, DateTime.UtcNow);

                return LocalRedirect("/");
            }

            // Something failed. Redisplay the form.
            return Page();
        }

    }
}
