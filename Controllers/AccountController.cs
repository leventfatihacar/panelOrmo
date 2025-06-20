using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using panelOrmo.Models;
using panelOrmo.Services;
using System.Security.Claims;

namespace panelOrmo.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(DatabaseService databaseService, ILogger<AccountController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                string username = model?.Username ?? Request.Form["Username"].ToString();
                string password = model?.Password ?? Request.Form["Password"].ToString();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Username and password are required");
                    return View(model);
                }

                var user = await _databaseService.ValidateUser(username, password);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim("IsSuperAdmin", user.IsSuperAdmin.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    try
                    {
                        await _databaseService.LogActivity(user.Id, user.Username, "Login", "Users");
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Failed to log activity");
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return RedirectToAction("Login");
            }
        }

        // Test endpoint to check form submission
        [HttpPost]
        public IActionResult TestFormData()
        {
            var result = new
            {
                FormKeys = Request.Form.Keys.ToList(),
                FormData = Request.Form.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()),
                HasFiles = Request.Form.Files.Any(),
                ContentType = Request.ContentType,
                Method = Request.Method
            };

            return Json(result);
        }
    }
}