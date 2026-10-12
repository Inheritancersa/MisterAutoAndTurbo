using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Mechanic: Notify Admin/Owner of job update.
    /// Staff can view notifications linked to UserAccount.
    /// </summary>
    public class NotificationsController : Controller
    {
        // GET: /Notifications
        [HttpGet]
        public IActionResult Index()
        {
            // TODO: Retrieve notifications for current user
            var list = new List<Notification>();
            return View(list);
        }

        // POST: /Notifications/Create  (Notify Admin/Owner)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string message, string type, string? targetUserAccountId = null)
        {
            // alt [job update entered] -> Save notification + Confirmation
            // else -> Display validation error
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Notification message is required.";
                return RedirectToAction(nameof(Index));
            }

            var model = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                UserAccountId = targetUserAccountId ?? string.Empty,
                Message = message,
                Type = string.IsNullOrWhiteSpace(type) ? "JobUpdate" : type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            // TODO: Save notification to Supabase (real-time for Flutter/staff)
            TempData["Success"] = "Notification sent.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Notifications/MarkRead/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkRead(string id)
        {
            // TODO: Mark as read
            return RedirectToAction(nameof(Index));
        }
    }
}
