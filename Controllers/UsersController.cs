using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using panelOrmo.Models;
using panelOrmo.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace panelOrmo.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly DatabaseService _databaseService;

        public UsersController(DatabaseService databaseService, ILogger<UsersController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is super admin
            if (!User.HasClaim("IsSuperAdmin", "True"))
            {
                return Forbid();
            }

            var users = await _databaseService.GetAllUsers();
            return View(users);
        }

        public IActionResult Create()
        {
            if (!User.HasClaim("IsSuperAdmin", "True"))
            {
                return Forbid();
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "SuperAdmin"))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        // Removed debug log
                    }
                }
                return View(model);
            }

            var success = await _databaseService.CreateUser(model, int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"));
            if (success)
            {
                TempData["Success"] = "User created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Failed to create user. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int userId, string newPassword)
        {
            if (!User.HasClaim("IsSuperAdmin", "True"))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            var success = await _databaseService.ChangeUserPassword(userId, newPassword);
            if (success)
            {
                await _databaseService.LogActivity(currentUserId, username, "Change User Password", "Users", userId);
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Failed to change password" });
        }

    }
}
