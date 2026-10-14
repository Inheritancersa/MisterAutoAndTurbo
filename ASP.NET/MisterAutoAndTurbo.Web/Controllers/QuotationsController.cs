using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Staff: create quotations, send to customer (notification).
    /// Customer (mobile): view quote, Approve or Reject.
    /// </summary>
    public class QuotationsController : Controller
    {
        [HttpGet]
        public IActionResult Index(string? customerId = null)
        {
            var quotes = AppDataStore.Quotations.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(customerId))
                quotes = quotes.Where(q => q.CustomerId == customerId);

            var list = quotes.OrderByDescending(q => q.CreatedAt).ToList();

            var groups = list
                .GroupBy(q => q.CustomerId)
                .Select(g =>
                {
                    var cust = AppDataStore.Customers.FirstOrDefault(c => c.Id == g.Key);
                    var name = cust != null
                        ? $"{cust.Name} {cust.Surname}".Trim()
                        : (string.IsNullOrEmpty(g.Key) ? "Unknown customer" : g.Key);
                    return new QuotationCustomerGroup
                    {
                        CustomerId = g.Key,
                        CustomerName = name,
                        Phone = cust?.Phone ?? "",
                        Quotations = g.OrderByDescending(q => q.CreatedAt).ToList()
                    };
                })
                .OrderBy(g => g.CustomerName)
                .ToList();

            ViewBag.CustomerFilter = customerId;
            ViewBag.Customers = BuildCustomerSelect(customerId);
            return View(groups);
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null)
                return NotFound();

            ViewBag.CustomerName = CustomerName(quote.CustomerId);
            ViewBag.VehicleLabel = VehicleLabel(quote.VehicleId);
            ViewBag.CustomerPhone = AppDataStore.Customers.FirstOrDefault(c => c.Id == quote.CustomerId)?.Phone ?? "";
            return View(quote);
        }

        [HttpGet]
        public IActionResult Create(string? customerId = null, string? jobCardId = null, string? vehicleId = null)
        {
            var model = new Quotation
            {
                CustomerId = customerId ?? string.Empty,
                JobCardId = jobCardId ?? string.Empty,
                VehicleId = vehicleId ?? string.Empty,
                Status = QuotationStatus.Pending,
                CreatedAt = DateTime.Now
            };

            if (!string.IsNullOrWhiteSpace(jobCardId))
            {
                var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == jobCardId);
                if (job != null)
                {
                    model.JobCardId = job.Id;
                    model.CustomerId = job.CustomerId;
                    model.VehicleId = job.VehichleId;
                    if (string.IsNullOrWhiteSpace(model.Description))
                        model.Description = job.Diagnosis ?? string.Empty;
                }
            }

            PopulateCreateLookups(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Quotation model)
        {
            PopulateCreateLookups(model);

            if (string.IsNullOrWhiteSpace(model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Please select the customer to quote.");
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.JobCardId))
            {
                var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == model.JobCardId);
                if (job == null)
                {
                    ModelState.AddModelError(nameof(model.JobCardId), "Job card not found.");
                    return View(model);
                }
                model.CustomerId = string.IsNullOrWhiteSpace(model.CustomerId) ? job.CustomerId : model.CustomerId;
                if (string.IsNullOrWhiteSpace(model.VehicleId))
                    model.VehicleId = job.VehichleId;
            }

            if (string.IsNullOrWhiteSpace(model.VehicleId))
            {
                var veh = AppDataStore.Vehicles.FirstOrDefault(v => v.CustomerId == model.CustomerId);
                if (veh != null)
                    model.VehicleId = veh.Id;
            }

            model.TotalCost = model.LabourCost + model.PartsCost - model.CustomerSuppliedDifference;
            if (model.TotalCost < 0) model.TotalCost = 0;

            model.Id = AppDataStore.NewId("quot");
            model.CreatedAt = DateTime.Now;
            model.Status = QuotationStatus.Pending;
            model.Description = model.Description?.Trim() ?? string.Empty;

            AppDataStore.Quotations.Add(model);

            TempData["Success"] = $"Quotation created for {CustomerName(model.CustomerId)} — total R {model.TotalCost:N2}. You can send it to the customer next.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        /// <summary>
        /// Staff: send quotation to the customer → creates a mobile notification
        /// and sets status to Sent.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendToCustomer(string id)
        {
            var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null)
                return NotFound();

            if (quote.Status == QuotationStatus.Approved || quote.Status == QuotationStatus.Rejected)
            {
                TempData["Error"] = "This quotation was already responded to by the customer.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var customer = AppDataStore.Customers.FirstOrDefault(c => c.Id == quote.CustomerId);
            var vehicle = AppDataStore.Vehicles.FirstOrDefault(v => v.Id == quote.VehicleId);
            var name = customer != null ? $"{customer.Name} {customer.Surname}".Trim() : "Customer";
            var reg = vehicle?.RegistrationNumber ?? "your vehicle";

            // Notification the mobile app would show (Flutter listens for Type = Quotation)
            var notification = new Notification
            {
                Id = AppDataStore.NewId("ntf"),
                UserAccountId = quote.CustomerId, // mobile uses customer id until UserAccount is linked
                Type = "Quotation",
                RelatedId = quote.Id,
                IsRead = false,
                CreatedAt = DateTime.Now,
                Message = $"Hi {name}, Mister Auto & Turbo has sent you a quotation for {reg}. " +
                          $"Total: R {quote.TotalCost:N2}. Open the app to Approve or Reject."
            };
            AppDataStore.Notifications.Add(notification);

            quote.Status = QuotationStatus.Sent;
            quote.SentAt = DateTime.Now;

            TempData["Success"] = $"Quotation sent to {name}. They will see a notification on the mobile app.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Customer mobile screen: view quotation and Approve / Reject.
        /// (Also usable in browser for demo until Flutter is connected.)
        /// GET /Quotations/CustomerView/{id}
        /// </summary>
        [HttpGet]
        public IActionResult CustomerView(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null)
                return NotFound();

            ViewBag.CustomerName = CustomerName(quote.CustomerId);
            ViewBag.VehicleLabel = VehicleLabel(quote.VehicleId);
            return View(quote);
        }

        /// <summary>Customer approves the quotation (mobile or demo page).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(string id)
        {
            return Respond(id, QuotationStatus.Approved);
        }

        /// <summary>Customer rejects the quotation.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(string id)
        {
            return Respond(id, QuotationStatus.Rejected);
        }

        private IActionResult Respond(string id, string newStatus)
        {
            var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null)
                return NotFound();

            if (quote.Status == QuotationStatus.Approved || quote.Status == QuotationStatus.Rejected)
            {
                TempData["Error"] = "You already responded to this quotation.";
                return RedirectToAction(nameof(CustomerView), new { id });
            }

            if (quote.Status != QuotationStatus.Sent && quote.Status != QuotationStatus.Pending)
            {
                TempData["Error"] = "This quotation cannot be updated.";
                return RedirectToAction(nameof(CustomerView), new { id });
            }

            quote.Status = newStatus;
            quote.RespondedAt = DateTime.Now;

            // Mark related customer notifications as read
            foreach (var n in AppDataStore.Notifications.Where(n => n.RelatedId == id && !n.IsRead))
                n.IsRead = true;

            // Notify workshop staff
            AppDataStore.Notifications.Add(new Notification
            {
                Id = AppDataStore.NewId("ntf"),
                UserAccountId = "staff",
                Type = "QuotationResponse",
                RelatedId = quote.Id,
                IsRead = false,
                CreatedAt = DateTime.Now,
                Message = $"Customer {CustomerName(quote.CustomerId)} {newStatus.ToLower()} quotation {quote.Id} (R {quote.TotalCost:N2})."
            });

            // If approved and job card exists, move job toward In Progress
            if (newStatus == QuotationStatus.Approved && !string.IsNullOrWhiteSpace(quote.JobCardId))
            {
                var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == quote.JobCardId);
                if (job != null && (job.Status == "Quotation" || job.Status == "Inspection"))
                    job.Status = "In Progress";
            }

            TempData["Success"] = newStatus == QuotationStatus.Approved
                ? "Thank you — you approved the quotation. The workshop will proceed with the work."
                : "You rejected the quotation. The workshop has been notified.";

            return RedirectToAction(nameof(CustomerView), new { id });
        }

        [HttpPost]
        public IActionResult Calculate(decimal labourCost, decimal partsCost, decimal customerSuppliedDifference)
        {
            var total = labourCost + partsCost - customerSuppliedDifference;
            if (total < 0) total = 0;
            return Json(new { labourCost, partsCost, customerSuppliedDifference, totalCost = total });
        }


        // GET: /Quotations/Edit/{id}
        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null)
                return NotFound();

            if (quote.Status == QuotationStatus.Approved || quote.Status == QuotationStatus.Rejected)
            {
                TempData["Error"] = "This quotation was already approved or rejected and cannot be edited. Create a new quotation instead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            PopulateCreateLookups(quote);
            return View(quote);
        }

        // POST: /Quotations/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, Quotation model)
        {
            if (id != model.Id)
                return NotFound();

            var existing = AppDataStore.Quotations.FirstOrDefault(q => q.Id == id);
            if (existing == null)
                return NotFound();

            if (existing.Status == QuotationStatus.Approved || existing.Status == QuotationStatus.Rejected)
            {
                TempData["Error"] = "This quotation was already approved or rejected and cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            PopulateCreateLookups(model);

            if (string.IsNullOrWhiteSpace(model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Please select the customer.");
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.JobCardId))
            {
                var job = AppDataStore.JobCards.FirstOrDefault(j => j.Id == model.JobCardId);
                if (job == null)
                {
                    ModelState.AddModelError(nameof(model.JobCardId), "Job card not found.");
                    return View(model);
                }
                if (string.IsNullOrWhiteSpace(model.VehicleId))
                    model.VehicleId = job.VehichleId;
            }

            if (string.IsNullOrWhiteSpace(model.VehicleId))
            {
                var veh = AppDataStore.Vehicles.FirstOrDefault(v => v.CustomerId == model.CustomerId);
                if (veh != null)
                    model.VehicleId = veh.Id;
            }

            var wasSent = existing.Status == QuotationStatus.Sent || existing.SentAt != null;

            existing.CustomerId = model.CustomerId;
            existing.JobCardId = model.JobCardId ?? string.Empty;
            existing.VehicleId = model.VehicleId ?? string.Empty;
            existing.Description = model.Description?.Trim() ?? string.Empty;
            existing.LabourCost = model.LabourCost;
            existing.PartsCost = model.PartsCost;
            existing.CustomerSuppliedDifference = model.CustomerSuppliedDifference;
            existing.TotalCost = model.LabourCost + model.PartsCost - model.CustomerSuppliedDifference;
            if (existing.TotalCost < 0)
                existing.TotalCost = 0;

            // After editing amounts, customer must be sent the updated quote again
            if (wasSent)
            {
                existing.Status = QuotationStatus.Pending;
                existing.SentAt = null;
                existing.RespondedAt = null;
                TempData["Success"] = "Quotation updated. Send it to the customer again so they see the new totals.";
            }
            else
            {
                existing.Status = QuotationStatus.Pending;
                TempData["Success"] = "Quotation updated.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Quotations/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null)
                return NotFound();

            AppDataStore.Quotations.Remove(quote);

            // Remove related quotation notifications
            var related = AppDataStore.Notifications.Where(n => n.RelatedId == id).ToList();
            foreach (var n in related)
                AppDataStore.Notifications.Remove(n);

            TempData["Success"] = $"Quotation {id} deleted.";
            return RedirectToAction(nameof(Index), new { customerId = quote.CustomerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string status)
        {
            var quote = AppDataStore.Quotations.FirstOrDefault(q => q.Id == id);
            if (quote == null)
                return NotFound();

            if (!QuotationStatus.All.Contains(status))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            quote.Status = status;
            if (status == QuotationStatus.Approved || status == QuotationStatus.Rejected)
                quote.RespondedAt = DateTime.Now;

            TempData["Success"] = $"Quotation status set to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public IActionResult ByCustomer(string customerId) =>
            RedirectToAction(nameof(Index), new { customerId });

        // --- helpers ---

        private void PopulateCreateLookups(Quotation model)
        {
            ViewBag.Customers = BuildCustomerSelect(model.CustomerId);

            ViewBag.JobCards = AppDataStore.JobCards
                .OrderByDescending(j => j.CreatedAt)
                .Select(j =>
                {
                    var v = AppDataStore.Vehicles.FirstOrDefault(x => x.Id == j.VehichleId);
                    var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == j.CustomerId);
                    var label = $"{j.Id} — {(c != null ? c.Name + " " + c.Surname : j.CustomerId)} / {(v != null ? v.RegistrationNumber : j.VehichleId)} ({j.Status})";
                    return new SelectListItem { Value = j.Id, Text = label, Selected = j.Id == model.JobCardId };
                })
                .ToList();

            ViewBag.Vehicles = AppDataStore.Vehicles
                .OrderBy(v => v.RegistrationNumber)
                .Select(v =>
                {
                    var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == v.CustomerId);
                    var label = $"{v.RegistrationNumber} — {v.Make} {v.Model} ({(c != null ? c.Name : v.CustomerId)})";
                    return new SelectListItem { Value = v.Id, Text = label, Selected = v.Id == model.VehicleId };
                })
                .ToList();
        }

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

        private static string CustomerName(string customerId)
        {
            var c = AppDataStore.Customers.FirstOrDefault(x => x.Id == customerId);
            return c != null ? $"{c.Name} {c.Surname}".Trim() : customerId;
        }

        private static string VehicleLabel(string vehicleId)
        {
            var v = AppDataStore.Vehicles.FirstOrDefault(x => x.Id == vehicleId);
            return v != null ? $"{v.RegistrationNumber} — {v.Make} {v.Model}" : vehicleId;
        }
    }

    public class QuotationCustomerGroup
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public List<Quotation> Quotations { get; set; } = new();
    }
}
