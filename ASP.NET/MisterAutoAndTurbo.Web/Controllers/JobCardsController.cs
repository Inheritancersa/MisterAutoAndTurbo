using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist: Create Job Card, Manage Job Card (incl. service/repair request).
    /// Mechanic: View Assigned Jobs, View Job Card, Update Repair Progress.
    /// Manager: View Active Jobs, View Completed Jobs, Monitor Job Progress.
    /// Job statuses: Inspection, Quotation, In Progress, Quality Check, Ready for Collection, Collected.
    /// </summary>
    public class JobCardsController : Controller
    {
        // GET: /JobCards
        [HttpGet]
        public IActionResult Index(string? status = null)
        {
            // TODO: Retrieve job cards (filter by status / role)
            // Manager: active vs completed; Mechanic: assigned only
            var jobs = new List<JobCard>();
            ViewData["StatusFilter"] = status;
            return View(jobs);
        }

        // GET: /JobCards/Active
        [HttpGet]
        public IActionResult Active()
        {
            // Manager: View Active Jobs
            return RedirectToAction(nameof(Index), new { status = "Active" });
        }

        // GET: /JobCards/Completed
        [HttpGet]
        public IActionResult Completed()
        {
            // Manager: View Completed Jobs
            return RedirectToAction(nameof(Index), new { status = "Collected" });
        }

        // GET: /JobCards/Assigned  (Mechanic: View Assigned Jobs)
        [HttpGet]
        public IActionResult Assigned()
        {
            // TODO: Filter by current mechanic id from session
            var jobs = new List<JobCard>();
            return View("Index", jobs);
        }

        // GET: /JobCards/Details/{id}
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            // TODO: Retrieve job card details from Supabase
            // alt [job card found] -> Display job card
            // else -> Display "job card not found"
            var job = new JobCard { Id = id };
            return View(job);
        }

        // GET: /JobCards/Create
        [HttpGet]
        public IActionResult Create(string? customerId = null, string? vehicleId = null)
        {
            var model = new JobCard
            {
                Status = "Inspection",
                CreatedAt = DateTime.UtcNow
            };
            ViewData["CustomerId"] = customerId;
            ViewData["VehicleId"] = vehicleId;
            return View(model);
        }

        // POST: /JobCards/Create  (Receptionist: Create Job Card)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(JobCard model, string? serviceDescription = null)
        {
            // alt [customer and vehicle registered] -> Save job card
            // else -> Display "register customer / vehicle first"
            if (string.IsNullOrWhiteSpace(model.VehichleId))
            {
                ModelState.AddModelError(string.Empty, "Register customer / vehicle first.");
                return View(model);
            }

            // TODO: Save job card; optionally include Record Service/Repair Request
            model.Id = Guid.NewGuid().ToString();
            model.CreatedAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(model.Status))
                model.Status = "Inspection";

            TempData["Success"] = "Job card created.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // GET: /JobCards/Edit/{id}
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            // TODO: Load job card
            var job = new JobCard { Id = id };
            return View(job);
        }

        // POST: /JobCards/Edit/{id}  (Manage Job Card / Open update)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, JobCard model)
        {
            if (id != model.Id)
                return NotFound();

            // TODO: Update job card in Supabase
            TempData["Success"] = "Job card updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /JobCards/UpdateStatus/{id}  (Mechanic: Update Repair Progress)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string status)
        {
            // Allowed: In Progress | Quality Check | Ready for Collection
            var allowed = new[] { "In Progress", "Quality Check", "Ready for Collection", "Inspection", "Quotation", "Collected" };
            if (!allowed.Contains(status))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // TODO: Save status to Supabase; notify customer on Ready for Collection
            TempData["Success"] = $"Status updated to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /JobCards/AddRepairNotes/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddRepairNotes(string id, string repairNotes)
        {
            if (string.IsNullOrWhiteSpace(repairNotes))
            {
                TempData["Error"] = "Notes are required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // TODO: Save repair notes on job card
            TempData["Success"] = "Repair notes saved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /JobCards/RecordDiagnosis/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecordDiagnosis(string id, string diagnosis)
        {
            // Mechanic: Record Diagnosis (requires inspection first)
            if (string.IsNullOrWhiteSpace(diagnosis))
            {
                TempData["Error"] = "Diagnosis is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // TODO: Save diagnosis; if no inspection -> "record inspection first"
            TempData["Success"] = "Diagnosis saved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /JobCards/Progress  (Manager: Monitor Job Progress)
        [HttpGet]
        public IActionResult Progress()
        {
            // TODO: Retrieve job status updates
            var jobs = new List<JobCard>();
            return View(jobs);
        }
    }
}
