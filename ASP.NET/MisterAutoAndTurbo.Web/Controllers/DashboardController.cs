using MisterAutoAndTurbo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Manager reports and performance snapshots from in-memory workshop data.
    /// </summary>
    // Dashboard pages per role plus the Manager reports and performance pages. Individual actions carry their own role rules.
    public class DashboardController : Controller
    {
        // GET: main dashboard; mechanics are redirected to their own dashboard page.
        [HttpGet]
        public IActionResult Index()
        {
            if (User.IsInRole("Mechanic"))
                return RedirectToAction(nameof(Mechanic));

            ViewData["Role"] = "Manager";
            return View();
        }

        // GET: manager dashboard page.
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public IActionResult Manager() => View();

        // GET: receptionist dashboard page.
        [HttpGet]
        [Authorize(Roles = "Manager,Receptionist")]
        public IActionResult Receptionist() => View();

        // GET: mechanic dashboard page.
        [HttpGet]
        [Authorize(Roles = "Manager,Mechanic")]
        public IActionResult Mechanic() => View();

        // Builds the workshop report model: jobs by status, payments by method and total, quotes by status and value, outstanding balance, appointments, collections, complaints, parts stock and value, and the 8 most recent jobs and payments. Customer names and vehicle labels are passed in ViewBag for the view.
        /// <summary>Manager: Generate Reports — live summary from AppDataStore.</summary>
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public IActionResult Reports()
        {
            var jobs = AppDataStore.JobCards;
            var payments = AppDataStore.Payments.Where(p => p.Status != "Refunded").ToList();
            var quotes = AppDataStore.Quotations;
            var appointments = AppDataStore.Appointments;
            var collections = AppDataStore.VehicleCollections;
            var complaints = AppDataStore.Complaints;
            var parts = AppDataStore.Parts;

            var jobsByStatus = JobStatus.All.ToDictionary(
                s => s,
                s => jobs.Count(j => j.Status == s));

            var paymentsByMethod = payments
                .GroupBy(p => string.IsNullOrEmpty(p.Method) ? "Other" : p.Method)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var quotesByStatus = new[] { "Pending", "Sent", "Approved", "Rejected" }
                .ToDictionary(s => s, s => quotes.Count(q => q.Status == s));

            // Outstanding: quote total - payments on that quote (positive balances only).
            // Outstanding: quote total - payments on that quote
            decimal outstanding = 0;
            foreach (var q in quotes.Where(q => q.Status == "Approved" || q.Status == "Sent" || q.Status == "Pending"))
            {
                var paid = payments.Where(p => p.QuotationId == q.Id).Sum(p => p.Amount);
                var bal = q.TotalCost - paid;
                if (bal > 0) outstanding += bal;
            }

            var today = SaTime.Now.Date;
            var report = new WorkshopReportVm
            {
                GeneratedAt = DateTime.UtcNow,

                TotalVehicles = AppDataStore.Vehicles.Count,
                TotalCustomers = AppDataStore.Customers.Count,

                TotalJobs = jobs.Count,
                ActiveJobs = jobs.Count(j => j.Status != JobStatus.Collected),
                JobsByStatus = jobsByStatus,
                JobsCreatedToday = jobs.Count(j => j.CreatedAt.Date == today),
                JobsCollected = jobs.Count(j => j.Status == JobStatus.Collected),
                JobsReadyForCollection = jobs.Count(j => j.Status == JobStatus.ReadyForCollection),

                TotalQuotations = quotes.Count,
                QuotesByStatus = quotesByStatus,
                QuotesApprovedValue = quotes.Where(q => q.Status == "Approved").Sum(q => q.TotalCost),
                QuotesPendingValue = quotes.Where(q => q.Status == "Pending" || q.Status == "Sent").Sum(q => q.TotalCost),

                PaymentsCount = payments.Count,
                PaymentsTotal = payments.Sum(p => p.Amount),
                PaymentsToday = payments.Where(p => p.PaidAt.Date == today).Sum(p => p.Amount),
                PaymentsByMethod = paymentsByMethod,
                OutstandingBalance = outstanding,

                AppointmentsTotal = appointments.Count,
                AppointmentsToday = appointments.Count(a => a.ScheduledAt.Date == today),
                AppointmentsUpcoming = appointments.Count(a => a.ScheduledAt >= SaTime.Now && a.Status == "Scheduled"),

                CollectionsReady = collections.Count(c => c.CollectionStatus == "Ready"),
                CollectionsCompleted = collections.Count(c => c.CollectionStatus == "Collected"),

                ComplaintsOpen = complaints.Count(c => c.Status == "Open" || c.Status == "In Progress"),
                ComplaintsResolved = complaints.Count(c => c.Status == "Resolved" || c.Status == "Closed"),
                ComplaintsFromMobile = complaints.Count(c => c.Source == "Mobile"),

                PartsInStock = parts.Sum(p => p.QuantityInStock),
                PartsLowStock = parts.Count(p => p.QuantityInStock <= p.ReorderLevel),
                PartsInventoryValue = parts.Sum(p => p.QuantityInStock * p.UnitPrice),

                RecentJobs = jobs.OrderByDescending(j => j.CreatedAt).Take(8).ToList(),
                RecentPayments = AppDataStore.Payments.OrderByDescending(p => p.PaidAt).Take(8).ToList()
            };

            ViewBag.CustomerNames = AppDataStore.Customers.ToDictionary(
                c => c.Id, c => $"{c.Name} {c.Surname}".Trim());
            ViewBag.VehicleLabels = AppDataStore.Vehicles.ToDictionary(
                v => v.Id, v => v.RegistrationNumber);

            return View(report);
        }

        // Builds the performance model: completion rate, job pipeline per status, mechanic workload, unassigned active jobs, quote approval rate, average turnaround in days (collected jobs), revenue today and this week (week starts on Sunday), jobs created per weekday and the bottleneck status (active status with the most jobs).
        /// <summary>Manager: Monitor workshop performance — throughput, completion, workload.</summary>
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public IActionResult Performance()
        {
            var jobs = AppDataStore.JobCards.ToList();
            var payments = AppDataStore.Payments.Where(p => p.Status != "Refunded").ToList();
            var quotes = AppDataStore.Quotations.ToList();
            var today = SaTime.Now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var total = jobs.Count;
            var completed = jobs.Count(j => j.Status == JobStatus.Collected);
            var active = jobs.Count(j => j.Status != JobStatus.Collected);
            var completionRate = total == 0 ? 0 : (decimal)completed / total * 100m;

            // Pipeline counts
            var pipeline = JobStatus.All.Select(s => new StatusCount
            {
                Status = s,
                Count = jobs.Count(j => j.Status == s)
            }).ToList();

            // Mechanic workload
            var byMechanic = jobs
                .Where(j => !string.IsNullOrWhiteSpace(j.MechanicId))
                .GroupBy(j => j.MechanicId)
                .Select(g => new MechanicWorkload
                {
                    MechanicId = g.Key!,
                    TotalJobs = g.Count(),
                    ActiveJobs = g.Count(j => j.Status != JobStatus.Collected),
                    CompletedJobs = g.Count(j => j.Status == JobStatus.Collected),
                    InProgress = g.Count(j => j.Status == JobStatus.InProgress)
                })
                .OrderByDescending(m => m.ActiveJobs)
                .ThenByDescending(m => m.TotalJobs)
                .ToList();

            // Unassigned active jobs
            var unassigned = jobs.Count(j =>
                j.Status != JobStatus.Collected && string.IsNullOrWhiteSpace(j.MechanicId));

            // Quote conversion
            var quotesApproved = quotes.Count(q => q.Status == "Approved");
            var quotesRejected = quotes.Count(q => q.Status == "Rejected");
            var quotesResponded = quotesApproved + quotesRejected;
            var approvalRate = quotesResponded == 0
                ? 0
                : (decimal)quotesApproved / quotesResponded * 100m;

            // Simple turnaround proxy: collected jobs with known CreatedAt → age in days
            var turnaroundDays = jobs
                .Where(j => j.Status == JobStatus.Collected && j.CreatedAt != default)
                .Select(j => (DateTime.UtcNow - j.CreatedAt).TotalDays)
                .ToList();
            var avgTurnaround = turnaroundDays.Count == 0 ? 0 : turnaroundDays.Average();

            var revenueThisWeek = payments
                .Where(p => p.PaidAt.Date >= weekStart)
                .Sum(p => p.Amount);
            var revenueToday = payments
                .Where(p => p.PaidAt.Date == today)
                .Sum(p => p.Amount);

            // Jobs created this week by day (Mon–Sun labels)
            var jobsThisWeek = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var day = weekStart.AddDays(i);
                    return new DayCount
                    {
                        Label = day.ToString("ddd"),
                        Date = day,
                        Count = jobs.Count(j => j.CreatedAt.Date == day)
                    };
                })
                .ToList();

            var vm = new WorkshopPerformanceVm
            {
                GeneratedAt = DateTime.UtcNow,
                TotalJobs = total,
                ActiveJobs = active,
                CompletedJobs = completed,
                CompletionRatePercent = Math.Round(completionRate, 1),
                JobsCreatedToday = jobs.Count(j => j.CreatedAt.Date == today),
                JobsCreatedThisWeek = jobs.Count(j => j.CreatedAt.Date >= weekStart),
                ReadyForCollection = jobs.Count(j => j.Status == JobStatus.ReadyForCollection),
                InProgress = jobs.Count(j => j.Status == JobStatus.InProgress),
                Pipeline = pipeline,
                MechanicWorkloads = byMechanic,
                UnassignedActiveJobs = unassigned,
                QuoteApprovalRatePercent = Math.Round(approvalRate, 1),
                QuotesApproved = quotesApproved,
                QuotesRejected = quotesRejected,
                QuotesPendingOrSent = quotes.Count(q => q.Status == "Pending" || q.Status == "Sent"),
                AverageTurnaroundDays = Math.Round((decimal)avgTurnaround, 1),
                RevenueToday = revenueToday,
                RevenueThisWeek = revenueThisWeek,
                PaymentsTotal = payments.Sum(p => p.Amount),
                OpenComplaints = AppDataStore.Complaints.Count(c => c.Status == "Open" || c.Status == "In Progress"),
                LowStockParts = AppDataStore.Parts.Count(p => p.QuantityInStock <= p.ReorderLevel),
                JobsThisWeekByDay = jobsThisWeek,
                BottleneckStatus = pipeline
                    .Where(p => p.Status != JobStatus.Collected)
                    .OrderByDescending(p => p.Count)
                    .FirstOrDefault()?.Status ?? "—"
            };

            return View(vm);
        }
    }

    /// <summary>
    /// Number of jobs in one status (pipeline).
    /// </summary>
    public class StatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// Job counts for one mechanic: total, active, completed and in progress.
    /// </summary>
    public class MechanicWorkload
    {
        public string MechanicId { get; set; } = string.Empty;
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int InProgress { get; set; }
    }

    /// <summary>
    /// Number of jobs created on one day, used for the weekly chart.
    /// </summary>
    public class DayCount
    {
        public string Label { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// View model for the performance page.
    /// </summary>
    public class WorkshopPerformanceVm
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int CompletedJobs { get; set; }
        public decimal CompletionRatePercent { get; set; }
        public int JobsCreatedToday { get; set; }
        public int JobsCreatedThisWeek { get; set; }
        public int ReadyForCollection { get; set; }
        public int InProgress { get; set; }
        public List<StatusCount> Pipeline { get; set; } = new();
        public List<MechanicWorkload> MechanicWorkloads { get; set; } = new();
        public int UnassignedActiveJobs { get; set; }
        public decimal QuoteApprovalRatePercent { get; set; }
        public int QuotesApproved { get; set; }
        public int QuotesRejected { get; set; }
        public int QuotesPendingOrSent { get; set; }
        public decimal AverageTurnaroundDays { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public decimal PaymentsTotal { get; set; }
        public int OpenComplaints { get; set; }
        public int LowStockParts { get; set; }
        public List<DayCount> JobsThisWeekByDay { get; set; } = new();
        public string BottleneckStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for the reports page.
    /// </summary>
    public class WorkshopReportVm
    {
        public DateTime GeneratedAt { get; set; }

        public int TotalVehicles { get; set; }
        public int TotalCustomers { get; set; }

        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public Dictionary<string, int> JobsByStatus { get; set; } = new();
        public int JobsCreatedToday { get; set; }
        public int JobsCollected { get; set; }
        public int JobsReadyForCollection { get; set; }

        public int TotalQuotations { get; set; }
        public Dictionary<string, int> QuotesByStatus { get; set; } = new();
        public decimal QuotesApprovedValue { get; set; }
        public decimal QuotesPendingValue { get; set; }

        public int PaymentsCount { get; set; }
        public decimal PaymentsTotal { get; set; }
        public decimal PaymentsToday { get; set; }
        public Dictionary<string, decimal> PaymentsByMethod { get; set; } = new();
        public decimal OutstandingBalance { get; set; }

        public int AppointmentsTotal { get; set; }
        public int AppointmentsToday { get; set; }
        public int AppointmentsUpcoming { get; set; }

        public int CollectionsReady { get; set; }
        public int CollectionsCompleted { get; set; }

        public int ComplaintsOpen { get; set; }
        public int ComplaintsResolved { get; set; }
        public int ComplaintsFromMobile { get; set; }

        public int PartsInStock { get; set; }
        public int PartsLowStock { get; set; }
        public decimal PartsInventoryValue { get; set; }

        public List<JobCard> RecentJobs { get; set; } = new();
        public List<Payment> RecentPayments { get; set; } = new();
    }
}
