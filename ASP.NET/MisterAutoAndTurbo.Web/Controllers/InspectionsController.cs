using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Mechanic: Record Inspection (findings + recommended action).
    /// </summary>
    public class InspectionsController : Controller
    {
        // GET: /Inspections/Create?jobCardId=
        [HttpGet]
        public IActionResult Create(string jobCardId)
        {
            if (string.IsNullOrWhiteSpace(jobCardId))
                return RedirectToAction("Index", "JobCards");

            var model = new Inspection
            {
                JobCardId = jobCardId,
                InspectedAt = DateTime.UtcNow
            };
            return View(model);
        }

        // POST: /Inspections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Inspection model)
        {
            // alt [findings entered] -> Save inspection
            // else -> Display validation error
            if (string.IsNullOrWhiteSpace(model.Findings))
            {
                ModelState.AddModelError(string.Empty, "Inspection findings are required.");
                return View(model);
            }

            // TODO: Save inspection to Supabase; set job status to Quotation if needed
            model.Id = Guid.NewGuid().ToString();
            model.InspectedAt = DateTime.UtcNow;

            TempData["Success"] = "Inspection saved.";
            return RedirectToAction("Details", "JobCards", new { id = model.JobCardId });
        }

        // GET: /Inspections/ByJobCard/{jobCardId}
        [HttpGet]
        public IActionResult ByJobCard(string jobCardId)
        {
            // TODO: Retrieve inspections for job card
            var list = new List<Inspection>();
            ViewData["JobCardId"] = jobCardId;
            return View(list);
        }
    }
}
