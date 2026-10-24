using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;
using MisterAutoAndTurbo.Web.Services;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Lightweight JSON endpoints polled by the browser for live badge / dashboard updates.
    /// PersistChangesFilter still runs on each request so Supabase data is pulled first.
    /// </summary>
    [Authorize]
    public class LiveController : Controller
    {
        // GET /Live/Summary: returns JSON with live numbers (unread staff notifications, active jobs, ready for collection, today's appointments, vehicles, outstanding payments, open complaints, low stock parts and the 5 latest notifications). The browser polls it to update badges and dashboard figures without reloading the page.
        [HttpGet]
        public IActionResult Summary()
        {
            var staffTypes = NotificationsController.CustomerAppTypes;
            var unread = AppDataStore.Notifications.Count(n =>
                n.UserAccountId != NotificationsController.DismissedUserId
                && !n.IsRead
                && (
                    n.UserAccountId == "staff"
                    || staffTypes.Contains(n.Type, StringComparer.OrdinalIgnoreCase)));

            var today = SaTime.Now.Date;
            var jobs = AppDataStore.JobCards;
            var payments = AppDataStore.Payments.Where(p => p.Status != "Refunded").ToList();

            // Outstanding balance across open quotations (quotation total minus payments, positive balances only).
            decimal outstanding = 0;
            foreach (var q in AppDataStore.Quotations.Where(q =>
                         q.Status == "Approved" || q.Status == "Sent" || q.Status == "Pending"))
            {
                var paid = payments.Where(p => p.QuotationId == q.Id).Sum(p => p.Amount);
                var bal = q.TotalCost - paid;
                if (bal > 0) outstanding += bal;
            }

            // Five newest notifications meant for staff (dismissed ones are excluded); times are converted to South African time.
            var latest = AppDataStore.Notifications
                .Where(n =>
                    n.UserAccountId != NotificationsController.DismissedUserId
                    && (
                        n.UserAccountId == "staff"
                        || staffTypes.Contains(n.Type, StringComparer.OrdinalIgnoreCase)))
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    id = n.Id,
                    type = n.Type,
                    message = n.Message,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt.ToSa().ToString("g")
                })
                .ToList();

            return Json(new
            {
                serverTime = SaTime.Now.ToString("HH:mm:ss"),
                unreadNotifications = unread,
                activeJobs = jobs.Count(j => j.Status != JobStatus.Collected),
                readyForCollection = jobs.Count(j => j.Status == JobStatus.ReadyForCollection),
                todaysAppointments = AppDataStore.Appointments.Count(a => a.ScheduledAt.Date == today),
                totalVehicles = AppDataStore.Vehicles.Count,
                outstandingPayments = outstanding,
                openComplaints = AppDataStore.Complaints.Count(c =>
                    c.Status == "Open" || c.Status == "In Progress"),
                lowStockParts = AppDataStore.Parts.Count(p => p.QuantityInStock <= p.ReorderLevel),
                latest
            });
        }
    }
}
