using MisterAutoAndTurbo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    // In-app notifications for staff. Customer-app alerts are written by AppDataStore.ApplyCustomerAppUpdates.
    [Authorize(Roles = "Manager,Receptionist,Mechanic")]
    public class NotificationsController : Controller
    {
        /// <summary>Types written by ApplyCustomerAppUpdates when the Flutter customer app acts.</summary>
        public static readonly string[] CustomerAppTypes =
        {
            "ServiceRequest",
            "AppointmentBooking",
            "Complaint",
            "PaymentProof",
            "QuotationResponse"
        };

        /// <summary>Hidden rows left after delete so customer-app alerts are not recreated.</summary>
        public const string DismissedUserId = "__dismissed__";

        // GET /Notifications: lists notifications newest first. Filters: a specific customer, 'staff' (staff alerts and customer-app alerts) or 'unread'. Dismissal stubs are hidden. Also passes the unread staff count to the view.
        [HttpGet]
        public IActionResult Index(string? customerId = null, string? filter = null)
        {
            // Hide dismissal stubs from the UI
            var list = AppDataStore.Notifications
                .Where(n => n.UserAccountId != DismissedUserId)
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(customerId))
                list = list.Where(n => n.UserAccountId == customerId);

            if (string.Equals(filter, "staff", StringComparison.OrdinalIgnoreCase))
                list = list.Where(n =>
                    n.UserAccountId == "staff"
                    || CustomerAppTypes.Contains(n.Type, StringComparer.OrdinalIgnoreCase));

            if (string.Equals(filter, "unread", StringComparison.OrdinalIgnoreCase))
                list = list.Where(n => !n.IsRead);

            ViewBag.Filter = filter;
            ViewBag.UnreadStaffCount = AppDataStore.Notifications.Count(n =>
                n.UserAccountId != DismissedUserId
                && !n.IsRead
                && (
                    n.UserAccountId == "staff"
                    || CustomerAppTypes.Contains(n.Type, StringComparer.OrdinalIgnoreCase)));

            return View(list.OrderByDescending(n => n.CreatedAt).ToList());
        }

        // POST: creates a notification with a message and type. It is sent to the staff group unless a target user account id is given.
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
                CreatedAt = DateTime.UtcNow
            });

            TempData["Success"] = "Notification sent.";
            return RedirectToAction(nameof(Index));
        }

        // POST: marks one notification as read and returns to the list with the same filter.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkRead(string id)
        {
            var n = AppDataStore.Notifications.FirstOrDefault(x => x.Id == id);
            if (n != null) n.IsRead = true;
            return RedirectToAction(nameof(Index), new { filter = Request.Form["filter"].ToString() });
        }

        // POST: marks all unread staff and customer-app alerts as read.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllStaffRead()
        {
            foreach (var n in AppDataStore.Notifications.Where(n =>
                n.UserAccountId != DismissedUserId
                && !n.IsRead
                && (
                    n.UserAccountId == "staff"
                    || CustomerAppTypes.Contains(n.Type, StringComparer.OrdinalIgnoreCase))))
            {
                n.IsRead = true;
            }
            TempData["Success"] = "All staff alerts marked as read.";
            return RedirectToAction(nameof(Index), new { filter = "staff" });
        }

        /// <summary>
        /// Removes a notification from the list. For customer-app alerts (booking, complaint, etc.)
        /// a hidden dismissal stub is kept so ApplyCustomerAppUpdates does not recreate it.
        /// </summary>
        // POST: deletes one notification. For staff/customer-app alerts a hidden 'dismissed' stub with the same type and related id is added so the alert is not created again at the next refresh.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id, string? filter = null)
        {
            var n = AppDataStore.Notifications.FirstOrDefault(x => x.Id == id);
            if (n == null)
            {
                TempData["Error"] = "Notification not found.";
                return RedirectToAction(nameof(Index), new { filter });
            }

            var type = n.Type;
            var relatedId = n.RelatedId;
            var isCustomerAppAlert = CustomerAppTypes.Contains(type, StringComparer.OrdinalIgnoreCase)
                || n.UserAccountId == "staff";

            AppDataStore.Notifications.Remove(n);

            // Prevent recreation of the same staff alert on the next Supabase refresh
            if (isCustomerAppAlert
                && !string.IsNullOrWhiteSpace(type)
                && !string.IsNullOrWhiteSpace(relatedId)
                && !AppDataStore.Notifications.Any(x =>
                    x.Type == type && x.RelatedId == relatedId))
            {
                AppDataStore.Notifications.Add(new Notification
                {
                    Id = AppDataStore.NewId("ntf"),
                    UserAccountId = DismissedUserId,
                    Type = type,
                    RelatedId = relatedId,
                    Message = "",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            TempData["Success"] = "Notification deleted.";
            return RedirectToAction(nameof(Index), new { filter });
        }

        // POST: deletes all read notifications (limited to staff alerts when the staff filter is on). Each deleted alert leaves a dismissal stub in the same way as Delete.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAllRead(string? filter = null)
        {
            var toRemove = AppDataStore.Notifications
                .Where(n =>
                    n.UserAccountId != DismissedUserId
                    && n.IsRead
                    && (string.IsNullOrEmpty(filter)
                        || (filter == "staff" && (n.UserAccountId == "staff"
                            || CustomerAppTypes.Contains(n.Type, StringComparer.OrdinalIgnoreCase)))
                        || (filter == "unread" && false))) // no read items in unread filter
                .ToList();

            foreach (var n in toRemove)
            {
                var type = n.Type;
                var relatedId = n.RelatedId;
                var isCustomerAppAlert = CustomerAppTypes.Contains(type, StringComparer.OrdinalIgnoreCase)
                    || n.UserAccountId == "staff";

                AppDataStore.Notifications.Remove(n);

                if (isCustomerAppAlert
                    && !string.IsNullOrWhiteSpace(type)
                    && !string.IsNullOrWhiteSpace(relatedId)
                    && !AppDataStore.Notifications.Any(x =>
                        x.Type == type && x.RelatedId == relatedId))
                {
                    AppDataStore.Notifications.Add(new Notification
                    {
                        Id = AppDataStore.NewId("ntf"),
                        UserAccountId = DismissedUserId,
                        Type = type,
                        RelatedId = relatedId,
                        Message = "",
                        IsRead = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            TempData["Success"] = toRemove.Count == 0
                ? "No read notifications to delete."
                : $"Deleted {toRemove.Count} read notification(s).";
            return RedirectToAction(nameof(Index), new { filter });
        }
    }
}
