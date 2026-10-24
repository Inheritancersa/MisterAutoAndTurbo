using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Manager: Manage Pricing (view, update effective prices for parts/services).
    /// </summary>
    public class PricingController : Controller
    {
        // GET: /Pricing
        [HttpGet]
        public IActionResult Index()
        {
            // TODO: Retrieve pricing records
            var list = new List<Pricing>();
            return View(list);
        }

        // GET: /Pricing/Create
        [HttpGet]
        public IActionResult Create(string? partId = null)
        {
            var model = new Pricing
            {
                PartId = partId ?? string.Empty,
                EffectiveFrom = DateTime.UtcNow.Date
            };
            return View(model);
        }

        // POST: /Pricing/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Pricing model)
        {
            // alt [price valid] -> Save pricing changes + Confirmation
            // else -> Display pricing error
            if (string.IsNullOrWhiteSpace(model.PartId) || model.Price < 0)
            {
                ModelState.AddModelError(string.Empty, "Valid part and price are required.");
                return View(model);
            }

            model.Id = Guid.NewGuid().ToString();
            // TODO: Save pricing to Supabase
            TempData["Success"] = "Pricing updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Pricing/Edit/{id}
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = new Pricing { Id = id };
            return View(model);
        }

        // POST: /Pricing/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Pricing model)
        {
            if (id != model.Id)
                return NotFound();

            if (model.Price < 0)
            {
                ModelState.AddModelError(string.Empty, "Price must be zero or greater.");
                return View(model);
            }

            // TODO: Save pricing changes
            TempData["Success"] = "Pricing saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
