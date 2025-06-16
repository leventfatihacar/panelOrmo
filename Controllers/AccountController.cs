
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
                // Log all incoming data for debugging
                _logger.LogInformation("=== LOGIN DEBUG INFO ===");
                _logger.LogInformation("Model Username: '{Username}'", model?.Username ?? "NULL");
                _logger.LogInformation("Model Password Length: {Length}", model?.Password?.Length ?? 0);

                // Log form data directly
                _logger.LogInformation("Form Keys: {Keys}", string.Join(", ", Request.Form.Keys));
                foreach (var key in Request.Form.Keys)
                {
                    _logger.LogInformation("Form[{Key}]: '{Value}'", key, Request.Form[key]);
                }

                // Log ModelState
                _logger.LogInformation("ModelState Valid: {Valid}", ModelState.IsValid);
                foreach (var kvp in ModelState)
                {
                    _logger.LogInformation("ModelState[{Key}]: Valid={Valid}, Value='{Value}'",
                        kvp.Key, kvp.Value.ValidationState, kvp.Value.AttemptedValue);
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Validation Error: {Error}", error.ErrorMessage);
                    }
                    return View(model);
                }

                // Manual fallback if model binding fails
                string username = model?.Username ?? Request.Form["Username"].ToString();
                string password = model?.Password ?? Request.Form["Password"].ToString();

                _logger.LogInformation("Final values - Username: '{Username}', Password provided: {HasPassword}",
                    username, !string.IsNullOrEmpty(password));

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Username and password are required");
                    return View(model);
                }

                var user = await _databaseService.ValidateUser(username, password);
                _logger.LogInformation("Database validation result: {Result}", user != null ? "Success" : "Failed");

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

                    _logger.LogInformation("User {Username} logged in successfully", user.Username);

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
                    _logger.LogWarning("Invalid login attempt for username: {Username}", username);
                    ModelState.AddModelError("", "Invalid username or password");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
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

            _logger.LogInformation("Test Form Data: {@FormData}", result);
            return Json(result);
        }
    }
}