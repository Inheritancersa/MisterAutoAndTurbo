using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;
using MisterAutoAndTurbo.Web.Services;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Login / Register / Logout using cookie authentication
    /// (Microsoft.AspNetCore.Authentication.Core + Cookies).
    /// Roles: Manager (full), Receptionist, Mechanic (limited per use-case diagrams).
    /// </summary>
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private static readonly string[] AllowedRoles = { "Manager", "Receptionist", "Mechanic" };

        private readonly IEmailService _email;
        private readonly PasswordResetService _resets;
        private readonly ILogger<AccountController> _log;
        private readonly ApplicationDbContext _db;

        public AccountController(
            IEmailService email,
            PasswordResetService resets,
            ILogger<AccountController> log,
            ApplicationDbContext db)
        {
            _email = email;
            _resets = resets;
            _log = log;
            _db = db;
        }

        // ---------- Forgot password: email a verification code immediately ----------

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Enter your email address.");
                return View();
            }

            var term = email.Trim();
            var user = AppDataStore.Users.FirstOrDefault(u =>
                string.Equals(u.Email, term, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Username, term, StringComparison.OrdinalIgnoreCase));

            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var code = _resets.CreateCode(user.Id);
                if (code != null)
                {
                    try
                    {
                        await _email.SendAsync(user.Email, "Your Mister Auto & Turbo verification code",
                            $"<p>Hi {System.Net.WebUtility.HtmlEncode(user.Username)},</p>" +
                            $"<p>Your password reset verification code is:</p>" +
                            $"<p style='font-size:28px;font-weight:bold;letter-spacing:6px'>{code}</p>" +
                            "<p>It expires in 10 minutes. If you didn't request this, you can ignore this email.</p>");
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Could not send password reset email to {Email}", user.Email);
                        ModelState.AddModelError(string.Empty, "We couldn't send the email right now. Please try again in a moment.");
                        return View();
                    }
                }
            }

            // Same response whether or not the account exists, so emails can't be probed.
            TempData["Success"] = "If an account matches, a verification code has been emailed. Enter it below.";
            return RedirectToAction(nameof(ResetPassword), new { email = term });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? email = null)
        {
            ViewData["Email"] = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string email, string code, string newPassword, string confirmPassword)
        {
            ViewData["Email"] = email;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError(string.Empty, "Email and verification code are required.");
                return View();
            }
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View();
            }
            var weak = PasswordService.Validate(newPassword);
            if (weak != null)
            {
                ModelState.AddModelError(string.Empty, weak);
                return View();
            }

            var term = email.Trim();
            var user = AppDataStore.Users.FirstOrDefault(u =>
                string.Equals(u.Email, term, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Username, term, StringComparison.OrdinalIgnoreCase));

            if (user == null || !_resets.Verify(user.Id, code))
            {
                ModelState.AddModelError(string.Empty, "That code is invalid or has expired. Request a new one.");
                return View();
            }

            user.PasswordHash = PasswordService.Hash(newPassword);
            AppDataStore.PersistUsers(_db);
            TempData["Success"] = "Your password has been reset. You can sign in now.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToLocal(returnUrl);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View();
            }

            var user = AppDataStore.Users.FirstOrDefault(u =>
                string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Username, email.Trim(), StringComparison.OrdinalIgnoreCase));

            if (user == null || !PasswordService.Verify(user.PasswordHash, password))
            {
                ModelState.AddModelError(string.Empty, "Invalid email/username or password.");
                return View();
            }

            await SignInUserAsync(user);
            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new UserAccount());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserAccount model, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ModelState.AddModelError(string.Empty, "Username is required.");
                return View(model);
            }

            if (model.PasswordHash != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Role) || !AllowedRoles.Contains(model.Role))
            {
                ModelState.AddModelError(string.Empty, "Please select a valid role (Receptionist, Mechanic, or Manager).");
                return View(model);
            }

            var emailExists = AppDataStore.Users.Any(u =>
                string.Equals(u.Email, model.Email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (emailExists)
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return View(model);
            }

            var usernameExists = AppDataStore.Users.Any(u =>
                string.Equals(u.Username, model.Username.Trim(), StringComparison.OrdinalIgnoreCase));
            if (usernameExists)
            {
                ModelState.AddModelError(string.Empty, "Username is already taken.");
                return View(model);
            }

            model.Id = AppDataStore.NewId("user");
            model.Email = model.Email.Trim();
            model.Username = model.Username.Trim();
            model.CreatedAt = DateTime.UtcNow;
            model.PasswordHash = PasswordService.Hash(model.PasswordHash);
            AppDataStore.Users.Add(model);
            AppDataStore.PersistUsers(_db);

            TempData["Success"] = $"Account created for {model.Username} ({model.Role}). You can sign in now.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutPost()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // Layout posts to asp-action="Logout" — support POST to Logout as well
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Logout")]
        public async Task<IActionResult> LogoutFromForm()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUserAsync(UserAccount user)
        {
            var displayName = string.IsNullOrWhiteSpace(user.Username) ? user.Email : user.Username;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? "Mechanic"),
                new Claim("Phone", user.Phone ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}
