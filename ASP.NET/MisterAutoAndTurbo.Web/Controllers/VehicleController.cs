using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist: Register / Update Vehicle. Manager: View Vehicle Records.
    /// </summary>
    public class VehiclesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var vehicles = new List<Vehicle>();
            return View(vehicles);
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var vehicle = new Vehicle { Id = id };
            return View(vehicle);
        }

        [HttpGet]
        public IActionResult Create(string? customerId = null)
        {
            var model = new Vehicle();
            if (!string.IsNullOrWhiteSpace(customerId))
                model.CustomerId = customerId;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Vehicle model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerId) ||
                string.IsNullOrWhiteSpace(model.RegistrationNumber))
            {
                ModelState.AddModelError(string.Empty, "Customer and registration number are required.");
                return View(model);
            }

            model.Id = Guid.NewGuid().ToString();
            TempData["Success"] = "Vehicle registered successfully.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var vehicle = new Vehicle { Id = id };
            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Vehicle model)
        {
            if (id != model.Id)
                return NotFound();

            TempData["Success"] = "Vehicle updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public IActionResult ByCustomer(string customerId)
        {
            var vehicles = new List<Vehicle>();
            ViewData["CustomerId"] = customerId;
            return View("Index", vehicles);
        }

        [HttpGet]
        public IActionResult Lookup(string registration)
        {
            ViewData["Registration"] = registration;
            return View();
        }
    }
}