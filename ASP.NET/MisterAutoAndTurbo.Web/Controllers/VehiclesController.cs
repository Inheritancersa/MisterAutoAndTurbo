using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist: Register / Update Vehicle. Manager: View Vehicle Records.
    /// In-memory store is refreshed from Supabase on list pages so Flutter-created rows appear.
    /// </summary>
    // Vehicle registration and records. Manager, Receptionist and Mechanic can use it.
    [Authorize(Roles = "Manager,Receptionist,Mechanic")]
    
    public class VehiclesController : Controller
    {
        // Database context used to reload vehicles that the Flutter app saved in Supabase.
        private readonly ApplicationDbContext _db;

        public VehiclesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /Vehicles: reloads data from the database, then optionally filters by search text (registration, make, model, customer id or name, year or mileage). Sorted by registration number.
        [HttpGet]
        public IActionResult Index(string? q = null)
        {
            AppDataStore.ReloadFromDatabase(_db);

            IEnumerable<Vehicle> vehicles = AppDataStore.Vehicles;

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                var customerNames = AppDataStore.Customers.ToDictionary(
                    c => c.Id,
                    c => $"{c.Name} {c.Surname}".Trim());
                vehicles = vehicles.Where(v =>
                    (v.RegistrationNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (v.Make?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (v.Model?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (v.CustomerId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (customerNames.TryGetValue(v.CustomerId, out var cn) && cn.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    v.Year.ToString().Contains(term) ||
                    v.Mileage.ToString().Contains(term));
            }

            ViewBag.Query = q;
            ViewBag.CustomerNames = AppDataStore.Customers.ToDictionary(
                c => c.Id,
                c => $"{c.Name} {c.Surname}".Trim());
            return View(vehicles.OrderBy(v => v.RegistrationNumber).ToList());
        }

        // GET: shows one vehicle with its owner (404 if not found).
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

        // GET: shows the register-vehicle form with the customer and make dropdowns. A customer can be preselected through customerId.
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
            PopulateMakes(vehicle.Make);

            return View(vehicle);
        }

        // POST: reloads data (to see duplicates created elsewhere), validates customer, registration and make, normalises the registration, blocks a registration that already exists and identical details for the same customer, then saves with a new id (veh-xxxx).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Vehicle vehicle)
        {
            // Refresh from database so we detect duplicates created elsewhere (e.g. Flutter)
            AppDataStore.ReloadFromDatabase(_db);

            ViewBag.Customers = AppDataStore.Customers
                .OrderBy(c => c.Name)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id,
                    Text = $"{c.Name} {c.Surname} ({c.Phone}) — {c.Id}"
                })
                .ToList();
            PopulateMakes(vehicle.Make);

            if (string.IsNullOrWhiteSpace(vehicle.CustomerId) ||
                string.IsNullOrWhiteSpace(vehicle.RegistrationNumber))
            {
                ModelState.AddModelError(string.Empty, "Customer and registration number are required.");
                return View(vehicle);
            }

            if (string.IsNullOrWhiteSpace(vehicle.Make))
            {
                ModelState.AddModelError(nameof(vehicle.Make), "Please select a make.");
                return View(vehicle);
            }

            vehicle.RegistrationNumber = NormalizeRegistration(vehicle.RegistrationNumber);
            vehicle.Make = vehicle.Make?.Trim() ?? string.Empty;
            vehicle.Model = vehicle.Model?.Trim() ?? string.Empty;

            // Block same registration number (ignoring spaces / case)
            var sameReg = AppDataStore.Vehicles.FirstOrDefault(v =>
                string.Equals(NormalizeRegistration(v.RegistrationNumber), vehicle.RegistrationNumber,
                    StringComparison.OrdinalIgnoreCase));
            if (sameReg != null)
            {
                ModelState.AddModelError(nameof(vehicle.RegistrationNumber),
                    $"A vehicle with registration {sameReg.RegistrationNumber} is already registered (Make: {sameReg.Make} {sameReg.Model}).");
                return View(vehicle);
            }

            // Block identical vehicle details for the same customer
            var sameDetails = AppDataStore.Vehicles.FirstOrDefault(v =>
                string.Equals(v.CustomerId, vehicle.CustomerId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((v.Make ?? "").Trim(), vehicle.Make, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((v.Model ?? "").Trim(), vehicle.Model, StringComparison.OrdinalIgnoreCase) &&
                v.Year == vehicle.Year &&
                string.Equals(NormalizeRegistration(v.RegistrationNumber), vehicle.RegistrationNumber,
                    StringComparison.OrdinalIgnoreCase));
            if (sameDetails != null)
            {
                ModelState.AddModelError(string.Empty,
                    "This vehicle is already registered with the same customer, make, model, year and registration.");
                return View(vehicle);
            }

            vehicle.Id = AppDataStore.NewId("veh");

            AppDataStore.Vehicles.Add(vehicle);

            TempData["Success"] = $"Vehicle {vehicle.RegistrationNumber} registered successfully. You can now book an appointment for it below.";
            return RedirectToAction(nameof(Details), new { id = vehicle.Id });
        }

        // GET: shows the edit form with the customer dropdown and makes.
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

            PopulateMakes(vehicle.Make);

            return View(vehicle);
        }

        // POST: same validation and duplicate checks as Create, but ignoring the vehicle being edited. Valid values are copied onto the existing vehicle.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Vehicle vehicle)
        {
            if (id != vehicle.Id)
                return NotFound();

            var existing = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == id);
            if (existing == null)
                return NotFound();

            // Refresh so duplicate checks see latest data
            AppDataStore.ReloadFromDatabase(_db);
            existing = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == id);
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
            PopulateMakes(vehicle.Make);

            if (string.IsNullOrWhiteSpace(vehicle.CustomerId) ||
                string.IsNullOrWhiteSpace(vehicle.RegistrationNumber))
            {
                ModelState.AddModelError(string.Empty, "Customer and registration number are required.");
                return View(vehicle);
            }

            if (string.IsNullOrWhiteSpace(vehicle.Make))
            {
                ModelState.AddModelError(nameof(vehicle.Make), "Please select a make.");
                return View(vehicle);
            }

            var normalizedReg = NormalizeRegistration(vehicle.RegistrationNumber);
            var make = vehicle.Make?.Trim() ?? string.Empty;
            var model = vehicle.Model?.Trim() ?? string.Empty;

            var sameReg = AppDataStore.Vehicles.FirstOrDefault(v =>
                v.Id != id &&
                string.Equals(NormalizeRegistration(v.RegistrationNumber), normalizedReg,
                    StringComparison.OrdinalIgnoreCase));
            if (sameReg != null)
            {
                ModelState.AddModelError(nameof(vehicle.RegistrationNumber),
                    $"Another vehicle already uses registration {sameReg.RegistrationNumber} ({sameReg.Make} {sameReg.Model}).");
                return View(vehicle);
            }

            var sameDetails = AppDataStore.Vehicles.FirstOrDefault(v =>
                v.Id != id &&
                string.Equals(v.CustomerId, vehicle.CustomerId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((v.Make ?? "").Trim(), make, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((v.Model ?? "").Trim(), model, StringComparison.OrdinalIgnoreCase) &&
                v.Year == vehicle.Year &&
                string.Equals(NormalizeRegistration(v.RegistrationNumber), normalizedReg,
                    StringComparison.OrdinalIgnoreCase));
            if (sameDetails != null)
            {
                ModelState.AddModelError(string.Empty,
                    "Another vehicle already has these same details (customer, make, model, year, registration).");
                return View(vehicle);
            }

            existing.CustomerId = vehicle.CustomerId;
            existing.RegistrationNumber = normalizedReg;
            existing.Make = make;
            existing.Model = model;
            existing.Year = vehicle.Year;
            existing.Mileage = vehicle.Mileage;

            TempData["Success"] = "Vehicle updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: lists the vehicles of one customer by reusing the Index view.
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

        // Returns the customer's vehicles as JSON (id and text) for dropdown filtering on other forms.
        /// <summary>JSON: vehicles belonging to one customer (used by forms that pair customer + vehicle).</summary>
        [HttpGet]
        public IActionResult JsonByCustomer(string customerId)
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

        // GET: looks a vehicle up by registration number (case-insensitive). Shows the vehicle and its owner, or an error message when not found.
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

        // How it works: finds all job cards of the vehicle and the quotations, payments, appointments, complaints, collections and service requests linked to it, removes the related notifications and payment proof files, then removes all those records and the vehicle (cascade delete done manually).
        // POST: /Vehicles/Delete/{id}
        // Removes the vehicle and everything linked to it (job cards, quotations, payments, appointments, etc.).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var vehicle = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == id);
            if (vehicle == null)
                return NotFound();

            var reg = vehicle.RegistrationNumber;

            var jobIds = AppDataStore.JobCards
                .Where(j => j.VehichleId == id)
                .Select(j => j.Id)
                .ToHashSet();

            var quotes = AppDataStore.Quotations
                .Where(q => q.VehicleId == id || jobIds.Contains(q.JobCardId))
                .ToList();
            var quoteIds = quotes.Select(q => q.Id).ToHashSet();

            var payments = AppDataStore.Payments
                .Where(p => quoteIds.Contains(p.QuotationId))
                .ToList();
            var appointments = AppDataStore.Appointments
                .Where(a => a.VehicleId == id || jobIds.Contains(a.JobCardId))
                .ToList();
            var complaints = AppDataStore.Complaints
                .Where(c => c.VehicleId == id || jobIds.Contains(c.JobCardId))
                .ToList();
            var collections = AppDataStore.VehicleCollections
                .Where(c => c.VehicleId == id || jobIds.Contains(c.JobCardId))
                .ToList();
            var requests = AppDataStore.ServiceRequests
                .Where(r => r.VehicleId == id)
                .ToList();

            // Notifications that point at any of the records being removed
            var relatedIds = new HashSet<string> { id };
            relatedIds.UnionWith(jobIds);
            relatedIds.UnionWith(quoteIds);
            relatedIds.UnionWith(payments.Select(x => x.Id));
            relatedIds.UnionWith(appointments.Select(x => x.Id));
            relatedIds.UnionWith(complaints.Select(x => x.Id));
            relatedIds.UnionWith(collections.Select(x => x.Id));
            relatedIds.UnionWith(requests.Select(x => x.Id));

            AppDataStore.Notifications.RemoveAll(n => n.RelatedId != null && relatedIds.Contains(n.RelatedId));
            foreach (var p in payments) PaymentsController.DeleteProofFile(p.ProofOfPaymentUrl);
            AppDataStore.Payments.RemoveAll(p => payments.Contains(p));
            AppDataStore.Quotations.RemoveAll(q => quoteIds.Contains(q.Id));
            AppDataStore.Appointments.RemoveAll(a => appointments.Contains(a));
            AppDataStore.Complaints.RemoveAll(c => complaints.Contains(c));
            AppDataStore.VehicleCollections.RemoveAll(c => collections.Contains(c));
            AppDataStore.ServiceRequests.RemoveAll(r => requests.Contains(r));
            AppDataStore.JobCards.RemoveAll(j => jobIds.Contains(j.Id));
            AppDataStore.Vehicles.Remove(vehicle);

            TempData["Success"] = $"Vehicle {reg} and all linked records were deleted.";
            return RedirectToAction(nameof(Index));
        }

        // Fills the make dropdown from CarMakes (keeps a legacy value selected).
        private void PopulateMakes(string? selected = null)
        {
            ViewBag.Makes = CarMakes.SelectList(selected);
        }

        /// <summary>
        /// Uppercase registration and strip spaces/dashes so "ABC 123 FS" and "ABC123FS" match.
        /// </summary>
        // Uppercases the registration and removes spaces, dashes and slashes so ABC 123 FS and ABC123FS compare as equal.
        private static string NormalizeRegistration(string? registration)
        {
            if (string.IsNullOrWhiteSpace(registration))
                return string.Empty;

            var chars = registration
                .Trim()
                .ToUpperInvariant()
                .Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '/')
                .ToArray();
            return new string(chars);
        }

    }
}
