using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using panelOrmo.Services;


namespace panelOrmo.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {

        private readonly DatabaseService _databaseService;

        public HomeController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _databaseService.GetDashboardStats();
            return Json(stats);
        }
    }
}
