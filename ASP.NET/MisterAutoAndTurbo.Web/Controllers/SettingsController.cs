using Microsoft.AspNetCore.Mvc;

namespace MisterAutoAndTurbo.Web.Controllers
{
    public class SettingsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
