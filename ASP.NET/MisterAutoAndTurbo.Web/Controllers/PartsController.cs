using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Manager / Mechanic: Parts inventory — list, add, edit, adjust stock, delete.
    /// </summary>
    public class PartsController : Controller
    {
        [HttpGet]
        public IActionResult Index(string? category = null, string? q = null, bool lowStockOnly = false)
        {
            var parts = AppDataStore.Parts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category))
                parts = parts.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                parts = parts.Where(p =>
                    (p.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Category?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (lowStockOnly)
                parts = parts.Where(p => p.QuantityInStock <= p.ReorderLevel);

            ViewBag.Category = category;
            ViewBag.Query = q;
            ViewBag.LowStockOnly = lowStockOnly;
            ViewBag.Categories = AppDataStore.Parts
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            ViewBag.LowStockCount = AppDataStore.Parts.Count(p => p.QuantityInStock <= p.ReorderLevel);

            return View(parts.OrderBy(p => p.Name).ToList());
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var part = AppDataStore.Parts.FirstOrDefault(p => p.Id == id);
            if (part == null)
                return NotFound();

            return View(part);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Part { ReorderLevel = 5 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Part model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Part name is required.");
                return View(model);
            }

            if (model.UnitPrice < 0)
            {
                ModelState.AddModelError(nameof(model.UnitPrice), "Unit price cannot be negative.");
                return View(model);
            }

            if (model.QuantityInStock < 0)
            {
                ModelState.AddModelError(nameof(model.QuantityInStock), "Stock cannot be negative.");
                return View(model);
            }
model.Id = AppDataStore.NewId("part");
            model.Name = model.Name.Trim();
            model.Category = model.Category?.Trim() ?? string.Empty;

            AppDataStore.Parts.Add(model);

            TempData["Success"] = $"Part “{model.Name}” added to inventory.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var part = AppDataStore.Parts.FirstOrDefault(p => p.Id == id);
            if (part == null)
                return NotFound();

            return View(part);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Part model)
        {
            if (id != model.Id)
                return NotFound();

            var existing = AppDataStore.Parts.FirstOrDefault(p => p.Id == id);
            if (existing == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Part name is required.");
                return View(model);
            }

            if (model.UnitPrice < 0 || model.QuantityInStock < 0)
            {
                ModelState.AddModelError(string.Empty, "Price and stock cannot be negative.");
                return View(model);
            }
            existing.Name = model.Name.Trim();
            existing.Category = model.Category?.Trim() ?? string.Empty;
            existing.QuantityInStock = model.QuantityInStock;
            existing.UnitPrice = model.UnitPrice;
            existing.ReorderLevel = model.ReorderLevel;

            TempData["Success"] = "Part updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Quick stock adjust: + or − quantity without full edit form.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdjustStock(string id, int delta)
        {
            var part = AppDataStore.Parts.FirstOrDefault(p => p.Id == id);
            if (part == null)
                return NotFound();

            var newQty = part.QuantityInStock + delta;
            if (newQty < 0)
            {
                TempData["Error"] = "Stock cannot go below zero.";
                return RedirectToAction(nameof(Details), new { id });
            }

            part.QuantityInStock = newQty;
            TempData["Success"] = delta >= 0
                ? $"Added {delta} — stock is now {part.QuantityInStock}."
                : $"Removed {-delta} — stock is now {part.QuantityInStock}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            var part = AppDataStore.Parts.FirstOrDefault(p => p.Id == id);
            if (part == null)
                return NotFound();

            AppDataStore.Parts.Remove(part);
            TempData["Success"] = $"Part “{part.Name}” removed from inventory.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToJob(string jobCardId, string partId, int quantity, decimal unitPrice, bool customerSupplied)
        {
            if (string.IsNullOrWhiteSpace(jobCardId) || string.IsNullOrWhiteSpace(partId) || quantity <= 0)
            {
                TempData["Error"] = "Job, part and quantity are required.";
                return RedirectToAction("Details", "JobCards", new { id = jobCardId });
            }

            var part = AppDataStore.Parts.FirstOrDefault(p => p.Id == partId);
            if (part == null)
            {
                TempData["Error"] = "Part not found.";
                return RedirectToAction("Details", "JobCards", new { id = jobCardId });
            }

            if (!customerSupplied)
            {
                if (part.QuantityInStock < quantity)
                {
                    TempData["Error"] = $"Not enough stock for {part.Name} (have {part.QuantityInStock}).";
                    return RedirectToAction("Details", "JobCards", new { id = jobCardId });
                }
                part.QuantityInStock -= quantity;
            }

            // JobPart not stored in a list yet — message only for demo
            TempData["Success"] = customerSupplied
                ? $"Customer-supplied {part.Name} × {quantity} recorded on job."
                : $"{part.Name} × {quantity} added to job (stock now {part.QuantityInStock}).";
            return RedirectToAction("Details", "JobCards", new { id = jobCardId });
        }
    }
}
