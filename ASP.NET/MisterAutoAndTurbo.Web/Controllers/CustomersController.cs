using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist: Register Customer. Manager: View Customer Records.
    /// </summary>
    public class CustomersController : Controller
    {
        // GET: /Customers
        [HttpGet]
        public IActionResult Index()
        {
            // TODO: Retrieve customer records from Supabase
            // Manager: View Customer Records | Receptionist: list for intake
            var customers = new List<Customer>();
            return View(customers);
        }

        // GET: /Customers/Details/{id}
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            // TODO: Retrieve customer by id
            var customer = new Customer { Id = id };
            return View(customer);
        }

        // GET: /Customers/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        // POST: /Customers/Create  (Register Customer – Receptionist sequence)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Customer model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Phone))
            {
                ModelState.AddModelError(string.Empty, "Name and phone are required.");
                return View(model);
            }

            // TODO: Save customer details to Supabase
            // alt [details valid] -> Customer record saved + Confirmation
            // else -> Display validation error

            model.Id = Guid.NewGuid().ToString();
            TempData["Success"] = "Customer registered successfully.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // GET: /Customers/Edit/{id}
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            // TODO: Load customer
            var customer = new Customer { Id = id };
            return View(customer);
        }

        // POST: /Customers/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Customer model)
        {
            if (id != model.Id)
                return NotFound();

            // TODO: Update customer in Supabase
            TempData["Success"] = "Customer updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Customers/Search?q=
        [HttpGet]
        public IActionResult Search(string q)
        {
            // TODO: Search customers by name, phone, email
            var results = new List<Customer>();
            ViewData["Query"] = q;
            return View("Index", results);
        }
    }
}
