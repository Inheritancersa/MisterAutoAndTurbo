using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist: Record Customer Complaints.
    /// Manager: View Customer Complaints.
    /// </summary>
    public class ComplaintsController : Controller
    {
        // GET: /Complaints
        [HttpGet]
        public IActionResult Index()
        {
            // TODO: Retrieve complaint records
            var list = new List<Complaint>();
            return View(list);
        }

        // GET: /Complaints/Create
        [HttpGet]
        public IActionResult Create(string? customerId = null, string? jobCardId = null)
        {
            var model = new Complaint
            {
                CustomerId = customerId ?? string.Empty,
                JobCardId = jobCardId ?? string.Empty,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };
            return View(model);
        }

        // POST: /Complaints/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Complaint model)
        {
            // alt [complaint details valid] -> Save complaint + Confirmation
            // else -> Display validation error
            if (string.IsNullOrWhiteSpace(model.CustomerId) || string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(string.Empty, "Customer and description are required.");
                return View(model);
            }

            model.Id = Guid.NewGuid().ToString();
            model.CreatedAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(model.Status))
                model.Status = "Open";

            // TODO: Save complaint to Supabase
            TempData["Success"] = "Complaint recorded.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Complaints/Details/{id}
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var model = new Complaint { Id = id };
            return View(model);
        }

        // POST: /Complaints/UpdateStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string status)
        {
            // TODO: Update complaint status
            TempData["Success"] = $"Complaint status set to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
