using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using panelOrmo.Models;
using panelOrmo.Services;
using System.Security.Claims;
using System.Linq;

namespace panelOrmo.Controllers
{
    [Authorize]
    public class CollectionsController : Controller
    {
        private readonly DatabaseService _databaseService;
        private readonly FTPService _ftpService;

        public CollectionsController(DatabaseService databaseService, FTPService ftpService)
        {
            _databaseService = databaseService;
            _ftpService = ftpService;
        }

        public async Task<IActionResult> Index()
        {
            var collections = await _databaseService.GetAllCollections();
            return View(collections.OrderByDescending(c => c.DCreatedDate).ToList());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CollectionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            string imagePath = null;
            if (model.Image != null)
            {
                imagePath = await _ftpService.UploadFile(model.Image, "/httpdocs/CMSFiles/Department/Image");
            }

            var success = await _databaseService.CreateCollection(model, userId, imagePath);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Create Collection", "CMSBanner");
                TempData["Success"] = "Collection created successfully";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to create collection");
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var collection = await _databaseService.GetCollectionById(id);
            if (collection == null)
            {
                return NotFound();
            }

            var model = new CollectionViewModel
            {
                Name = collection.DName,
                Summary = collection.DSummary,
                LanguageID = collection.DLanguageID,
                IsActive = collection.DIsValid
            };
            ViewBag.CurrentImageUrl = !string.IsNullOrEmpty(collection.DPicture) 
                ? $"/Image/collections/{collection.DPicture}" 
                : null;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CollectionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            string imagePath = null;
            if (model.Image != null)
            {
                imagePath = await _ftpService.UploadFile(model.Image, "/httpdocs/CMSFiles/Department/Image");
            }

            var success = await _databaseService.UpdateCollection(id, model, userId, imagePath);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Update Collection", "CMSBanner", id);
                TempData["Success"] = "Collection updated successfully";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to update collection");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            var success = await _databaseService.ToggleCollectionStatus(request.Id, request.Status);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Toggle Collection Status", "CMSBanner", request.Id);
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
            var success = await _databaseService.DeleteCollection(id);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Delete Collection", "CMSBanner", id);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
    public class ToggleStatusRequest
    {
        public int Id { get; set; }
        public bool Status { get; set; }
    }
}


