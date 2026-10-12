using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Manager / Mechanic: Parts inventory, pricing, job parts (customer-supplied flag).
    /// </summary>
    public class PartsController : Controller
    {
        // GET: /Parts
        [HttpGet]
        public IActionResult Index()
        {
            // TODO: Retrieve parts from Supabase
            var parts = new List<Part>();
            return View(parts);
        }

        // GET: /Parts/Details/{id}
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var part = new Part { Id = id };
            return View(part);
        }

        // GET: /Parts/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Part());
        }

        // POST: /Parts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Part model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(string.Empty, "Part name is required.");
                return View(model);
            }

            model.Id = Guid.NewGuid().ToString();
            // TODO: Save part
            TempData["Success"] = "Part added.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // GET: /Parts/Edit/{id}
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var part = new Part { Id = id };
            return View(part);
        }

        // POST: /Parts/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Part model)
        {
            if (id != model.Id)
                return NotFound();

            // TODO: Update part / stock
            TempData["Success"] = "Part updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Parts/AddToJob  (JobPart: quantity, unit price, customerSupplied)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToJob(string jobCardId, string partId, int quantity, decimal unitPrice, bool customerSupplied)
        {
            if (string.IsNullOrWhiteSpace(jobCardId) || string.IsNullOrWhiteSpace(partId) || quantity <= 0)
            {
                TempData["Error"] = "Job, part and quantity are required.";
                return RedirectToAction("Details", "JobCards", new { id = jobCardId });
            }

            var jobPart = new JobPart
            {
                Id = Guid.NewGuid().ToString(),
                JobCardId = jobCardId,
                PartId = partId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                CustomerSupplied = customerSupplied
            };

            // TODO: Save JobPart; used in quotation parts cost / customer-supplied difference
            TempData["Success"] = customerSupplied
                ? "Customer-supplied part recorded on job."
                : "Part added to job.";
            return RedirectToAction("Details", "JobCards", new { id = jobCardId });
        }
    }
}
