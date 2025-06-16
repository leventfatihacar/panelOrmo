using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using panelOrmo.Models;
using panelOrmo.Services;
using System.Security.Claims;

namespace panelOrmo.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {

        private readonly DatabaseService _databaseService;

        public UsersController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
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
            if (!User.HasClaim("IsSuperAdmin", "True"))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            var success = await _databaseService.CreateUser(model, userId);
            if (success)
            {
                await _databaseService.LogActivity(userId, username, "Create User", "Users");
                TempData["Success"] = "User created successfully";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to create user");
            return View(model);
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
