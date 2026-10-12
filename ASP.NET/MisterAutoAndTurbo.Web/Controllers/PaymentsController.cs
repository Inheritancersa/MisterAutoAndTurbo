using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist / Manager: Record Payment. Manager: View Payment Records.
    /// Only management should process payments per design alignment (Receptionist may record under policy).
    /// </summary>
    public class PaymentsController : Controller
    {
        // GET: /Payments
        [HttpGet]
        public IActionResult Index()
        {
            // Manager: View Payment Records
            // TODO: Retrieve payment records from Supabase
            var payments = new List<Payment>();
            return View(payments);
        }

        // GET: /Payments/Details/{id}
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var payment = new Payment { Id = id };
            return View(payment);
        }

        // GET: /Payments/Create?quotationId=
        [HttpGet]
        public IActionResult Create(string? quotationId = null)
        {
            var model = new Payment
            {
                QuotationId = quotationId ?? string.Empty,
                PaidAt = DateTime.UtcNow,
                Status = "Recorded"
            };
            return View(model);
        }

        // POST: /Payments/Create  (Record Payment sequence)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Payment model)
        {
            // alt [payment valid] -> Save payment record + Confirmation
            // else -> Display payment error
            if (string.IsNullOrWhiteSpace(model.QuotationId) || model.Amount <= 0)
            {
                ModelState.AddModelError(string.Empty, "Valid quotation and amount are required.");
                return View(model);
            }

            // TODO: Save payment to Supabase (proof of payment URL from storage if uploaded)
            model.Id = Guid.NewGuid().ToString();
            model.PaidAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(model.Status))
                model.Status = "Recorded";

            TempData["Success"] = "Payment recorded.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // GET: /Payments/ByQuotation/{quotationId}
        [HttpGet]
        public IActionResult ByQuotation(string quotationId)
        {
            var list = new List<Payment>();
            ViewData["QuotationId"] = quotationId;
            return View("Index", list);
        }
    }
}
