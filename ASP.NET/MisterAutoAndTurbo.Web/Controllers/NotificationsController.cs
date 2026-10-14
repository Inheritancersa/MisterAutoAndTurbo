using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    public class NotificationsController : Controller
    {
        [HttpGet]
        public IActionResult Index(string? customerId = null)
        {
            // Staff see all; optional filter by customer (mobile would pass their id)
            var list = AppDataStore.Notifications.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(customerId))
                list = list.Where(n => n.UserAccountId == customerId);

            return View(list.OrderByDescending(n => n.CreatedAt).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string message, string type, string? targetUserAccountId = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Notification message is required.";
                return RedirectToAction(nameof(Index));
            }

            AppDataStore.Notifications.Add(new Notification
            {
                Id = AppDataStore.NewId("ntf"),
                UserAccountId = targetUserAccountId ?? "staff",
                Message = message.Trim(),
                Type = string.IsNullOrWhiteSpace(type) ? "JobUpdate" : type,
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            TempData["Success"] = "Notification sent.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkRead(string id)
        {
            var n = AppDataStore.Notifications.FirstOrDefault(x => x.Id == id);
            if (n != null) n.IsRead = true;
            return RedirectToAction(nameof(Index));
        }
    }
}
