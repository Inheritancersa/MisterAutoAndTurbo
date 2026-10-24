using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    // Customer management (list, details, register, edit, delete, search). Only Manager and Receptionist can use it.
    [Authorize(Roles = "Manager,Receptionist")]
    
    public class CustomersController : Controller
    {
        // Database context, used to reload customers that the Flutter app saved in Supabase.
        private readonly ApplicationDbContext _db;

        public CustomersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /Customers: reloads data from the database (so app registrations show up) and lists customers sorted by name and surname.
        [HttpGet]
        public IActionResult Index()
        {
            // Pick up customers registered from Flutter / Supabase since last load.
            AppDataStore.ReloadFromDatabase(_db);

            var customers = AppDataStore.Customers
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Surname)
                .ToList();
            return View(customers);
        }

        // GET /Customers/Details/{id}: shows one customer and their vehicles. Returns 404 if the id is missing or unknown.
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            AppDataStore.MergeExternalChanges(_db);

            var customer = AppDataStore.Customers.FirstOrDefault(c => c.Id == id);
            if (customer == null)
                return NotFound();

            ViewBag.Vehicles = AppDataStore.Vehicles
                .Where(v => v.CustomerId == customer.Id)
                .OrderBy(v => v.RegistrationNumber)
                .ToList();
            return View(customer);
        }

        // GET: shows an empty customer registration form.
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        // POST: validates name and phone, trims the text, gives the customer a new id (cust-xxxx), adds it to the store and opens the details page.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Phone))
            {
                ModelState.AddModelError(string.Empty, "Name and phone are required.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Id = AppDataStore.NewId("cust");
            model.Name = model.Name.Trim();
            model.Surname = model.Surname?.Trim() ?? string.Empty;
            model.Phone = model.Phone.Trim();
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.Address = model.Address?.Trim() ?? string.Empty;
            model.WorkPhone = model.WorkPhone?.Trim();

            AppDataStore.Customers.Add(model);

            TempData["Success"] = $"Customer {model.Name} {model.Surname} registered.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // GET: shows the edit form for an existing customer (404 if not found).
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var customer = AppDataStore.Customers.FirstOrDefault(c => c.Id == id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: checks that the route id matches the form, validates name and phone, then copies the trimmed values onto the existing customer.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Customer model)
        {
            if (id != model.Id)
                return NotFound();

            var existing = AppDataStore.Customers.FirstOrDefault(c => c.Id == id);
            if (existing == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Phone))
            {
                ModelState.AddModelError(string.Empty, "Name and phone are required.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            existing.Name = model.Name.Trim();
            existing.Surname = model.Surname?.Trim() ?? string.Empty;
            existing.Phone = model.Phone.Trim();
            existing.Email = model.Email?.Trim() ?? string.Empty;
            existing.Address = model.Address?.Trim() ?? string.Empty;
            existing.WorkPhone = model.WorkPhone?.Trim() ?? string.Empty;
            existing.WorkAddress = model.WorkAddress?.Trim() ?? string.Empty;

            TempData["Success"] = "Customer updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // How it works: collects all vehicles, jobs, quotations, payments, appointments, complaints, collections and service requests that belong to the customer, removes the related notifications and uploaded proof files, then removes all those records and finally the customer (a cascade delete done manually).
        // POST: /Customers/Delete/{id}
        // Removes the customer and everything linked to them (vehicles, jobs, quotations, etc.).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var customer = AppDataStore.Customers.FirstOrDefault(c => c.Id == id);
            if (customer == null)
                return NotFound();

            var name = $"{customer.Name} {customer.Surname}".Trim();

            // Ids of the customer's vehicles and of every job card for the customer or those vehicles.
            var vehicleIds = AppDataStore.Vehicles.Where(v => v.CustomerId == id).Select(v => v.Id).ToHashSet();
            var jobIds = AppDataStore.JobCards
                .Where(j => j.CustomerId == id || vehicleIds.Contains(j.VehichleId))
                .Select(j => j.Id).ToHashSet();

            // Records linked to the customer through the customer id, their vehicles or their jobs.
            var quotes = AppDataStore.Quotations
                .Where(q => q.CustomerId == id || vehicleIds.Contains(q.VehicleId) || jobIds.Contains(q.JobCardId))
                .ToList();
            var quoteIds = quotes.Select(q => q.Id).ToHashSet();

            var payments = AppDataStore.Payments
                .Where(p => p.CustomerId == id || quoteIds.Contains(p.QuotationId)).ToList();
            var appointments = AppDataStore.Appointments
                .Where(a => a.CustomerId == id || vehicleIds.Contains(a.VehicleId) || jobIds.Contains(a.JobCardId)).ToList();
            var complaints = AppDataStore.Complaints
                .Where(c => c.CustomerId == id || vehicleIds.Contains(c.VehicleId) || jobIds.Contains(c.JobCardId)).ToList();
            var collections = AppDataStore.VehicleCollections
                .Where(c => c.CustomerId == id || vehicleIds.Contains(c.VehicleId) || jobIds.Contains(c.JobCardId)).ToList();
            var requests = AppDataStore.ServiceRequests
                .Where(r => r.CustomerId == id || vehicleIds.Contains(r.VehicleId)).ToList();

            // Notifications that point at any of the records being removed
            var relatedIds = new HashSet<string> { id };
            relatedIds.UnionWith(vehicleIds);
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
            AppDataStore.Vehicles.RemoveAll(v => vehicleIds.Contains(v.Id));
            AppDataStore.Customers.Remove(customer);

            TempData["Success"] = $"Customer {name} and all linked vehicles and records were deleted.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Customers/Search?q=: reloads data and filters by name, surname, phone or e-mail (case-insensitive). An empty search shows all customers. Reuses the Index view.
        [HttpGet]
        public IActionResult Search(string q)
        {
            AppDataStore.ReloadFromDatabase(_db);

            ViewData["Query"] = q;
            if (string.IsNullOrWhiteSpace(q))
                return View("Index", AppDataStore.Customers.OrderBy(c => c.Name).ToList());

            var term = q.Trim();
            var results = AppDataStore.Customers.Where(c =>
                    (c.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Surname?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderBy(c => c.Name)
                .ToList();
            return View("Index", results);
        }
    }
}
