using MisterAutoAndTurbo.Web.Services;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "Manager,Receptionist,Mechanic")]
    
    public class JobCardsController : Controller
    {
        /// <summary>JSON: job cards for one customer (appointment / quotation forms).</summary>
        [HttpGet]
        public IActionResult JsonByCustomer(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return Json(Array.Empty<object>());

            var list = AppDataStore.JobCards
                .Where(j => j.CustomerId == customerId)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j =>
                {
                    var v = AppDataStore.Vehicles.FirstOrDefault(x => x.Id == j.VehichleId);
                    return new
                    {
                        id = j.Id,
                        text = $"{j.Id} — {(v != null ? v.RegistrationNumber : j.VehichleId)} ({j.Status})"
                    };
                })
                .ToList();
            return Json(list);
        }


        [HttpGet]
        public IActionResult Index(string? status = null, string? q = null)
        {
            IEnumerable<JobCard> jobs = AppDataStore.JobCards;

            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                jobs = jobs.Where(j => j.Status != JobStatus.Collected);
            else if (!string.IsNullOrWhiteSpace(status))
                jobs = jobs.Where(j => string.Equals(j.Status, status, StringComparison.OrdinalIgnoreCase));

            LoadVehicleLabels();
            var vehicleLabels = ViewBag.VehicleLabels as Dictionary<string, string>
                ?? new Dictionary<string, string>();
            var customerNames = AppDataStore.Customers.ToDictionary(
                c => c.Id,
                c => $"{c.Name} {c.Surname}".Trim());

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                jobs = jobs.Where(j =>
                    (j.Id?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.Status?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.Diagnosis?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.RepairNotes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.MechanicId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.CustomerId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.VehichleId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (customerNames.TryGetValue(j.CustomerId, out var cn) && cn.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (vehicleLabels.TryGetValue(j.VehichleId, out var vl) && vl.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            ViewData["StatusFilter"] = status;
            ViewBag.Query = q;
            ViewBag.CustomerNames = customerNames;
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
            ViewBag.CustomerNames = AppDataStore.Customers.ToDictionary(
                c => c.Id,
                c => $"{c.Name} {c.Surname}".Trim());
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

            // Prefer ServiceRequest created with this job; fall back to appointment linked to this job card.
            var serviceReq = AppDataStore.ServiceRequests
                .FirstOrDefault(r => r.VehicleId == job.VehichleId && r.CreatedAt == job.CreatedAt)
                ?? AppDataStore.ServiceRequests
                    .Where(r => r.VehicleId == job.VehichleId && r.CustomerId == job.CustomerId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault();

            var linkedAppointment = AppDataStore.Appointments
                .FirstOrDefault(a => a.JobCardId == job.Id);

            ViewBag.ServiceType = serviceReq?.RequestedService
                ?? linkedAppointment?.ServiceType
                ?? string.Empty;
            ViewBag.ServiceDescription = serviceReq?.Description
                ?? linkedAppointment?.Description
                ?? string.Empty;
            // Keep legacy ViewBag for older views
            ViewBag.ServiceRequest = ViewBag.ServiceDescription;

            return View(job);
        }

        [Authorize(Roles = "Manager,Receptionist")]
        [HttpGet]
        public IActionResult Create(string? customerId = null, string? vehicleId = null, string? appointmentId = null)
        {
            var jobCard = new JobCard
            {
                VehichleId = vehicleId ?? string.Empty,
                CustomerId = customerId ?? string.Empty,
                Status = JobStatus.Inspection
            };

            ViewBag.AppointmentId = appointmentId ?? string.Empty;
            ViewBag.ServiceType = string.Empty;
            ViewBag.ServiceDescription = string.Empty;

            if (!string.IsNullOrWhiteSpace(appointmentId))
            {
                var appointment = AppDataStore.Appointments.FirstOrDefault(a => a.Id == appointmentId);
                if (appointment != null)
                {
                    jobCard.CustomerId = appointment.CustomerId;
                    jobCard.VehichleId = appointment.VehicleId;

                    var serviceType = appointment.ServiceType ?? string.Empty;
                    var description = appointment.Description ?? string.Empty;

                    // Back-fill from ServiceRequest when appointment fields are empty (older mobile bookings)
                    if (string.IsNullOrWhiteSpace(serviceType) || string.IsNullOrWhiteSpace(description))
                    {
                        var sr = AppDataStore.ServiceRequests
                            .Where(r => r.CustomerId == appointment.CustomerId && r.VehicleId == appointment.VehicleId)
                            .OrderByDescending(r => r.CreatedAt)
                            .FirstOrDefault();
                        if (sr != null)
                        {
                            if (string.IsNullOrWhiteSpace(serviceType))
                                serviceType = sr.RequestedService ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(description))
                                description = sr.Description ?? string.Empty;
                        }
                    }

                    ViewBag.ServiceType = serviceType;
                    ViewBag.ServiceDescription = description;
                    ViewBag.AppointmentId = appointment.Id;
                }
            }

            PopulateVehicles();
            return View(jobCard);
        }

        [Authorize(Roles = "Manager,Receptionist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(JobCard jobCard, string? serviceDescription = null, string? serviceType = null, string? appointmentId = null)
        {
            var vehicle = string.IsNullOrWhiteSpace(jobCard.VehichleId)
                ? null
                : AppDataStore.Vehicles.FirstOrDefault(v => v.Id == jobCard.VehichleId);

            if (vehicle == null)
            {
                ModelState.AddModelError(string.Empty, "Select a registered vehicle. Register the customer / vehicle first if it isn't listed.");
                ViewBag.AppointmentId = appointmentId ?? string.Empty;
                ViewBag.ServiceType = serviceType ?? string.Empty;
                ViewBag.ServiceDescription = serviceDescription ?? string.Empty;
                PopulateVehicles();
                return View(jobCard);
            }

            var now = DateTime.UtcNow;

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

            var typeText = (serviceType ?? string.Empty).Trim();
            var descText = (serviceDescription ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(typeText) || !string.IsNullOrWhiteSpace(descText))
            {
                var requested = !string.IsNullOrWhiteSpace(typeText)
                    ? typeText
                    : (descText.Length > 80 ? descText.Substring(0, 80) : descText);
                AppDataStore.ServiceRequests.Add(new ServiceRequest
                {
                    Id = AppDataStore.NewId("sr"),
                    CustomerId = vehicle.CustomerId,
                    VehicleId = vehicle.Id,
                    RequestedService = requested,
                    Description = !string.IsNullOrWhiteSpace(descText) ? descText : requested,
                    Status = "Pending",
                    CreatedAt = now
                });
            }

            // Link the appointment to this job card so Service/Description stay connected
            if (!string.IsNullOrWhiteSpace(appointmentId))
            {
                var appointment = AppDataStore.Appointments.FirstOrDefault(a => a.Id == appointmentId.Trim());
                if (appointment != null)
                {
                    appointment.JobCardId = saved.Id;
                    if (string.IsNullOrWhiteSpace(appointment.ServiceType) && !string.IsNullOrWhiteSpace(typeText))
                        appointment.ServiceType = typeText;
                    if (string.IsNullOrWhiteSpace(appointment.Description) && !string.IsNullOrWhiteSpace(descText))
                        appointment.Description = descText;
                }
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

            var entry = $"[{SaTime.Now:dd MMM yyyy HH:mm}] {repairNotes.Trim()}";
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

        [Authorize(Roles = "Manager,Receptionist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == id);
            if (job == null)
                return NotFound();

            AppDataStore.JobCards.Remove(job);
            TempData["Success"] = $"Job card {id} deleted.";
            return RedirectToAction(nameof(Index));
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
