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

        [AllowAnonymous]
        [HttpGet("generate-password")]
        public IActionResult GeneratePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return BadRequest("Please provide a password in the query string, e.g., /generate-password?password=your_password");
            }
            var (hash, salt) = _databaseService.GetHashAndSalt(password);
            return Ok(new { password, hash, salt });
        }
    }
}
