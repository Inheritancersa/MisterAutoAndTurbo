using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;
using MisterAutoAndTurbo.Web.Services;

namespace MisterAutoAndTurbo.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>Main dashboard — live counts from AppDataStore.</summary>
        public IActionResult Index()
        {
            // Mechanics have their own dashboard (assigned jobs, parts) - no payments/revenue.
            if (User.IsInRole(UserRoles.Mechanic))
                return RedirectToAction("Mechanic", "Dashboard");

            var today = SaTime.Now.Date;
            var jobs = AppDataStore.JobCards;
            var payments = AppDataStore.Payments.Where(p => p.Status != "Refunded").ToList();
            var quotes = AppDataStore.Quotations;

            // Outstanding balance across open quotes
            decimal outstanding = 0;
            foreach (var q in quotes.Where(q =>
                q.Status == "Approved" || q.Status == "Sent" || q.Status == "Pending"))
            {
                var paid = payments.Where(p => p.QuotationId == q.Id).Sum(p => p.Amount);
                var bal = q.TotalCost - paid;
                if (bal > 0) outstanding += bal;
            }

            var vehicleJobStatus = new Dictionary<string, string>();
            foreach (var j in jobs.OrderByDescending(x => x.CreatedAt))
            {
                if (!vehicleJobStatus.ContainsKey(j.VehichleId))
                    vehicleJobStatus[j.VehichleId] = j.Status;
            }

            var recentVehicles = AppDataStore.Vehicles
                .OrderByDescending(v => v.Id)
                .Take(6)
                .Select(v =>
                {
                    var owner = AppDataStore.Customers.FirstOrDefault(c => c.Id == v.CustomerId);
                    vehicleJobStatus.TryGetValue(v.Id, out var status);
                    return new DashboardVehicleRow
                    {
                        Id = v.Id,
                        Registration = v.RegistrationNumber,
                        MakeModel = $"{v.Make} {v.Model}".Trim(),
                        Owner = owner != null ? $"{owner.Name} {owner.Surname}".Trim() : "—",
                        JobStatus = status ?? "No active job"
                    };
                })
                .ToList();

            var recentJobs = jobs
                .OrderByDescending(j => j.CreatedAt)
                .Take(6)
                .Select(j =>
                {
                    var v = AppDataStore.Vehicles.FirstOrDefault(x => x.Id == j.VehichleId);
                    var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == j.CustomerId);
                    return new DashboardJobRow
                    {
                        Id = j.Id,
                        Status = j.Status,
                        Vehicle = v != null ? v.RegistrationNumber : j.VehichleId,
                        Customer = c != null ? $"{c.Name} {c.Surname}".Trim() : j.CustomerId,
                        CreatedAt = j.CreatedAt,
                        Mechanic = j.MechanicId
                    };
                })
                .ToList();

            var todayAppts = AppDataStore.Appointments
                .Where(a => a.ScheduledAt.Date == today)
                .OrderBy(a => a.ScheduledAt)
                .Take(8)
                .Select(a =>
                {
                    var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == a.CustomerId);
                    return new DashboardApptRow
                    {
                        Id = a.Id,
                        Time = a.ScheduledAt,
                        Customer = c != null ? $"{c.Name} {c.Surname}".Trim() : a.CustomerId,
                        Status = a.Status,
                        JobId = a.JobCardId
                    };
                })
                .ToList();

            var vm = new DashboardVm
            {
                TotalVehicles = AppDataStore.Vehicles.Count,
                TotalCustomers = AppDataStore.Customers.Count,
                ActiveJobs = jobs.Count(j => j.Status != JobStatus.Collected),
                ReadyForCollection = jobs.Count(j => j.Status == JobStatus.ReadyForCollection),
                TodaysAppointments = AppDataStore.Appointments.Count(a => a.ScheduledAt.Date == today),
                OutstandingPayments = outstanding,
                PaymentsToday = payments.Where(p => p.PaidAt.Date == today).Sum(p => p.Amount),
                OpenComplaints = AppDataStore.Complaints.Count(c => c.Status == "Open" || c.Status == "In Progress"),
                LowStockParts = AppDataStore.Parts.Count(p => p.QuantityInStock <= p.ReorderLevel),
                UnreadNotifications = AppDataStore.Notifications.Count(n =>
                    n.UserAccountId != NotificationsController.DismissedUserId
                    && !n.IsRead && (
                        n.UserAccountId == "staff"
                        || NotificationsController.CustomerAppTypes.Contains(n.Type, StringComparer.OrdinalIgnoreCase))),
                JobsByStatus = JobStatus.All.ToDictionary(s => s, s => jobs.Count(j => j.Status == s)),
                RecentVehicles = recentVehicles,
                RecentJobs = recentJobs,
                TodaysAppointmentList = todayAppts
            };

            return View(vm);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class DashboardVm
    {
        public int TotalVehicles { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveJobs { get; set; }
        public int ReadyForCollection { get; set; }
        public int TodaysAppointments { get; set; }
        public decimal OutstandingPayments { get; set; }
        public decimal PaymentsToday { get; set; }
        public int OpenComplaints { get; set; }
        public int LowStockParts { get; set; }
        public int UnreadNotifications { get; set; }
        public Dictionary<string, int> JobsByStatus { get; set; } = new();
        public List<DashboardVehicleRow> RecentVehicles { get; set; } = new();
        public List<DashboardJobRow> RecentJobs { get; set; } = new();
        public List<DashboardApptRow> TodaysAppointmentList { get; set; } = new();
    }

    public class DashboardVehicleRow
    {
        public string Id { get; set; } = string.Empty;
        public string Registration { get; set; } = string.Empty;
        public string MakeModel { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string JobStatus { get; set; } = string.Empty;
    }

    public class DashboardJobRow
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Vehicle { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Mechanic { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class DashboardApptRow
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Customer { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
    }
}
