using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;
using MisterAutoAndTurbo.Web.Services;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Mechanic / Manager: record inspection findings and recommended action on a job card.
    /// Saving moves the job from Inspection → Quotation when appropriate.
    /// </summary>
    [Authorize(Roles = "Manager,Mechanic")]
    public class InspectionsController : Controller
    {
        // GET: shows the inspection form for a job card, prefilled with the current user as the mechanic and the current time.
        // GET: /Inspections/Create?jobCardId=
        [HttpGet]
        public IActionResult Create(string jobCardId)
        {
            if (string.IsNullOrWhiteSpace(jobCardId))
                return RedirectToAction("Index", "JobCards");

            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == jobCardId);
            if (job == null)
            {
                TempData["Error"] = "Job card not found.";
                return RedirectToAction("Index", "JobCards");
            }

            var mechanicId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.Identity?.Name
                             ?? string.Empty;

            var model = new Inspection
            {
                JobCardId = jobCardId,
                MechanicId = mechanicId,
                InspectedAt = SaTime.Now
            };

            ViewBag.JobStatus = job.Status;
            return View(model);
        }

        // POST: validates, then saves the inspection with a new id, UTC time and trimmed text. If the job was still in the Inspection status it moves to Quotation. Redirects to the job card details.
        // POST: /Inspections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Inspection model)
        {
            if (string.IsNullOrWhiteSpace(model.JobCardId))
            {
                TempData["Error"] = "Job card is required.";
                return RedirectToAction("Index", "JobCards");
            }

            var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == model.JobCardId);
            if (job == null)
            {
                TempData["Error"] = "Job card not found.";
                return RedirectToAction("Index", "JobCards");
            }

            if (string.IsNullOrWhiteSpace(model.Findings))
            {
                ModelState.AddModelError(nameof(model.Findings), "Inspection findings are required.");
                ViewBag.JobStatus = job.Status;
                return View(model);
            }

            model.Id = AppDataStore.NewId("insp");
            model.InspectedAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(model.MechanicId))
            {
                model.MechanicId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                                   ?? User.Identity?.Name
                                   ?? string.Empty;
            }
            model.Findings = model.Findings.Trim();
            model.RecommendedAction = model.RecommendedAction?.Trim() ?? string.Empty;

            AppDataStore.Inspections.Add(model);

            // After inspection, job is ready for quotation (unless already further along).
            if (job.Status == JobStatus.Inspection || string.IsNullOrWhiteSpace(job.Status))
            {
                job.Status = JobStatus.Quotation;
            }

            TempData["Success"] = "Inspection saved. Job is ready for quotation.";
            return RedirectToAction("Details", "JobCards", new { id = model.JobCardId });
        }

        // GET: lists all inspections for a job card, newest first.
        // GET: /Inspections/ByJobCard/{jobCardId}
        [HttpGet]
        public IActionResult ByJobCard(string jobCardId)
        {
            if (string.IsNullOrWhiteSpace(jobCardId))
                return RedirectToAction("Index", "JobCards");

            var list = AppDataStore.Inspections
                .Where(i => i.JobCardId == jobCardId)
                .OrderByDescending(i => i.InspectedAt)
                .ToList();

            ViewData["JobCardId"] = jobCardId;
            return View(list);
        }
    }
}
