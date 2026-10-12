using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist / Manager: Generate Quotation.
    /// Includes: Labour Cost, Parts Cost, Customer-Supplied Part Difference, Total.
    /// </summary>
    public class QuotationsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var list = new List<Quotation>();
            return View(list);
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var quote = new Quotation { Id = id };
            return View(quote);
        }

        [HttpGet]
        public IActionResult Create(string jobCardId)
        {
            if (string.IsNullOrWhiteSpace(jobCardId))
                return RedirectToAction("Index", "JobCards");

            var model = new Quotation
            {
                JobCardId = jobCardId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Quotation model)
        {
            if (string.IsNullOrWhiteSpace(model.JobCardId))
            {
                ModelState.AddModelError(string.Empty, "Job card not found.");
                return View(model);
            }

            model.TotalCost = model.LabourCost + model.PartsCost - model.CustomerSuppliedDifference;
            if (model.TotalCost < 0)
                model.TotalCost = 0;

            model.Id = Guid.NewGuid().ToString();
            model.CreatedAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(model.Status))
                model.Status = "Pending";

            TempData["Success"] = "Quotation saved.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpPost]
        public IActionResult Calculate(decimal labourCost, decimal partsCost, decimal customerSuppliedDifference)
        {
            var total = labourCost + partsCost - customerSuppliedDifference;
            if (total < 0) total = 0;
            return Json(new
            {
                labourCost,
                partsCost,
                customerSuppliedDifference,
                totalCost = total
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string status)
        {
            TempData["Success"] = $"Quotation status set to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public IActionResult ByJobCard(string jobCardId)
        {
            var list = new List<Quotation>();
            ViewData["JobCardId"] = jobCardId;
            return View("Index", list);
        }
    }
}