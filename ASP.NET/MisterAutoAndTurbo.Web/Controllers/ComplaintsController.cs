using MisterAutoAndTurbo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Customer (mobile): submit complaint → staff notification.
    /// Receptionist: record complaint. Manager: view and resolve.
    /// </summary>
    // Complaint handling for Manager and Receptionist. The CustomerSubmit pages mimic the mobile form that customers use.
    [Authorize(Roles = "Manager,Receptionist")]
    
    public class ComplaintsController : Controller
    {
        // GET /Complaints: lists complaints newest first. Filters: status, customer and free text (id, subject, description, status, source, job, vehicle or customer name). Passes the open count and lookup data to the view.
        [HttpGet]
        public IActionResult Index(string? status = null, string? customerId = null, string? q = null)
        {
            var list = AppDataStore.Complaints.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status))
                list = list.Where(c => string.Equals(c.Status, status, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(customerId))
                list = list.Where(c => c.CustomerId == customerId);

            var customerNames = AppDataStore.Customers.ToDictionary(
                c => c.Id, c => $"{c.Name} {c.Surname}".Trim());

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                list = list.Where(c =>
                    (c.Id?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Subject?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Status?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Source?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.JobCardId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.CustomerId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.VehicleId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (customerNames.TryGetValue(c.CustomerId ?? "", out var cn) && cn.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            ViewBag.StatusFilter = status;
            ViewBag.CustomerFilter = customerId;
            ViewBag.Query = q;
            ViewBag.CustomerNames = customerNames;
            ViewBag.OpenCount = AppDataStore.Complaints.Count(c => c.Status == "Open" || c.Status == "In Progress");
            ViewBag.Customers = BuildCustomerSelect(customerId);

            return View(list.OrderByDescending(c => c.CreatedAt).ToList());
        }

        // GET: shows one complaint with the customer name and vehicle label (404 if not found).
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = AppDataStore.Complaints.FirstOrDefault(c => c.Id == id);
            if (model == null)
                return NotFound();

            ViewBag.CustomerName = CustomerName(model.CustomerId);
            ViewBag.VehicleLabel = VehicleLabel(model.VehicleId);
            return View(model);
        }

        /// <summary>Staff records a complaint (phone / walk-in).</summary>
        // GET: shows the staff complaint form. When opened from a job card, the job, customer and vehicle are prefilled.
        [HttpGet]
        public IActionResult Create(string? customerId = null, string? jobCardId = null)
        {
            var model = new Complaint
            {
                CustomerId = customerId ?? string.Empty,
                JobCardId = jobCardId ?? string.Empty,
                Status = "Open",
                Source = "Staff",
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(jobCardId))
            {
                var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == jobCardId);
                if (job != null)
                {
                    model.JobCardId = job.Id;
                    model.CustomerId = job.CustomerId;
                    model.VehicleId = job.VehichleId;
                }
            }

            PopulateLookups(model);
            return View(model);
        }

        // POST: requires a customer and description, then saves the complaint through SaveComplaint (which also notifies staff).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Complaint model)
        {
            PopulateLookups(model);

            if (string.IsNullOrWhiteSpace(model.CustomerId) || string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(string.Empty, "Customer and description are required.");
                return View(model);
            }

            SaveComplaint(model, source: string.IsNullOrWhiteSpace(model.Source) ? "Staff" : model.Source, notifyStaff: true);

            TempData["Success"] = "Complaint recorded and staff notified.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        /// <summary>
        /// Mobile-style form for the customer to submit a complaint.
        /// GET /Complaints/CustomerSubmit?customerId=cust-001
        /// </summary>
        // GET: shows the customer-facing complaint form with source set to Mobile.
        [HttpGet]
        public IActionResult CustomerSubmit(string? customerId = null)
        {
            var model = new Complaint
            {
                CustomerId = customerId ?? string.Empty,
                Status = "Open",
                Source = "Mobile",
                CreatedAt = DateTime.UtcNow
            };
            PopulateLookups(model);
            return View(model);
        }

        /// <summary>Customer posts complaint from mobile → saved + notification to staff.</summary>
        // POST: requires a customer and description, saves the complaint as a Mobile complaint, notifies staff and redirects to the thank-you page.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CustomerSubmit(Complaint model)
        {
            PopulateLookups(model);

            if (string.IsNullOrWhiteSpace(model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Please select your account / customer.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(nameof(model.Description), "Please describe the complaint.");
                return View(model);
            }

            SaveComplaint(model, source: "Mobile", notifyStaff: true);

            TempData["Success"] = "Your complaint was submitted. Mister Auto & Turbo will follow up soon.";
            return RedirectToAction(nameof(CustomerSubmitDone), new { id = model.Id });
        }

        // GET: confirmation page after a customer submitted a complaint.
        [HttpGet]
        public IActionResult CustomerSubmitDone(string id)
        {
            var model = AppDataStore.Complaints.FirstOrDefault(c => c.Id == id);
            if (model == null)
                return RedirectToAction(nameof(CustomerSubmit));
            ViewBag.CustomerName = CustomerName(model.CustomerId);
            return View(model);
        }

        // POST: changes the status (Open, In Progress, Resolved or Closed) and optional staff notes. Resolved or Closed sets ResolvedAt and sends the customer a notification.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string status, string? staffNotes = null)
        {
            var model = AppDataStore.Complaints.FirstOrDefault(c => c.Id == id);
            if (model == null)
                return NotFound();

            var allowed = new[] { "Open", "In Progress", "Resolved", "Closed" };
            if (!allowed.Contains(status))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            model.Status = status;
            if (!string.IsNullOrWhiteSpace(staffNotes))
                model.StaffNotes = staffNotes.Trim();

            if (status == "Resolved" || status == "Closed")
                model.ResolvedAt = DateTime.UtcNow;

            // Notify customer when resolved
            if (status == "Resolved" || status == "Closed")
            {
                AppDataStore.Notifications.Add(new Notification
                {
                    Id = AppDataStore.NewId("ntf"),
                    UserAccountId = model.CustomerId,
                    Type = "Complaint",
                    RelatedId = model.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Message = $"Your complaint “{(string.IsNullOrEmpty(model.Subject) ? model.Id : model.Subject)}” was marked {status} by Mister Auto & Turbo."
                });
            }

            TempData["Success"] = $"Complaint status set to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: deletes a complaint together with the notifications that point to it.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            var model = AppDataStore.Complaints.FirstOrDefault(c => c.Id == id);
            if (model == null)
                return NotFound();

            AppDataStore.Complaints.Remove(model);
            foreach (var n in AppDataStore.Notifications.Where(n => n.RelatedId == id).ToList())
                AppDataStore.Notifications.Remove(n);

            TempData["Success"] = "Complaint deleted.";
            return RedirectToAction(nameof(Index));
        }

        // --- helpers ---

        // Saves a new complaint: creates its id (cmp-xxxx), sets time, source and status Open, trims text, fills the vehicle and customer from the job card when missing, and (optionally) adds a staff notification with a short preview of the description.
        private void SaveComplaint(Complaint model, string source, bool notifyStaff)
        {
            model.Id = AppDataStore.NewId("cmp");
            model.CreatedAt = DateTime.UtcNow;
            model.Source = source;
            model.Status = "Open";
            model.Subject = model.Subject?.Trim() ?? string.Empty;
            model.Description = model.Description.Trim();
            model.JobCardId = model.JobCardId?.Trim() ?? string.Empty;
            model.VehicleId = model.VehicleId?.Trim() ?? string.Empty;

            // Fill vehicle from job if needed
            if (string.IsNullOrWhiteSpace(model.VehicleId) && !string.IsNullOrWhiteSpace(model.JobCardId))
            {
                var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == model.JobCardId);
                if (job != null)
                {
                    model.VehicleId = job.VehichleId;
                    if (string.IsNullOrWhiteSpace(model.CustomerId))
                        model.CustomerId = job.CustomerId;
                }
            }

            AppDataStore.Complaints.Add(model);

            if (notifyStaff)
            {
                var name = CustomerName(model.CustomerId);
                var preview = model.Description.Length > 80
                    ? model.Description.Substring(0, 80) + "…"
                    : model.Description;

                AppDataStore.Notifications.Add(new Notification
                {
                    Id = AppDataStore.NewId("ntf"),
                    UserAccountId = "staff",
                    Type = "Complaint",
                    RelatedId = model.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Message = $"New complaint from {name} ({source}): {(string.IsNullOrEmpty(model.Subject) ? preview : model.Subject + " — " + preview)}"
                });
            }
        }

        // Fills the dropdown lists: all customers, plus only the vehicles and job cards of the selected customer.
        private void PopulateLookups(Complaint model)
        {
            ViewBag.Customers = BuildCustomerSelect(model.CustomerId);

            // Only vehicles + job cards for the selected customer.
            ViewBag.Vehicles = string.IsNullOrWhiteSpace(model.CustomerId)
                ? new List<SelectListItem>()
                : AppDataStore.Vehicles
                    .Where(v => v.CustomerId == model.CustomerId)
                    .OrderBy(v => v.RegistrationNumber)
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id,
                        Text = $"{v.RegistrationNumber} — {v.Make} {v.Model}",
                        Selected = v.Id == model.VehicleId
                    })
                    .ToList();

            ViewBag.JobCards = string.IsNullOrWhiteSpace(model.CustomerId)
                ? new List<SelectListItem>()
                : AppDataStore.JobCards
                    .Where(j => j.CustomerId == model.CustomerId)
                    .OrderByDescending(j => j.CreatedAt)
                    .Select(j =>
                    {
                        var v = AppDataStore.Vehicles.FirstOrDefault(x => x.Id == j.VehichleId);
                        return new SelectListItem
                        {
                            Value = j.Id,
                            Text = $"{j.Id} — {v?.RegistrationNumber ?? j.VehichleId} ({j.Status})",
                            Selected = j.Id == model.JobCardId
                        };
                    })
                    .ToList();
        }

        // Builds the customer dropdown items (name, surname and phone), marking the selected one.
        private static List<SelectListItem> BuildCustomerSelect(string? selectedId) =>
            AppDataStore.Customers
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = $"{c.Name} {c.Surname} ({c.Phone})",
                    Selected = c.Id == selectedId
                })
                .ToList();

        // Returns the customer's full name, or the id when the customer is not found.
        private static string CustomerName(string customerId)
        {
            var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == customerId);
            return c != null ? $"{c.Name} {c.Surname}".Trim() : customerId;
        }

        // Returns 'registration - make model' for a vehicle, or the id when it is not found.
        private static string VehicleLabel(string vehicleId)
        {
            var v = AppDataStore.Vehicles.FirstOrDefault(x => x.Id == vehicleId);
            return v != null ? $"{v.RegistrationNumber} — {v.Make} {v.Model}" : vehicleId;
        }
    }
}
