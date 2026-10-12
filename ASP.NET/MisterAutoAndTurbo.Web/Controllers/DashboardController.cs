using Microsoft.AspNetCore.Mvc;

namespace MisterAutoAndTurbo.Web.Controllers
{
    /// <summary>
    /// Role-based landing after login.
    /// Manager: workshop performance, active/completed jobs, reports entry.
    /// Receptionist / Mechanic: scoped task hubs.
    /// </summary>
    public class DashboardController : Controller
    {
        // GET: /Dashboard  (default after successful login)
        [HttpGet]
        public IActionResult Index()
        {
            // TODO: Resolve role from session / claims and load summary data from Supabase
            // Manager: active jobs, payments today, performance snapshot
            // Receptionist: today's appointments, open job cards
            // Mechanic: assigned jobs count

            ViewData["Role"] = "Manager"; // placeholder until auth is wired
            return View();
        }

        // GET: /Dashboard/Manager
        [HttpGet]
        public IActionResult Manager()
        {
            // Monitor Workshop Performance, View Active/Completed Jobs entry points
            return View();
        }

        // GET: /Dashboard/Receptionist
        [HttpGet]
        public IActionResult Receptionist()
        {
            return View();
        }

        // GET: /Dashboard/Mechanic
        [HttpGet]
        public IActionResult Mechanic()
        {
            return View();
        }

        // GET: /Dashboard/Reports  (Manager: Generate Reports)
        [HttpGet]
        public IActionResult Reports()
        {
            // TODO: Retrieve workshop data for reports
            // alt [records found] -> Display reports
            // else -> No data for report
            return View();
        }

        // GET: /Dashboard/Performance  (Manager: Monitor Workshop Performance)
        [HttpGet]
        public IActionResult Performance()
        {
            // TODO: Retrieve workshop performance data
            return View();
        }
    }
}
