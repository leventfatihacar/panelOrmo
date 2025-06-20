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

        public async Task<IActionResult> Edit(int id)
        {
            var model = await _databaseService.GetCollectionProductForEdit(id);
            if (model == null)
            {
                return NotFound();
            }

            ViewBag.CollectionGroups = await _databaseService.GetAllCollectionGroups();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CollectionProductEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CollectionGroups = await _databaseService.GetAllCollectionGroups();
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            var success = await _databaseService.UpdateCollectionProduct(id, model.Product, userId);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Update Collection Product", "CMSProduct", id);
                TempData["Success"] = "Collection product updated successfully";
                return RedirectToAction("Index");
            }

            ViewBag.CollectionGroups = await _databaseService.GetAllCollectionGroups();
            ModelState.AddModelError("", "Failed to update collection product");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
            var success = await _databaseService.DeleteCollectionProduct(id);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Delete Collection Product", "CMSProduct", id);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
