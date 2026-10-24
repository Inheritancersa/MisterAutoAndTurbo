using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;
using MisterAutoAndTurbo.Web.Services;
using MisterAutoAndTurbo.Web.ViewModels;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Manager only: add staff accounts, change roles, reset passwords, remove accounts.
    /// Accounts are stored in Supabase (<c>UserAccounts</c>) and survive app restarts.
    /// </summary>
    [Authorize(Roles = "Manager")]
    public class UsersController : Controller
    {
        // Database context used to save staff accounts (PersistUsers) and to force a fresh reload on the list page.
        private readonly ApplicationDbContext _db;

        public UsersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Lists staff accounts. Forces a refresh from the database, then filters by search text (username, e-mail or phone) and role, sorts by username and marks the signed-in user's row.
        // GET: /Users?search=..&role=..
        [HttpGet]
        public IActionResult Index(string? search = null, string? role = null)
        {
            // Always show the latest accounts from the database.
            AppDataStore.RefreshFromDatabase(_db, force: true);
            IEnumerable<UserAccount> query = AppDataStore.Users;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(u =>
                    (u.Username ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (u.Phone ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase));

            var currentId = CurrentUserId();

            var model = new UsersIndexViewModel
            {
                Search = search,
                Role = role,
                TotalUsers = AppDataStore.Users.Count,
                Users = query.OrderBy(u => u.Username).Select(u => new UserListItemViewModel
                {
                    Id = u.Id,
                    Username = u.Username ?? "",
                    Email = u.Email ?? "",
                    Phone = u.Phone ?? "",
                    Role = u.Role ?? "",
                    CreatedAt = u.CreatedAt,
                    IsCurrentUser = u.Id == currentId
                }).ToList()
            };
            return View(model);
        }

        // GET: shows the add-user form with Receptionist as the default role.
        // GET: /Users/Create
        [HttpGet]
        public IActionResult Create() =>
            FormView(new UserFormViewModel { Role = UserRoles.Receptionist }, "Add user", "Create a staff account and choose what they can access.");

        // POST: validates the form (role, unique e-mail and username, strong matching password), creates the account with a hashed password and saves it.
        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(UserFormViewModel form)
        {
            ValidateUser(form, isNew: true);
            if (!ModelState.IsValid)
                return FormView(form, "Add user", "Create a staff account and choose what they can access.");

            var user = new UserAccount
            {
                Id = AppDataStore.NewId("user"),
                Username = form.Username.Trim(),
                Email = form.Email.Trim(),
                Phone = form.Phone?.Trim() ?? string.Empty,
                Role = form.Role,
                PasswordHash = PasswordService.Hash(form.Password!),
                CreatedAt = DateTime.UtcNow
            };
            AppDataStore.Users.Add(user);
            AppDataStore.PersistUsers(_db);

            TempData["Success"] = $"Account created for {user.Username} ({user.Role}).";
            return RedirectToAction(nameof(Index));
        }

        // GET: shows the edit form filled with the user's current details.
        // GET: /Users/Edit/{id}
        [HttpGet]
        public IActionResult Edit(string id)
        {
            var user = Find(id);
            if (user == null) return NotFound();

            return FormView(new UserFormViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role
            }, "Edit user", "Update this person's details or role.");
        }

        // POST: validates the changes. A manager cannot change their own role, and the last Manager cannot be changed to another role. Valid changes are copied onto the account and saved.
        // POST: /Users/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, UserFormViewModel form)
        {
            if (id != form.Id) return NotFound();

            var user = Find(id);
            if (user == null) return NotFound();

            ValidateUser(form, isNew: false);

            // Keep the workshop from locking itself out.
            if (user.Role != form.Role)
            {
                if (user.Id == CurrentUserId())
                    ModelState.AddModelError(nameof(form.Role), "You can't change your own role.");
                else if (user.Role == UserRoles.Manager && ManagerCount() <= 1)
                    ModelState.AddModelError(nameof(form.Role), "There must be at least one Manager.");
            }

            if (!ModelState.IsValid)
                return FormView(form, "Edit user", "Update this person's details or role.");

            user.Username = form.Username.Trim();
            user.Email = form.Email.Trim();
            user.Phone = form.Phone?.Trim() ?? string.Empty;
            user.Role = form.Role;
            AppDataStore.PersistUsers(_db);

            TempData["Success"] = user.Id == CurrentUserId()
                ? "Your details were updated. A new name shows in the top bar after you sign in again."
                : $"{user.Username} was updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: shows the reset-password form for a user.
        // GET: /Users/ResetPassword/{id}
        [HttpGet]
        public IActionResult ResetPassword(string id)
        {
            var user = Find(id);
            if (user == null) return NotFound();

            return View(new ResetPasswordViewModel { Id = user.Id, Username = user.Username });
        }

        // POST: validates the new password (strength and confirmation), hashes it, and saves it so the manager can hand it to the user.
        // POST: /Users/ResetPassword/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string id, ResetPasswordViewModel form)
        {
            if (id != form.Id) return NotFound();

            var user = Find(id);
            if (user == null) return NotFound();

            form.Username = user.Username;

            if (ModelState.IsValid)
            {
                var weak = PasswordService.Validate(form.NewPassword);
                if (weak != null)
                    ModelState.AddModelError(nameof(form.NewPassword), weak);
                else if (form.NewPassword != form.ConfirmPassword)
                    ModelState.AddModelError(nameof(form.ConfirmPassword), "Passwords do not match.");
            }

            if (!ModelState.IsValid)
                return View(form);

            user.PasswordHash = PasswordService.Hash(form.NewPassword);

            AppDataStore.PersistUsers(_db);

            TempData["Success"] = $"Password reset for {user.Username}. Give them the new password and ask them to keep it private.";
            return RedirectToAction(nameof(Index));
        }

        // GET: confirmation page before deleting an account.
        // GET: /Users/Delete/{id}  (confirmation page)
        [HttpGet]
        public IActionResult Delete(string id)
        {
            var user = Find(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: deletes the account. Refused when it is your own account or the last Manager, so the workshop is never locked out.
        // POST: /Users/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            var user = Find(id);
            if (user == null) return NotFound();

            if (user.Id == CurrentUserId())
            {
                TempData["Error"] = "You can't delete your own account while signed in.";
                return RedirectToAction(nameof(Index));
            }

            if (user.Role == UserRoles.Manager && ManagerCount() <= 1)
            {
                TempData["Error"] = "There must be at least one Manager, so this account can't be deleted.";
                return RedirectToAction(nameof(Index));
            }

            AppDataStore.Users.Remove(user);
            AppDataStore.PersistUsers(_db);
            TempData["Success"] = $"{user.Username} was deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- helpers ----------

        // Finds a user by id (null when the id is empty or unknown).
        private static UserAccount? Find(string? id) =>
            string.IsNullOrWhiteSpace(id) ? null : AppDataStore.Users.FirstOrDefault(u => u.Id == id);

        // Id of the signed-in user taken from the login cookie claims.
        private string? CurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Counts accounts that have the Manager role.
        private static int ManagerCount() => AppDataStore.Users.Count(u => u.Role == UserRoles.Manager);

        // Shows the shared user form view with a title and subtitle.
        private IActionResult FormView(UserFormViewModel form, string title, string subtitle)
        {
            ViewData["Title"] = title;
            ViewData["Subtitle"] = subtitle;
            return View("Form", form);
        }

        // Shared validation for create and edit: role must be valid, e-mail and username must be unique (ignoring the account being edited), and when creating a user the password must be strong and match its confirmation.
        private void ValidateUser(UserFormViewModel form, bool isNew)
        {
            if (!string.IsNullOrWhiteSpace(form.Role) && !UserRoles.All.Contains(form.Role))
                ModelState.AddModelError(nameof(form.Role), "Select a valid role.");

            if (!string.IsNullOrWhiteSpace(form.Email) &&
                AppDataStore.Users.Any(u => u.Id != form.Id &&
                    string.Equals(u.Email, form.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(form.Email), "An account with this email already exists.");
            }

            if (!string.IsNullOrWhiteSpace(form.Username) &&
                AppDataStore.Users.Any(u => u.Id != form.Id &&
                    string.Equals(u.Username, form.Username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(form.Username), "That username is already taken.");
            }

            if (isNew)
            {
                var weak = PasswordService.Validate(form.Password);
                if (weak != null)
                    ModelState.AddModelError(nameof(form.Password), weak);
                else if (form.Password != form.ConfirmPassword)
                    ModelState.AddModelError(nameof(form.ConfirmPassword), "Passwords do not match.");
            }
        }
    }
}
