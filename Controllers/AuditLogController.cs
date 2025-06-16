using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using panelOrmo.Services;

namespace panelOrmo.Controllers
{
    [Authorize]
    public class AuditLogController : Controller
    {

        private readonly DatabaseService _databaseService;

        public AuditLogController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _databaseService.GetAuditLogs();
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            var logs = await _databaseService.GetRecentAuditLogs(10);
            return Json(logs);
        }

    }
}
