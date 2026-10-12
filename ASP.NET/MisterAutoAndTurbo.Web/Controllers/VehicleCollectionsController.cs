using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist / Manager: Manage Vehicle Collection.
    /// alt [status = Ready for Collection and payment recorded] -> Update status = Collected
    /// else -> Display "vehicle not ready for collection"
    /// </summary>
    public class VehicleCollectionsController : Controller
    {
        // GET: /VehicleCollections
        [HttpGet]
        public IActionResult Index()
        {
            // TODO: List collection records
            var list = new List<VehicleCollection>();
            return View(list);
        }

        // GET: /VehicleCollections/Manage?jobCardId=
        [HttpGet]
        public IActionResult Manage(string jobCardId)
        {
            if (string.IsNullOrWhiteSpace(jobCardId))
                return RedirectToAction("Index", "JobCards");

            // TODO: Retrieve collection status / job status and payment
            var model = new VehicleCollection
            {
                JobCardId = jobCardId,
                CollectionStatus = "Pending",
                ScheduledAt = DateTime.UtcNow
            };
            return View(model);
        }

        // POST: /VehicleCollections/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string jobCardId, string collectionStatus, DateTime? collectedAt)
        {
            // TODO: Retrieve job status and payment from Supabase
            // if Ready for Collection and payment recorded -> Update job status = Collected
            // else -> Display "vehicle not ready for collection"

            if (string.IsNullOrWhiteSpace(jobCardId))
            {
                TempData["Error"] = "Job card is required.";
                return RedirectToAction(nameof(Index));
            }

            // Placeholder validation – wire real checks when Supabase is connected
            bool readyAndPaid = true; // replace with actual check

            if (!readyAndPaid && collectionStatus == "Collected")
            {
                TempData["Error"] = "Vehicle not ready for collection.";
                return RedirectToAction(nameof(Manage), new { jobCardId });
            }

            // TODO: Save VehicleCollection + update JobCard status
            TempData["Success"] = "Collection status updated.";
            return RedirectToAction("Details", "JobCards", new { id = jobCardId });
        }

        // GET: /VehicleCollections/Details/{id}
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = new VehicleCollection { Id = id };
            return View(model);
        }
    }
}
