using MisterAutoAndTurbo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist: Book / manage appointments, send date to customer (mobile notification).
    /// </summary>
    // Appointment booking and management. Manager, Receptionist and Mechanic can use it.
    [Authorize(Roles = "Manager,Receptionist,Mechanic")]
    
    public class AppointmentsController : Controller
    {
        // GET /Appointments: lists appointments by scheduled time. Older mobile bookings that miss a service type or description are filled in from the matching service request. Optional search q matches id, status, service, description, job, customer or vehicle.
        [HttpGet]
        public IActionResult Index(string? q = null)
        {
            IEnumerable<Appointment> list = AppDataStore.Appointments;

            // Back-fill service/description from ServiceRequests for older mobile bookings
            foreach (var a in list)
            {
                if (!string.IsNullOrWhiteSpace(a.ServiceType) && !string.IsNullOrWhiteSpace(a.Description))
                    continue;
                var sr = AppDataStore.ServiceRequests
                    .Where(r => r.CustomerId == a.CustomerId && r.VehicleId == a.VehicleId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault();
                if (sr == null) continue;
                if (string.IsNullOrWhiteSpace(a.ServiceType))
                    a.ServiceType = sr.RequestedService ?? string.Empty;
                if (string.IsNullOrWhiteSpace(a.Description))
                    a.Description = sr.Description ?? string.Empty;
            }

            var customerNames = AppDataStore.Customers.ToDictionary(
                c => c.Id,
                c => $"{c.Name} {c.Surname}".Trim());
            var vehicleLabels = AppDataStore.Vehicles.ToDictionary(
                v => v.Id,
                v => v.RegistrationNumber);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                list = list.Where(a =>
                    (a.Id?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.Status?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.ServiceType?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.JobCardId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.CustomerId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.VehicleId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (customerNames.TryGetValue(a.CustomerId, out var cn) && cn.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (vehicleLabels.TryGetValue(a.VehicleId, out var vl) && vl.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            ViewBag.Query = q;
            ViewBag.CustomerNames = customerNames;
            ViewBag.VehicleLabels = vehicleLabels;
            return View(list.OrderBy(a => a.ScheduledAt).ToList());
        }

        // GET: shows the booking form with a default time of tomorrow at 09:00. When opened from a job card, job, customer and vehicle are prefilled.
        [HttpGet]
        public IActionResult Create(string? customerId = null, string? vehicleId = null, string? jobCardId = null)
        {
            var model = new Appointment
            {
                CustomerId = customerId ?? string.Empty,
                VehicleId = vehicleId ?? string.Empty,
                JobCardId = jobCardId ?? string.Empty,
                ScheduledAt = SaTime.Now.AddDays(1).Date.AddHours(9),
                Status = "Scheduled"
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

        // POST: validates customer, time, that the vehicle belongs to the customer and that the job card exists. If no vehicle is chosen, the customer's first vehicle is used. Saves with a new id (apt-xxxx) and status Scheduled.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Appointment model)
        {
            PopulateLookups(model);

            if (string.IsNullOrWhiteSpace(model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Please select a customer.");
                return View(model);
            }

            if (!AppDataStore.Customers.Any(c => c.Id == model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Selected customer was not found.");
                return View(model);
            }

            if (model.ScheduledAt == default)
            {
                ModelState.AddModelError(nameof(model.ScheduledAt), "Scheduled date and time are required.");
                return View(model);
            }

            // Optional vehicle must belong to customer if set
            if (!string.IsNullOrWhiteSpace(model.VehicleId))
            {
                var veh = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == model.VehicleId);
                if (veh == null)
                {
                    ModelState.AddModelError(nameof(model.VehicleId), "Selected vehicle was not found.");
                    return View(model);
                }
                if (veh.CustomerId != model.CustomerId)
                {
                    ModelState.AddModelError(nameof(model.VehicleId), "That vehicle does not belong to the selected customer.");
                    return View(model);
                }
            }
            else
            {
                // Default to first vehicle of customer if any
                var first = AppDataStore.Vehicles.FirstOrDefault(v => v.CustomerId == model.CustomerId);
                if (first != null)
                    model.VehicleId = first.Id;
            }

            // Optional job card validation
            if (!string.IsNullOrWhiteSpace(model.JobCardId))
            {
                var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == model.JobCardId.Trim());
                if (job == null)
                {
                    ModelState.AddModelError(nameof(model.JobCardId), "Job ID not found. Leave blank or enter a valid job card id.");
                    return View(model);
                }
                model.JobCardId = job.Id;
            }

            model.Id = AppDataStore.NewId("apt");
            if (string.IsNullOrWhiteSpace(model.Status))
                model.Status = "Scheduled";
            if (string.IsNullOrWhiteSpace(model.CreatedById))
                model.CreatedById = "staff";
            model.CreatedById = model.CreatedById.Trim();
            model.JobCardId = model.JobCardId?.Trim() ?? string.Empty;

            AppDataStore.Appointments.Add(model);

            TempData["Success"] = "Appointment booked. You can send the date to the customer next.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // GET: shows appointment details with the customer name, phone and vehicle. Missing service type or description is taken from the latest service request of that customer and vehicle.
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = AppDataStore.Appointments.FirstOrDefault(a => a.Id == id);
            if (model == null)
                return NotFound();

            // Enrich from ServiceRequest when Flutter only stored service there.
            if (string.IsNullOrWhiteSpace(model.ServiceType) || string.IsNullOrWhiteSpace(model.Description))
            {
                var sr = AppDataStore.ServiceRequests
                    .Where(r => r.CustomerId == model.CustomerId && r.VehicleId == model.VehicleId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault();
                if (sr != null)
                {
                    if (string.IsNullOrWhiteSpace(model.ServiceType))
                        model.ServiceType = sr.RequestedService ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(model.Description))
                        model.Description = sr.Description ?? string.Empty;
                }
            }

            var cust = AppDataStore.Customers.FirstOrDefault(c => c.Id == model.CustomerId);
            var veh = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == model.VehicleId);
            ViewBag.CustomerName = cust != null ? $"{cust.Name} {cust.Surname}".Trim() : model.CustomerId;
            ViewBag.CustomerPhone = cust?.Phone ?? "";
            ViewBag.VehicleLabel = veh != null ? $"{veh.RegistrationNumber} — {veh.Make} {veh.Model}" : model.VehicleId;
            return View(model);
        }

        // GET: shows the edit form (404 if not found).
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = AppDataStore.Appointments.FirstOrDefault(a => a.Id == id);
            if (model == null)
                return NotFound();

            PopulateLookups(model);
            return View(model);
        }

        // POST: checks the id and customer, then copies the customer, vehicle, job, creator, time and status onto the existing appointment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Appointment model)
        {
            if (id != model.Id)
                return NotFound();

            var existing = AppDataStore.Appointments.FirstOrDefault(a => a.Id == id);
            if (existing == null)
                return NotFound();

            PopulateLookups(model);

            if (string.IsNullOrWhiteSpace(model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Please select a customer.");
                return View(model);
            }

            existing.CustomerId = model.CustomerId;
            existing.VehicleId = model.VehicleId ?? string.Empty;
            existing.JobCardId = model.JobCardId?.Trim() ?? string.Empty;
            existing.CreatedById = model.CreatedById?.Trim() ?? string.Empty;
            existing.ScheduledAt = model.ScheduledAt;
            existing.Status = model.Status;

            TempData["Success"] = "Appointment updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: changes the status. Only Scheduled, Arrived, Cancelled or Completed are accepted.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string status)
        {
            var existing = AppDataStore.Appointments.FirstOrDefault(a => a.Id == id);
            if (existing == null)
                return NotFound();

            var allowed = new[] { "Scheduled", "Arrived", "Cancelled", "Completed" };
            if (!allowed.Contains(status))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            existing.Status = status;
            TempData["Success"] = $"Appointment status set to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // How it works: builds a message with the customer name, date and time, vehicle registration and job id, adds it as a notification for the customer (shown in the mobile app) and records the time it was sent.
        /// <summary>
        /// Send appointment date/time to the customer as a mobile notification.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendToCustomer(string id)
        {
            var apt = AppDataStore.Appointments.FirstOrDefault(a => a.Id == id);
            if (apt == null)
                return NotFound();

            var customer = AppDataStore.Customers.FirstOrDefault(c => c.Id == apt.CustomerId);
            var vehicle = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == apt.VehicleId);
            var name = customer != null ? $"{customer.Name} {customer.Surname}".Trim() : "Customer";
            var reg = vehicle?.RegistrationNumber ?? "your vehicle";
            var when = apt.ScheduledAt.ToString("dddd, dd MMMM yyyy 'at' HH:mm");

            var message = $"Hi {name}, your appointment at Mister Auto & Turbo is scheduled for {when}" +
                          (string.IsNullOrEmpty(reg) || reg == "your vehicle" ? "." : $" for {reg}.") +
                          (string.IsNullOrWhiteSpace(apt.JobCardId) ? "" : $" Job: {apt.JobCardId}.");

            AppDataStore.Notifications.Add(new Notification
            {
                Id = AppDataStore.NewId("ntf"),
                UserAccountId = apt.CustomerId,
                Type = "Appointment",
                RelatedId = apt.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Message = message
            });

            apt.SentAt = DateTime.UtcNow;

            TempData["Success"] = $"Appointment date sent to {name}. They will see a notification on the mobile app.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: deletes an appointment and the notifications related to it.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            var apt = AppDataStore.Appointments.FirstOrDefault(a => a.Id == id);
            if (apt == null)
                return NotFound();

            AppDataStore.Appointments.Remove(apt);
            foreach (var n in AppDataStore.Notifications.Where(n => n.RelatedId == id).ToList())
                AppDataStore.Notifications.Remove(n);

            TempData["Success"] = "Appointment deleted.";
            return RedirectToAction(nameof(Index));
        }


        // Returns the customer's vehicles as JSON (id and text). The booking page calls it to refill the vehicle dropdown when the customer changes.
        /// <summary>JSON: vehicles for one customer (used by Create/Edit dropdown filter).</summary>
        [HttpGet]
        public IActionResult VehiclesByCustomer(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return Json(Array.Empty<object>());

            var list = AppDataStore.Vehicles
                .Where(v => v.CustomerId == customerId)
                .OrderBy(v => v.RegistrationNumber)
                .Select(v => new
                {
                    id = v.Id,
                    text = $"{v.RegistrationNumber} — {v.Make} {v.Model}"
                })
                .ToList();

            return Json(list);
        }

        // Fills the customer dropdown, plus only the vehicles and job cards that belong to the selected customer.
        private void PopulateLookups(Appointment model)
        {
            ViewBag.Customers = AppDataStore.Customers
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Surname)
                .Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = $"{c.Name} {c.Surname} — {c.Phone} ({c.Id})",
                    Selected = c.Id == model.CustomerId
                })
                .ToList();

            // Only vehicles that belong to the selected customer (never the full fleet).
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

            // Only job cards for the selected customer.
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
                            Text = $"{j.Id} — {(v != null ? v.RegistrationNumber : j.VehichleId)} ({j.Status})",
                            Selected = j.Id == model.JobCardId
                        };
                    })
                    .ToList();
        }
    }
}
