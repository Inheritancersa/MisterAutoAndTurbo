using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist: Create / Manage Job Card.
    /// Mechanic: Assigned jobs, diagnosis, notes, status.
    /// Manager: Active / completed / progress.
    /// </summary>
    public class JobCardsController : Controller
    {
        [HttpGet]
        public IActionResult Index(string? status = null)
        {
            IEnumerable<JobCard> jobs = AppDataStore.JobCards;

            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                jobs = jobs.Where(j => j.Status != JobStatus.Collected);
            else if (!string.IsNullOrWhiteSpace(status))
                jobs = jobs.Where(j => string.Equals(j.Status, status, StringComparison.OrdinalIgnoreCase));

            ViewData["StatusFilter"] = status;
            LoadVehicleLabels();
            return View(jobs.OrderByDescending(j => j.CreatedAt).ToList());
        }

        [HttpGet]
        public IActionResult Active() => RedirectToAction(nameof(Index), new { status = "Active" });

        [HttpGet]
        public IActionResult Completed() => RedirectToAction(nameof(Index), new { status = JobStatus.Collected });

        [HttpGet]
        public IActionResult Assigned()
        {
            var jobs = AppDataStore.JobCards
                .Where(j => !string.IsNullOrWhiteSpace(j.MechanicId))
                .OrderByDescending(j => j.CreatedAt)
                .ToList();

            ViewData["StatusFilter"] = "Assigned";
            LoadVehicleLabels();
            return View("Index", jobs);
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == id);
            if (job == null)
                return NotFound();

            LoadVehicleLabels();

            var vehicle = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == job.VehichleId);
            ViewBag.Vehicle = vehicle;
            ViewBag.Customer = vehicle == null
                ? null
                : AppDataStore.Customers.FirstOrDefault(c => c.Id == vehicle.CustomerId);

            ViewBag.ServiceRequest = AppDataStore.ServiceRequests
                .FirstOrDefault(r => r.VehicleId == job.VehichleId && r.CreatedAt == job.CreatedAt)?.Description;

            return View(job);
        }

        [HttpGet]
        public IActionResult Create(string? customerId = null, string? vehicleId = null)
        {
            var jobCard = new JobCard
            {
                VehichleId = vehicleId ?? string.Empty,
                CustomerId = customerId ?? string.Empty,
                Status = JobStatus.Inspection
            };

            PopulateVehicles();
            return View(jobCard);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(JobCard jobCard, string? serviceDescription = null)
        {
            var vehicle = string.IsNullOrWhiteSpace(jobCard.VehichleId)
                ? null
                : AppDataStore.Vehicles.FirstOrDefault(v => v.Id == jobCard.VehichleId);

            if (vehicle == null)
            {
                ModelState.AddModelError(string.Empty, "Select a registered vehicle. Register the customer / vehicle first if it isn't listed.");
                PopulateVehicles();
                return View(jobCard);
            }

            var now = DateTime.Now;

            var saved = new JobCard
            {
                Id = AppDataStore.NextJobCardId(),
                VehichleId = vehicle.Id,
                CustomerId = vehicle.CustomerId,
                CreatedById = jobCard.CreatedById?.Trim() ?? string.Empty,
                MechanicId = jobCard.MechanicId?.Trim() ?? string.Empty,
                Status = JobStatus.All.Contains(jobCard.Status) ? jobCard.Status : JobStatus.Inspection,
                Diagnosis = jobCard.Diagnosis ?? string.Empty,
                RepairNotes = jobCard.RepairNotes ?? string.Empty,
                CreatedAt = now
            };
            AppDataStore.JobCards.Add(saved);

            if (!string.IsNullOrWhiteSpace(serviceDescription))
            {
                var text = serviceDescription.Trim();
                AppDataStore.ServiceRequests.Add(new ServiceRequest
                {
                    Id = AppDataStore.NewId("sr"),
                    CustomerId = vehicle.CustomerId,
                    VehicleId = vehicle.Id,
                    RequestedService = text.Length > 80 ? text.Substring(0, 80) : text,
                    Description = text,
                    Status = "Pending",
                    CreatedAt = now
                });
            }

            TempData["Success"] = $"Job card {saved.Id} created.";
            return RedirectToAction(nameof(Details), new { id = saved.Id });
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == id);
            if (job == null)
                return NotFound();

            PopulateVehicles();
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, JobCard jobCard)
        {
            if (id != jobCard.Id)
                return NotFound();

            var existing = AppDataStore.JobCards.FirstOrDefault(j => j.Id == id);
            if (existing == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(jobCard.VehichleId) ||
                !AppDataStore.Vehicles.Any(v => v.Id == jobCard.VehichleId))
            {
                ModelState.AddModelError(string.Empty, "Select a registered vehicle.");
                PopulateVehicles();
                return View(jobCard);
            }

            var vehicle = AppDataStore.Vehicles.First(v => v.Id == jobCard.VehichleId);
            existing.VehichleId = jobCard.VehichleId;
            existing.CustomerId = vehicle.CustomerId;
            existing.MechanicId = jobCard.MechanicId?.Trim() ?? string.Empty;
            existing.Status = JobStatus.All.Contains(jobCard.Status) ? jobCard.Status : existing.Status;
            existing.Diagnosis = jobCard.Diagnosis?.Trim() ?? string.Empty;
            existing.RepairNotes = jobCard.RepairNotes?.Trim() ?? string.Empty;

            TempData["Success"] = "Job card updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string status)
        {
            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == id);
            if (job == null)
                return NotFound();

            if (!JobStatus.All.Contains(status))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            job.Status = status;
            TempData["Success"] = $"Status updated to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddRepairNotes(string id, string repairNotes)
        {
            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == id);
            if (job == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(repairNotes))
            {
                TempData["Error"] = "Notes are required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var entry = $"[{DateTime.Now:dd MMM yyyy HH:mm}] {repairNotes.Trim()}";
            job.RepairNotes = string.IsNullOrWhiteSpace(job.RepairNotes)
                ? entry
                : job.RepairNotes + Environment.NewLine + entry;

            TempData["Success"] = "Repair notes saved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecordDiagnosis(string id, string diagnosis)
        {
            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == id);
            if (job == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(diagnosis))
            {
                TempData["Error"] = "Diagnosis is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            job.Diagnosis = diagnosis.Trim();
            TempData["Success"] = "Diagnosis saved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public IActionResult Progress()
        {
            LoadVehicleLabels();
            return View(AppDataStore.JobCards.OrderByDescending(j => j.CreatedAt).ToList());
        }

        private static string VehicleLabel(Vehicle v)
        {
            var owner = AppDataStore.Customers.FirstOrDefault(c => c.Id == v.CustomerId);
            var ownerName = owner == null ? "" : $" ({owner.Name} {owner.Surname})".TrimEnd();
            return $"{v.RegistrationNumber} — {v.Make} {v.Model}{ownerName}";
        }

        private void PopulateVehicles()
        {
            ViewBag.Vehicles = AppDataStore.Vehicles
                .OrderBy(v => v.RegistrationNumber)
                .Select(v => new SelectListItem { Value = v.Id, Text = VehicleLabel(v) })
                .ToList();
        }

        private void LoadVehicleLabels()
        {
            ViewBag.VehicleLabels = AppDataStore.Vehicles
                .ToDictionary(v => v.Id, v => VehicleLabel(v));
        }
    }
}
