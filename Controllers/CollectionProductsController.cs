using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using panelOrmo.Models;
using panelOrmo.Services;
using System.Security.Claims;

namespace panelOrmo.Controllers
{
    public class CollectionProductsController : Controller
    {
        private readonly DatabaseService _databaseService;
        private readonly FTPService _ftpService;

        public CollectionProductsController(DatabaseService databaseService, FTPService ftpService)
        {
            _databaseService = databaseService;
            _ftpService = ftpService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _databaseService.GetAllCollectionProducts();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.CollectionGroups = await _databaseService.GetAllCollectionGroups();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CollectionProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CollectionGroups = await _databaseService.GetAllCollectionGroups();
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            var success = await _databaseService.CreateCollectionProduct(model, userId);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Create Collection Product", "CMSProduct");
                TempData["Success"] = "Collection product created successfully";
                return RedirectToAction("Index");
            }

            ViewBag.CollectionGroups = await _databaseService.GetAllCollectionGroups();
            ModelState.AddModelError("", "Failed to create collection product");
            return View(model);
        }

    }
}
