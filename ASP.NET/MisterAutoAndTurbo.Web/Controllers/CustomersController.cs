using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    public class CustomersController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var customers = AppDataStore.Customers
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Surname)
                .ToList();
            return View(customers);
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var customer = AppDataStore.Customers.FirstOrDefault(c => c.Id == id);
            if (customer == null)
                return NotFound();

            ViewBag.Vehicles = AppDataStore.Vehicles
                .Where(v => v.CustomerId == customer.Id)
                .OrderBy(v => v.RegistrationNumber)
                .ToList();
            return View(customer);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Phone))
            {
                ModelState.AddModelError(string.Empty, "Name and phone are required.");
                return View(model);
            }

            model.Id = AppDataStore.NewId("cust");
            model.Name = model.Name.Trim();
            model.Surname = model.Surname?.Trim() ?? string.Empty;
            model.Phone = model.Phone.Trim();
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.Address = model.Address?.Trim() ?? string.Empty;

            AppDataStore.Customers.Add(model);

            TempData["Success"] = $"Customer {model.Name} {model.Surname} registered.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

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

        [HttpGet]
        public IActionResult Search(string q)
        {
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
