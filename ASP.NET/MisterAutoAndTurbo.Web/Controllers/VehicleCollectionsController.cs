using MisterAutoAndTurbo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist / Manager: vehicles ready for collection and handover.
    /// Requires job status Ready for Collection (and preferably payment recorded).
    /// </summary>
    [Authorize(Roles = "Manager,Receptionist")]
    
    public class VehicleCollectionsController : Controller
    {
        [HttpGet]
        public IActionResult Index(string? status = null, string? q = null)
        {
            var list = AppDataStore.VehicleCollections.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(status))
                list = list.Where(c => string.Equals(c.CollectionStatus, status, StringComparison.OrdinalIgnoreCase));

            var customerNames = AppDataStore.Customers.ToDictionary(
                c => c.Id, c => $"{c.Name} {c.Surname}".Trim());
            var vehicleLabels = AppDataStore.Vehicles.ToDictionary(
                v => v.Id, v => $"{v.RegistrationNumber} — {v.Make} {v.Model}");

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                list = list.Where(c =>
                    (c.Id?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.JobCardId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.CollectionStatus?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.ManagedById?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.CustomerId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.VehicleId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (customerNames.TryGetValue(c.CustomerId ?? "", out var cn) && cn.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (vehicleLabels.TryGetValue(c.VehicleId ?? "", out var vl) && vl.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            ViewBag.StatusFilter = status;
            ViewBag.Query = q;
            ViewBag.CustomerNames = customerNames;
            ViewBag.VehicleLabels = vehicleLabels;

            var readyJobs = AppDataStore.JobCards
                .Where(j => j.Status == JobStatus.ReadyForCollection)
                .Where(j => !AppDataStore.VehicleCollections.Any(c =>
                    c.JobCardId == j.Id && c.CollectionStatus == "Collected"))
                .ToList();
            ViewBag.ReadyJobs = readyJobs;
            ViewBag.ReadyJobLabels = readyJobs.ToDictionary(
                j => j.Id,
                j =>
                {
                    var v = AppDataStore.Vehicles.FirstOrDefault(x => x.Id == j.VehichleId);
                    var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == j.CustomerId);
                    var reg = v?.RegistrationNumber ?? j.VehichleId;
                    var name = c != null ? $"{c.Name} {c.Surname}" : j.CustomerId;
                    return $"{j.Id} — {reg} ({name})";
                });

            return View(list.OrderByDescending(c => c.ScheduledAt).ToList());
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = AppDataStore.VehicleCollections.FirstOrDefault(c => c.Id == id);
            if (model == null)
                return NotFound();

            LoadDetailBags(model);
            return View(model);
        }

        [HttpGet]
        public IActionResult Manage(string? jobCardId = null, string? id = null)
        {
            VehicleCollection model;

            if (!string.IsNullOrWhiteSpace(id))
            {
                model = AppDataStore.VehicleCollections.FirstOrDefault(c => c.Id == id)
                        ?? new VehicleCollection();
                if (string.IsNullOrEmpty(model.Id))
                    return NotFound();
            }
            else if (!string.IsNullOrWhiteSpace(jobCardId))
            {
                model = AppDataStore.VehicleCollections.FirstOrDefault(c => c.JobCardId == jobCardId)
                        ?? CreateFromJob(jobCardId);
                if (model == null)
                {
                    TempData["Error"] = "Job card not found.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }

            LoadDetailBags(model);
            return View(model);
        }

        [HttpGet]
        public IActionResult Create(string? jobCardId = null)
        {
            var model = new VehicleCollection
            {
                JobCardId = jobCardId ?? string.Empty,
                CollectionStatus = "Ready",
                ScheduledAt = SaTime.Now.AddHours(1)
            };

            if (!string.IsNullOrWhiteSpace(jobCardId))
            {
                var fromJob = CreateFromJob(jobCardId);
                if (fromJob != null)
                    model = fromJob;
            }

            PopulateJobSelect(model.JobCardId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(VehicleCollection model)
        {
            PopulateJobSelect(model.JobCardId);

            if (string.IsNullOrWhiteSpace(model.JobCardId))
            {
                ModelState.AddModelError(nameof(model.JobCardId), "Select a job card.");
                return View(model);
            }

            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == model.JobCardId);
            if (job == null)
            {
                ModelState.AddModelError(nameof(model.JobCardId), "Job card not found.");
                return View(model);
            }

            // Prefer jobs that are Ready for Collection
            if (job.Status != JobStatus.ReadyForCollection && job.Status != JobStatus.Collected)
            {
                // Allow create but warn via TempData after save — or soft-block
                TempData["Error"] = $"Note: job {job.Id} status is “{job.Status}”. Collection is normally for Ready for Collection.";
            }

            model.Id = AppDataStore.NewId("col");
            model.CustomerId = job.CustomerId;
            model.VehicleId = job.VehichleId;
            if (string.IsNullOrWhiteSpace(model.CollectionStatus))
                model.CollectionStatus = "Ready";
            if (string.IsNullOrWhiteSpace(model.ManagedById))
                model.ManagedById = "staff";
            if (model.ScheduledAt == default)
                model.ScheduledAt = SaTime.Now;
            model.Notes = model.Notes?.Trim() ?? string.Empty;

            // Ensure job is at least Ready for Collection when scheduling collection
            if (job.Status != JobStatus.Collected && job.Status != JobStatus.ReadyForCollection)
                job.Status = JobStatus.ReadyForCollection;

            AppDataStore.VehicleCollections.Add(model);

            // Notify customer
            var cust = AppDataStore.Customers.FirstOrDefault(c => c.Id == model.CustomerId);
            var veh = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == model.VehicleId);
            var name = cust != null ? $"{cust.Name} {cust.Surname}".Trim() : "Customer";
            var reg = veh?.RegistrationNumber ?? "your vehicle";
            AppDataStore.Notifications.Add(new Notification
            {
                Id = AppDataStore.NewId("ntf"),
                UserAccountId = model.CustomerId,
                Type = "Collection",
                RelatedId = model.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Message = $"Hi {name}, {reg} is ready for collection at Mister Auto & Turbo. Please arrange pickup."
            });

            TempData["Success"] = "Collection record created. Customer notified.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string jobCardId, string collectionStatus, DateTime? collectedAt, string? notes, string? managedById)
        {
            VehicleCollection? record = null;
            if (!string.IsNullOrWhiteSpace(id))
                record = AppDataStore.VehicleCollections.FirstOrDefault(c => c.Id == id);

            if (record == null && !string.IsNullOrWhiteSpace(jobCardId))
                record = AppDataStore.VehicleCollections.FirstOrDefault(c => c.JobCardId == jobCardId);

            if (record == null && !string.IsNullOrWhiteSpace(jobCardId))
            {
                // Create on the fly
                var created = CreateFromJob(jobCardId);
                if (created == null)
                {
                    TempData["Error"] = "Job card not found.";
                    return RedirectToAction(nameof(Index));
                }
                created.Id = AppDataStore.NewId("col");
                AppDataStore.VehicleCollections.Add(created);
                record = created;
            }

            if (record == null)
            {
                TempData["Error"] = "Collection record not found.";
                return RedirectToAction(nameof(Index));
            }

            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == record.JobCardId);
            var allowed = new[] { "Pending", "Ready", "Collected", "Cancelled" };
            if (!allowed.Contains(collectionStatus))
            {
                TempData["Error"] = "Invalid collection status.";
                return RedirectToAction(nameof(Manage), new { id = record.Id });
            }

            // Enforce: mark Collected only if job is Ready for Collection (or already Collected)
            if (collectionStatus == "Collected")
            {
                if (job == null)
                {
                    TempData["Error"] = "Linked job card not found.";
                    return RedirectToAction(nameof(Manage), new { id = record.Id });
                }

                if (job.Status != JobStatus.ReadyForCollection && job.Status != JobStatus.Collected)
                {
                    TempData["Error"] = $"Vehicle not ready for collection. Job status is “{job.Status}”. Set the job to Ready for Collection first.";
                    return RedirectToAction(nameof(Manage), new { id = record.Id });
                }

                // Soft payment check — warn if no payment but still allow (workshop policy)
                var hasPayment = AppDataStore.Payments.Any(p =>
                    p.CustomerId == record.CustomerId && p.Status != "Refunded");
                // Also check quotes for this job
                var quoteIds = AppDataStore.Quotations
                    .Where(q => q.JobCardId == record.JobCardId)
                    .Select(q => q.Id)
                    .ToHashSet();
                var paidOnQuote = AppDataStore.Payments.Any(p =>
                    quoteIds.Contains(p.QuotationId) && p.Status != "Refunded");

                if (!hasPayment && !paidOnQuote)
                {
                    TempData["Error"] = "No payment recorded for this customer/job. Record payment before completing collection, or confirm cash on handover in Notes.";
                    // Still allow if they set notes - for demo we'll block strict
                    // Change to allow with warning only:
                    // TempData["Error"] stays; user can add note and try again with policy override
                    // For practical workshop flow, allow complete but flag:
                    TempData["Error"] = null;
                    TempData["Success"] = "Collected — note: no payment was found on file. Confirm payment with reception.";
                }

                job.Status = JobStatus.Collected;
                record.CollectedAt = collectedAt ?? SaTime.Now;
            }

            record.CollectionStatus = collectionStatus;
            if (!string.IsNullOrWhiteSpace(notes))
                record.Notes = notes.Trim();
            if (!string.IsNullOrWhiteSpace(managedById))
                record.ManagedById = managedById.Trim();

            if (collectionStatus == "Ready" && job != null && job.Status != JobStatus.Collected)
                job.Status = JobStatus.ReadyForCollection;

            if (string.IsNullOrEmpty(TempData["Success"] as string))
                TempData["Success"] = $"Collection status set to {collectionStatus}.";

            return RedirectToAction(nameof(Details), new { id = record.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NotifyCustomer(string id)
        {
            var record = AppDataStore.VehicleCollections.FirstOrDefault(c => c.Id == id);
            if (record == null)
                return NotFound();

            var cust = AppDataStore.Customers.FirstOrDefault(c => c.Id == record.CustomerId);
            var veh = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == record.VehicleId);
            var name = cust != null ? $"{cust.Name} {cust.Surname}".Trim() : "Customer";
            var reg = veh?.RegistrationNumber ?? "your vehicle";

            AppDataStore.Notifications.Add(new Notification
            {
                Id = AppDataStore.NewId("ntf"),
                UserAccountId = record.CustomerId,
                Type = "Collection",
                RelatedId = record.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Message = $"Hi {name}, {reg} is ready for collection at Mister Auto & Turbo."
            });

            TempData["Success"] = $"Collection notification sent to {name}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            var record = AppDataStore.VehicleCollections.FirstOrDefault(c => c.Id == id);
            if (record == null)
                return NotFound();

            AppDataStore.VehicleCollections.Remove(record);
            TempData["Success"] = "Collection record deleted.";
            return RedirectToAction(nameof(Index));
        }

        // --- helpers ---

        private static VehicleCollection? CreateFromJob(string jobCardId)
        {
            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == jobCardId);
            if (job == null)
                return null;

            return new VehicleCollection
            {
                JobCardId = job.Id,
                CustomerId = job.CustomerId,
                VehicleId = job.VehichleId,
                CollectionStatus = job.Status == JobStatus.Collected ? "Collected" : "Ready",
                ScheduledAt = SaTime.Now,
                ManagedById = "staff"
            };
        }

        private void LoadDetailBags(VehicleCollection model)
        {
            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == model.JobCardId);
            var vehicle = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == model.VehicleId || v.Id == job?.VehichleId);
            var customer = AppDataStore.Customers.FirstOrDefault(c => c.Id == model.CustomerId || c.Id == job?.CustomerId);

            ViewBag.Job = job;
            ViewBag.Vehicle = vehicle;
            ViewBag.Customer = customer;
            ViewBag.CustomerName = customer != null ? $"{customer.Name} {customer.Surname}".Trim() : model.CustomerId;
            ViewBag.VehicleLabel = vehicle != null
                ? $"{vehicle.RegistrationNumber} — {vehicle.Make} {vehicle.Model}"
                : model.VehicleId;

            var quoteIds = AppDataStore.Quotations.Where(q => q.JobCardId == model.JobCardId).Select(q => q.Id).ToHashSet();
            ViewBag.Payments = AppDataStore.Payments
                .Where(p => quoteIds.Contains(p.QuotationId) || p.CustomerId == model.CustomerId)
                .OrderByDescending(p => p.PaidAt)
                .ToList();
        }

        private void PopulateJobSelect(string? selectedId)
        {
            ViewBag.Jobs = AppDataStore.JobCards
                .Where(j => j.Status != JobStatus.Collected || j.Id == selectedId)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j =>
                {
                    var v = AppDataStore.Vehicles.FirstOrDefault(x => x.Id == j.VehichleId);
                    var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == j.CustomerId);
                    var label = $"{j.Id} — {(v?.RegistrationNumber ?? j.VehichleId)} · {(c != null ? c.Name + " " + c.Surname : j.CustomerId)} ({j.Status})";
                    return new SelectListItem { Value = j.Id, Text = label, Selected = j.Id == selectedId };
                })
                .ToList();
        }
    }
}
