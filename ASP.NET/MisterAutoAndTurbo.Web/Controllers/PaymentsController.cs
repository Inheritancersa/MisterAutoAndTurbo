using MisterAutoAndTurbo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Receptionist / Manager: Record payment against a quotation.
    /// Manager: View payment records, outstanding balances.
    /// </summary>
    // Payments for Manager and Receptionist: record payments against a quotation, upload proof of payment, and view outstanding balances.
    [Authorize(Roles = "Manager,Receptionist")]
    
    public class PaymentsController : Controller
    {
        // GET /Payments: lists payments newest first, optionally filtered by customer, status and search text (id, quotation, method, status, customer, recorder, amount or customer name). Also passes the total outstanding balance and the number of unpaid quotations.
        [HttpGet]
        public IActionResult Index(string? customerId = null, string? status = null, string? q = null)
        {
            var list = AppDataStore.Payments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(customerId))
                list = list.Where(p => p.CustomerId == customerId);

            if (!string.IsNullOrWhiteSpace(status))
                list = list.Where(p => string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase));

            var customerNames = AppDataStore.Customers.ToDictionary(
                c => c.Id,
                c => $"{c.Name} {c.Surname}".Trim());

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                list = list.Where(p =>
                    (p.Id?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.QuotationId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Method?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Status?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.CustomerId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.RecordedById?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    p.Amount.ToString("N2").Contains(term) ||
                    (customerNames.TryGetValue(p.CustomerId, out var cn) && cn.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            ViewBag.CustomerFilter = customerId;
            ViewBag.StatusFilter = status;
            ViewBag.Query = q;
            ViewBag.Customers = BuildCustomerSelect(customerId);
            ViewBag.CustomerNames = customerNames;

            var outstanding = ComputeOutstanding();
            ViewBag.OutstandingTotal = outstanding.Sum(x => x.Balance);
            ViewBag.OutstandingCount = outstanding.Count(x => x.Balance > 0);

            return View(list.OrderByDescending(p => p.PaidAt).ToList());
        }

        // GET: shows quotations that still have a balance due, largest balance first.
        [HttpGet]
        public IActionResult Outstanding()
        {
            var rows = ComputeOutstanding().Where(x => x.Balance > 0).OrderByDescending(x => x.Balance).ToList();
            return View(rows);
        }

        // GET: shows a payment with its quotation total/status and every payment that has proof for the same quotation (or the same customer when there is no quotation).
        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var payment = AppDataStore.Payments.FirstOrDefault(p => p.Id == id);
            if (payment == null)
                return NotFound();

            ViewBag.CustomerName = CustomerName(payment.CustomerId);
            var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == payment.QuotationId);
            ViewBag.QuoteTotal = quote?.TotalCost;
            ViewBag.QuoteStatus = quote?.Status;

            // All payment rows with proof for this quotation (or same customer if no quote).
            // Each mobile upload creates its own Payment row — show every one.
            IEnumerable<Payment> related = AppDataStore.Payments
                .Where(p => p.HasProof && p.Status != "Refunded");
            if (!string.IsNullOrWhiteSpace(payment.QuotationId))
                related = related.Where(p => p.QuotationId == payment.QuotationId);
            else
                related = related.Where(p => p.CustomerId == payment.CustomerId);

            ViewBag.AllProofPayments = related
                .OrderByDescending(p => p.PaidAt)
                .ToList();

            return View(payment);
        }

        // GET: shows the record-payment form (default method EFT, time now). When a quotation is given, the customer and the remaining balance are prefilled.
        [HttpGet]
        public IActionResult Create(string? quotationId = null, string? customerId = null)
        {
            var model = new Payment
            {
                QuotationId = quotationId ?? string.Empty,
                CustomerId = customerId ?? string.Empty,
                PaidAt = SaTime.Now,
                Status = "Recorded",
                Method = "EFT"
            };

            if (!string.IsNullOrWhiteSpace(quotationId))
            {
                var q = AppDataStore.Quotations.FirstOrDefault(x => x.Id == quotationId);
                if (q != null)
                {
                    model.QuotationId = q.Id;
                    model.CustomerId = q.CustomerId;
                    // Suggest remaining balance
                    var paid = AppDataStore.Payments
                        .Where(p => p.QuotationId == q.Id && p.Status != "Refunded")
                        .Sum(p => p.Amount);
                    var remaining = q.TotalCost - paid;
                    if (remaining > 0)
                        model.Amount = remaining;
                }
            }

            PopulateLookups(model);
            return View(model);
        }

        // POST: requires an existing quotation and an amount above zero, takes the customer from the quotation, creates the id (pay-xxxx), converts the entered South African time to UTC, fills defaults, and saves an uploaded proof file when provided.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment model, IFormFile? proofFile)
        {
            PopulateLookups(model);

            if (string.IsNullOrWhiteSpace(model.QuotationId))
            {
                ModelState.AddModelError(nameof(model.QuotationId), "Please select a quotation.");
                return View(model);
            }

            var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == model.QuotationId);
            if (quote == null)
            {
                ModelState.AddModelError(nameof(model.QuotationId), "Quotation not found.");
                return View(model);
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Amount must be greater than zero.");
                return View(model);
            }

            model.CustomerId = quote.CustomerId;
            model.Id = AppDataStore.NewId("pay");
            model.PaidAt = model.PaidAt == default ? DateTime.UtcNow : SaTime.FromSa(model.PaidAt);
            if (string.IsNullOrWhiteSpace(model.Status))
                model.Status = "Recorded";
            if (string.IsNullOrWhiteSpace(model.Method))
                model.Method = "Cash";
            if (string.IsNullOrWhiteSpace(model.RecordedById))
                model.RecordedById = "staff";
            model.ProofOfPaymentUrl = model.ProofOfPaymentUrl?.Trim() ?? string.Empty;

            if (proofFile != null && proofFile.Length > 0)
            {
                var (url, error) = await SaveProofAsync(proofFile);
                if (error != null)
                {
                    ModelState.AddModelError("proofFile", error);
                    return View(model);
                }
                model.ProofOfPaymentUrl = url!;
            }

            AppDataStore.Payments.Add(model);

            TempData["Success"] = $"Payment of R {model.Amount:N2} recorded for {CustomerName(model.CustomerId)}.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // GET: shows the edit form for a payment.
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var payment = AppDataStore.Payments.FirstOrDefault(p => p.Id == id);
            if (payment == null)
                return NotFound();

            PopulateLookups(payment);
            return View(payment);
        }

        // POST: validates the amount and updates quotation, amount, method, status, time and recorder. A new proof file replaces the old one (the old file is deleted); a typed reference is kept or replaced only when deliberately changed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Payment model, IFormFile? proofFile)
        {
            if (id != model.Id)
                return NotFound();

            var existing = AppDataStore.Payments.FirstOrDefault(p => p.Id == id);
            if (existing == null)
                return NotFound();

            PopulateLookups(model);

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Amount must be greater than zero.");
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.QuotationId))
            {
                var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == model.QuotationId);
                if (quote != null)
                    existing.CustomerId = quote.CustomerId;
                existing.QuotationId = model.QuotationId;
            }

            existing.Amount = model.Amount;
            existing.Method = model.Method ?? string.Empty;
            existing.Status = model.Status ?? "Recorded";
            existing.PaidAt = SaTime.FromSa(model.PaidAt);
            existing.RecordedById = model.RecordedById?.Trim() ?? string.Empty;
            if (proofFile != null && proofFile.Length > 0)
            {
                var (url, error) = await SaveProofAsync(proofFile);
                if (error != null)
                {
                    ModelState.AddModelError("proofFile", error);
                    return View(model);
                }
                DeleteProofFile(existing.ProofOfPaymentUrl);   // replace the old upload
                existing.ProofOfPaymentUrl = url!;
            }
            else
            {
                var typed = model.ProofOfPaymentUrl?.Trim() ?? string.Empty;
                // Keep an uploaded file unless the reference was deliberately changed
                if (!IsUploadedProof(existing.ProofOfPaymentUrl) || !string.IsNullOrEmpty(typed) && typed != existing.ProofOfPaymentUrl)
                {
                    if (typed != existing.ProofOfPaymentUrl) DeleteProofFile(existing.ProofOfPaymentUrl);
                    existing.ProofOfPaymentUrl = typed;
                }
            }

            TempData["Success"] = "Payment updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: sets the status to Recorded, Confirmed or Refunded (refunded payments are ignored in balance calculations).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string status)
        {
            var payment = AppDataStore.Payments.FirstOrDefault(p => p.Id == id);
            if (payment == null)
                return NotFound();

            var allowed = new[] { "Recorded", "Confirmed", "Refunded" };
            if (!allowed.Contains(status))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            payment.Status = status;
            TempData["Success"] = $"Payment status set to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: deletes a payment and its uploaded proof file.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            var payment = AppDataStore.Payments.FirstOrDefault(p => p.Id == id);
            if (payment == null)
                return NotFound();

            DeleteProofFile(payment.ProofOfPaymentUrl);
            AppDataStore.Payments.Remove(payment);
            TempData["Success"] = "Payment deleted.";
            return RedirectToAction(nameof(Index));
        }

        // GET: shortcut that opens the record-payment form for a quotation.
        [HttpGet]
        public IActionResult ByQuotation(string quotationId)
        {
            return RedirectToAction(nameof(Create), new { quotationId });
        }

        // --- proof of payment uploads ---
        // Files are saved under wwwroot/uploads/proofs, limited to 5 MB and the types PDF, JPG, JPEG, PNG and WEBP.
        // --- proof of payment uploads ---

        private const string ProofFolder = "uploads/proofs";
        private const long MaxProofBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedProofExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };

        // True when the url points to a file this app uploaded (as opposed to a typed reference).
        private static bool IsUploadedProof(string? url) =>
            !string.IsNullOrEmpty(url) && url.StartsWith("/" + ProofFolder + "/", StringComparison.OrdinalIgnoreCase);

        // Validates size and extension, then saves the file under a random GUID name (so files cannot overwrite each other) and returns its url, or an error message.
        private async Task<(string? Url, string? Error)> SaveProofAsync(IFormFile file)
        {
            if (file.Length > MaxProofBytes)
                return (null, "Proof of payment must be 5 MB or smaller.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedProofExtensions.Contains(ext))
                return (null, "Proof of payment must be a PDF or an image (JPG, PNG, WEBP).");

            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var folder = Path.Combine(env.WebRootPath, "uploads", "proofs");
            Directory.CreateDirectory(folder);

            // Random file name so uploads can never overwrite each other or escape the folder
            var fileName = $"{Guid.NewGuid():N}{ext}";
            await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
            {
                await file.CopyToAsync(stream);
            }
            return ($"/{ProofFolder}/{fileName}", null);
        }

        /// <summary>Deletes an uploaded proof file from disk (ignores plain-text references).</summary>
        // Deletes an uploaded proof file from disk. Typed references are ignored and failures are swallowed (best effort).
        public static void DeleteProofFile(string? url)
        {
            if (!IsUploadedProof(url)) return;
            try
            {
                var name = Path.GetFileName(url!);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proofs", name);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { /* best effort */ }
        }

        // --- helpers ---

        // Builds one row per open quotation (Pending, Sent or Approved): total, amount paid (refunded payments excluded) and the remaining balance.
        private static List<OutstandingRow> ComputeOutstanding()
        {
            // Quotes that are Approved or Sent can have balance due
            var quotes = AppDataStore.Quotations
                .Where(q => q.Status == "Approved" || q.Status == "Sent" || q.Status == "Pending")
                .ToList();

            var rows = new List<OutstandingRow>();
            foreach (var q in quotes)
            {
                var paid = AppDataStore.Payments
                    .Where(p => p.QuotationId == q.Id && p.Status != "Refunded")
                    .Sum(p => p.Amount);
                var balance = q.TotalCost - paid;
                rows.Add(new OutstandingRow
                {
                    QuotationId = q.Id,
                    CustomerId = q.CustomerId,
                    CustomerName = CustomerName(q.CustomerId),
                    QuoteTotal = q.TotalCost,
                    Paid = paid,
                    Balance = balance,
                    QuoteStatus = q.Status
                });
            }
            return rows;
        }

        // Fills the quotation dropdown (with total and balance) and the customer dropdown.
        private void PopulateLookups(Payment model)
        {
            ViewBag.Quotations = AppDataStore.Quotations
                .OrderByDescending(q => q.CreatedAt)
                .Select(q =>
                {
                    var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == q.CustomerId);
                    var name = c != null ? $"{c.Name} {c.Surname}" : q.CustomerId;
                    var paid = AppDataStore.Payments
                        .Where(p => p.QuotationId == q.Id && p.Status != "Refunded")
                        .Sum(p => p.Amount);
                    var bal = q.TotalCost - paid;
                    return new SelectListItem
                    {
                        Value = q.Id,
                        Text = $"{q.Id} — {name} · Total R {q.TotalCost:N2} · Balance R {bal:N2} ({q.Status})",
                        Selected = q.Id == model.QuotationId
                    };
                })
                .ToList();

            ViewBag.Customers = BuildCustomerSelect(model.CustomerId);
        }

        // Builds the customer dropdown items.
        private static List<SelectListItem> BuildCustomerSelect(string? selectedId) =>
            AppDataStore.Customers
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = $"{c.Name} {c.Surname} ({c.Phone})",
                    Selected = c.Id == selectedId
                })
                .ToList();

        // Returns the customer's full name, or the id when not found.
        private static string CustomerName(string customerId)
        {
            var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == customerId);
            return c != null ? $"{c.Name} {c.Surname}".Trim() : customerId;
        }
    }

    /// <summary>
    /// One row on the outstanding balances page: quotation total, paid amount and balance.
    /// </summary>
    public class OutstandingRow
    {
        public string QuotationId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal QuoteTotal { get; set; }
        public decimal Paid { get; set; }
        public decimal Balance { get; set; }
        public string QuoteStatus { get; set; } = string.Empty;
    }
}
