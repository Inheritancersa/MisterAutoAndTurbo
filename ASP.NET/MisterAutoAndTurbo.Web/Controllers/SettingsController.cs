using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterAutoAndTurbo.Web.Data;
using MisterAutoAndTurbo.Web.Models;
using MisterAutoAndTurbo.Web.ViewModels;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Manager only: business details and payment settings.
    /// Kept in AppDataStore.Settings and saved to the database after each POST.
    /// </summary>
    [Authorize(Roles = "Manager")]
    public class SettingsController : Controller
    {
        // GET: /Settings
        [HttpGet]
        public IActionResult Index()
        {
            var s = AppDataStore.Settings;
            return View(new SettingsViewModel
            {
                BusinessName = s.BusinessName,
                VatNumber = s.VatNumber,
                Address = s.Address,
                Currency = s.Currency,
                VatRate = s.VatRate
            });
        }

        // POST: /Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(SettingsViewModel form)
        {
            if (!string.IsNullOrWhiteSpace(form.Currency) && !BusinessSettings.Currencies.ContainsKey(form.Currency))
                ModelState.AddModelError(nameof(form.Currency), "Choose a valid currency.");

            if (!ModelState.IsValid)
                return View(form);

            var s = AppDataStore.Settings;
            s.BusinessName = form.BusinessName.Trim();
            s.VatNumber = form.VatNumber?.Trim() ?? string.Empty;
            s.Address = form.Address?.Trim() ?? string.Empty;
            s.Currency = form.Currency;
            s.VatRate = form.VatRate ?? s.VatRate;

            TempData["Success"] = "Settings saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
