using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist: Register / Update Vehicle. Manager: View Vehicle Records.
    /// Saves to in-memory AppDataStore (visible on list until app restart).
    /// </summary>
    public class VehiclesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var vehicles = AppDataStore.Vehicles
                .OrderBy(v => v.RegistrationNumber)
                .ToList();
            return View(vehicles);
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var vehicle = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == id);
            if (vehicle == null)
                return NotFound();

            ViewBag.Customer = AppDataStore.Customers.FirstOrDefault(c => c.Id == vehicle.CustomerId);
            return View(vehicle);
        }

        [HttpGet]
        public IActionResult Create(string? customerId = null)
        {
            var vehicle = new Vehicle();
            if (!string.IsNullOrWhiteSpace(customerId))
                vehicle.CustomerId = customerId;

            ViewBag.Customers = AppDataStore.Customers
                .OrderBy(c => c.Name)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id,
                    Text = $"{c.Name} {c.Surname} ({c.Phone}) — {c.Id}"
                })
                .ToList();

            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Vehicle vehicle)
        {
            ViewBag.Customers = AppDataStore.Customers
                .OrderBy(c => c.Name)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id,
                    Text = $"{c.Name} {c.Surname} ({c.Phone}) — {c.Id}"
                })
                .ToList();

            if (string.IsNullOrWhiteSpace(vehicle.CustomerId) ||
                string.IsNullOrWhiteSpace(vehicle.RegistrationNumber))
            {
                ModelState.AddModelError(string.Empty, "Customer and registration number are required.");
                return View(vehicle);
            }

            // Prevent duplicate registration numbers
            var exists = AppDataStore.Vehicles.Any(v =>
                string.Equals(v.RegistrationNumber.Trim(), vehicle.RegistrationNumber.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                ModelState.AddModelError(nameof(vehicle.RegistrationNumber),
                    "A vehicle with this registration number is already registered.");
                return View(vehicle);
            }

            vehicle.Id = AppDataStore.NewId("veh");
            vehicle.RegistrationNumber = vehicle.RegistrationNumber.Trim().ToUpperInvariant();
            vehicle.Make = vehicle.Make?.Trim() ?? string.Empty;
            vehicle.Model = vehicle.Model?.Trim() ?? string.Empty;

            AppDataStore.Vehicles.Add(vehicle);

            TempData["Success"] = $"Vehicle {vehicle.RegistrationNumber} registered successfully.";
            return RedirectToAction(nameof(Details), new { id = vehicle.Id });
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var vehicle = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == id);
            if (vehicle == null)
                return NotFound();

            ViewBag.Customers = AppDataStore.Customers
                .OrderBy(c => c.Name)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id,
                    Text = $"{c.Name} {c.Surname} ({c.Phone}) — {c.Id}",
                    Selected = c.Id == vehicle.CustomerId
                })
                .ToList();

            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Vehicle vehicle)
        {
            if (id != vehicle.Id)
                return NotFound();

            var existing = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == id);
            if (existing == null)
                return NotFound();

            ViewBag.Customers = AppDataStore.Customers
                .OrderBy(c => c.Name)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id,
                    Text = $"{c.Name} {c.Surname} ({c.Phone}) — {c.Id}"
                })
                .ToList();

            if (string.IsNullOrWhiteSpace(vehicle.CustomerId) ||
                string.IsNullOrWhiteSpace(vehicle.RegistrationNumber))
            {
                ModelState.AddModelError(string.Empty, "Customer and registration number are required.");
                return View(vehicle);
            }

            var duplicate = AppDataStore.Vehicles.Any(v =>
                v.Id != id &&
                string.Equals(v.RegistrationNumber.Trim(), vehicle.RegistrationNumber.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                ModelState.AddModelError(nameof(vehicle.RegistrationNumber),
                    "Another vehicle already uses this registration number.");
                return View(vehicle);
            }

            existing.CustomerId = vehicle.CustomerId;
            existing.RegistrationNumber = vehicle.RegistrationNumber.Trim().ToUpperInvariant();
            existing.Make = vehicle.Make?.Trim() ?? string.Empty;
            existing.Model = vehicle.Model?.Trim() ?? string.Empty;
            existing.Year = vehicle.Year;
            existing.Mileage = vehicle.Mileage;

            TempData["Success"] = "Vehicle updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public IActionResult ByCustomer(string customerId)
        {
            var vehicles = AppDataStore.Vehicles
                .Where(v => v.CustomerId == customerId)
                .OrderBy(v => v.RegistrationNumber)
                .ToList();
            ViewData["CustomerId"] = customerId;
            return View("Index", vehicles);
        }

        [HttpGet]
        public IActionResult Lookup(string registration)
        {
            ViewData["Registration"] = registration;
            if (!string.IsNullOrWhiteSpace(registration))
            {
                var found = AppDataStore.Vehicles.FirstOrDefault(v =>
                    string.Equals(v.RegistrationNumber.Trim(), registration.Trim(),
                        StringComparison.OrdinalIgnoreCase));
                if (found != null)
                {
                    ViewBag.Vehicle = found;
                    ViewBag.Customer = AppDataStore.Customers.FirstOrDefault(c => c.Id == found.CustomerId);
                    return View();
                }

                TempData["Error"] = $"No vehicle found with registration “{registration}”.";
            }
            return View();
        }
    }
}
