using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using panelOrmo.Models;
using panelOrmo.Services;
using System.Security.Claims;
using System.Linq;

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
            var products = await _databaseService.GetAllCollectionProductsWithGroups();
            return View(products.OrderByDescending(p => p.PCreatedDate).ToList());
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

            // Convert form data to ImagePairs if they exist
            if (Request.Form.Files.Any())
            {
                var imagePairs = new List<ImagePair>();
                var groupedFiles = Request.Form.Files
                    .Where(f => f.Name.StartsWith("ImagePairs["))
                    .GroupBy(f => {
                        // Extract index from names like "ImagePairs[0].SmallImage"
                        var startIndex = f.Name.IndexOf('[') + 1;
                        var endIndex = f.Name.IndexOf(']');
                        if (startIndex > 0 && endIndex > startIndex)
                        {
                            var indexStr = f.Name.Substring(startIndex, endIndex - startIndex);
                            return int.TryParse(indexStr, out var index) ? index : -1;
                        }
                        return -1;
                    })
                    .Where(g => g.Key >= 0)
                    .OrderBy(g => g.Key);

                foreach (var group in groupedFiles)
                {
                    var smallImage = group.FirstOrDefault(f => f.Name.Contains(".SmallImage"));
                    var mediumImage = group.FirstOrDefault(f => f.Name.Contains(".MediumImage"));

                    if (smallImage != null || mediumImage != null)
                    {
                        imagePairs.Add(new ImagePair
                        {
                            SmallImage = smallImage,
                            MediumImage = mediumImage
                        });
                    }
                }

                model.ImagePairs = imagePairs;
            }

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

            // Get existing images for the view
            var existingImages = await _databaseService.GetProductImages(id);
            ViewBag.ExistingImages = existingImages;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CollectionProductEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CollectionGroups = await _databaseService.GetAllCollectionGroups();
                var existingImages = await _databaseService.GetProductImages(id);
                ViewBag.ExistingImages = existingImages;
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            // Process existing images with replacements
            if (Request.Form.Files.Any())
            {
                var existingImagesList = new List<ExistingImagePair>();
                var newImagePairsList = new List<ImagePair>();

                // Process existing image replacements
                var existingFiles = Request.Form.Files
                    .Where(f => f.Name.StartsWith("ExistingImages["))
                    .GroupBy(f => {
                        var startIndex = f.Name.IndexOf('[') + 1;
                        var endIndex = f.Name.IndexOf(']');
                        if (startIndex > 0 && endIndex > startIndex)
                        {
                            var indexStr = f.Name.Substring(startIndex, endIndex - startIndex);
                            return int.TryParse(indexStr, out var index) ? index : -1;
                        }
                        return -1;
                    })
                    .Where(g => g.Key >= 0);

                foreach (var group in existingFiles)
                {
                    var smallImage = group.FirstOrDefault(f => f.Name.Contains(".NewSmallImage"));
                    var mediumImage = group.FirstOrDefault(f => f.Name.Contains(".NewMediumImage"));
                    var piidValue = Request.Form[$"ExistingImages[{group.Key}].PIID"].FirstOrDefault();

                    if (int.TryParse(piidValue, out var piid))
                    {
                        existingImagesList.Add(new ExistingImagePair
                        {
                            PIID = piid,
                            NewSmallImage = smallImage,
                            NewMediumImage = mediumImage
                        });
                    }
                }

                // Process new image pairs
                var newFiles = Request.Form.Files
                    .Where(f => f.Name.StartsWith("NewImagePairs["))
                    .GroupBy(f => {
                        var startIndex = f.Name.IndexOf('[') + 1;
                        var endIndex = f.Name.IndexOf(']');
                        if (startIndex > 0 && endIndex > startIndex)
                        {
                            var indexStr = f.Name.Substring(startIndex, endIndex - startIndex);
                            return int.TryParse(indexStr, out var index) ? index : -1;
                        }
                        return -1;
                    })
                    .Where(g => g.Key >= 0)
                    .OrderBy(g => g.Key);

                foreach (var group in newFiles)
                {
                    var smallImage = group.FirstOrDefault(f => f.Name.Contains(".SmallImage"));
                    var mediumImage = group.FirstOrDefault(f => f.Name.Contains(".MediumImage"));

                    if (smallImage != null || mediumImage != null)
                    {
                        newImagePairsList.Add(new ImagePair
                        {
                            SmallImage = smallImage,
                            MediumImage = mediumImage
                        });
                    }
                }

                model.ExistingImages = existingImagesList;
                model.NewImagePairs = newImagePairsList;
            }

            var success = await _databaseService.UpdateCollectionProduct(id, model, userId);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Update Collection Product", "CMSProduct", id);
                TempData["Success"] = "Collection product updated successfully";
                return RedirectToAction("Index");
            }

            ViewBag.CollectionGroups = await _databaseService.GetAllCollectionGroups();
            var existingImagesForView = await _databaseService.GetProductImages(id);
            ViewBag.ExistingImages = existingImagesForView;
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