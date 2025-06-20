using panelOrmo.Models;
using panelOrmo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace panelOrmo.Controllers
{
    [Authorize]
    public class CollectionGroupsController : Controller
    {
        private readonly DatabaseService _databaseService;
        private readonly FTPService _ftpService;

        public CollectionGroupsController(DatabaseService databaseService, FTPService ftpService)
        {
            _databaseService = databaseService;
            _ftpService = ftpService;
        }

        public async Task<IActionResult> Index()
        {
            var groups = await _databaseService.GetAllCollectionGroups();
            return View(groups);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Collections = await _databaseService.GetAllCollections();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CollectionGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Collections = await _databaseService.GetAllCollections();
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            string imagePath = null;
            if (model.Image != null)
            {
                imagePath = await _ftpService.UploadFile(model.Image, "/httpdocs/CMSFiles/Department/Image");
            }

            var success = await _databaseService.CreateCollectionGroup(model, userId, imagePath);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Create Collection Group", "CMSDepartment");
                TempData["Success"] = "Collection group created successfully";
                return RedirectToAction("Index");
            }

            ViewBag.Collections = await _databaseService.GetAllCollections();
            ModelState.AddModelError("", "Failed to create collection group");
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var group = await _databaseService.GetCollectionGroupById(id);
            if (group == null)
            {
                return NotFound();
            }

            var model = new CollectionGroupViewModel
            {
                Name = group.DName,
                ParentCollectionID = group.DParentID,
                LanguageID = group.DLanguageID,
                IsActive = group.DIsValid
            };

            ViewBag.Collections = await _databaseService.GetAllCollections();
            ViewBag.CurrentImageUrl = group.DPicture;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CollectionGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Collections = await _databaseService.GetAllCollections();
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            string imagePath = null;
            if (model.Image != null)
            {
                imagePath = await _ftpService.UploadFile(model.Image, "/httpdocs/CMSFiles/Department/Image");
            }

            var success = await _databaseService.UpdateCollectionGroup(id, model, userId, imagePath);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Update Collection Group", "CMSDepartment", id);
                TempData["Success"] = "Collection group updated successfully";
                return RedirectToAction("Index");
            }

            ViewBag.Collections = await _databaseService.GetAllCollections();
            ModelState.AddModelError("", "Failed to update collection group");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
            var success = await _databaseService.DeleteCollectionGroup(id);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Delete Collection Group", "CMSDepartment", id);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
