using Microsoft.AspNetCore.Mvc;

namespace MisterAutoAndTurbo.Web.Controllers
{
    public class UsersController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
