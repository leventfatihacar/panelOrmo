using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using panelOrmo.Models;
using panelOrmo.Services;
using System.Security.Claims;

namespace panelOrmo.Controllers
{
    [Authorize]
    public class NewsController : Controller
    {

        private readonly DatabaseService _databaseService;
        private readonly FTPService _ftpService;

        public NewsController(DatabaseService databaseService, FTPService ftpService)
        {
            _databaseService = databaseService;
            _ftpService = ftpService;
        }

        public async Task<IActionResult> Index()
        {
            var news = await _databaseService.GetAllNews();
            return View(news);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(NewsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            string imagePath = null;
            if (model.Image != null)
            {
                imagePath = await _ftpService.UploadFile(model.Image, "/httpdocs/CMSFiles/Image/Content");
            }

            var success = await _databaseService.CreateNews(model, userId, imagePath);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Create News", "CMSContent");
                TempData["Success"] = "News created successfully";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to create news");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            var success = await _databaseService.ToggleNewsStatus(request.Id, request.Status);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Toggle News Status", "CMSContent", request.Id);
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }
    }
}
