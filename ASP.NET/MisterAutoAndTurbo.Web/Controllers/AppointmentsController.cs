using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist: Book and manage workshop appointments.
    /// </summary>
    public class AppointmentsController : Controller
    {
        // Simple in-memory store so the page shows data while running (resets on app restart)
        private static readonly List<Appointment> _store = new()
        {
            new Appointment
            {
                Id = "apt-001",
                CustomerId = "cust-001",
                VehicleId = "veh-001",
                CreatedById = "staff-01",
                ScheduledAt = DateTime.Today.AddHours(9),
                Status = "Scheduled"
            },
            new Appointment
            {
                Id = "apt-002",
                CustomerId = "cust-002",
                VehicleId = "veh-002",
                CreatedById = "staff-01",
                ScheduledAt = DateTime.Today.AddHours(11),
                Status = "Arrived"
            },
            new Appointment
            {
                Id = "apt-003",
                CustomerId = "cust-003",
                VehicleId = "veh-003",
                CreatedById = "staff-01",
                ScheduledAt = DateTime.Today.AddDays(1).AddHours(10),
                Status = "Scheduled"
            }
        };

        [HttpGet]
        public IActionResult Index()
        {
            var list = _store.OrderBy(a => a.ScheduledAt).ToList();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create(string? customerId = null, string? vehicleId = null)
        {
            var model = new Appointment
            {
                CustomerId = customerId ?? string.Empty,
                VehicleId = vehicleId ?? string.Empty,
                ScheduledAt = DateTime.Now.AddDays(1).Date.AddHours(9),
                Status = "Scheduled"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Appointment model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerId) || string.IsNullOrWhiteSpace(model.VehicleId))
            {
                ModelState.AddModelError(string.Empty, "Customer and vehicle are required.");
                return View(model);
            }

            if (model.ScheduledAt == default)
            {
                ModelState.AddModelError(nameof(model.ScheduledAt), "Scheduled date and time are required.");
                return View(model);
            }

            model.Id = Guid.NewGuid().ToString("N")[..8];
            if (string.IsNullOrWhiteSpace(model.Status))
                model.Status = "Scheduled";
            if (string.IsNullOrWhiteSpace(model.CreatedById))
                model.CreatedById = "staff";

            _store.Add(model);
            TempData["Success"] = "Appointment booked successfully.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = _store.FirstOrDefault(a => a.Id == id);
            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = _store.FirstOrDefault(a => a.Id == id);
            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Appointment model)
        {
            if (id != model.Id)
                return NotFound();

            var existing = _store.FirstOrDefault(a => a.Id == id);
            if (existing == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.CustomerId) || string.IsNullOrWhiteSpace(model.VehicleId))
            {
                ModelState.AddModelError(string.Empty, "Customer and vehicle are required.");
                return View(model);
            }

            existing.CustomerId = model.CustomerId;
            existing.VehicleId = model.VehicleId;
            existing.CreatedById = model.CreatedById;
            existing.ScheduledAt = model.ScheduledAt;
            existing.Status = model.Status;

            TempData["Success"] = "Appointment updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string status)
        {
            var existing = _store.FirstOrDefault(a => a.Id == id);
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
    }
}
