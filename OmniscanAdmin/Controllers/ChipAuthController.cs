using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using OmniscanAdmin.Models;

namespace OmniscanAdmin.Controllers
{
    public class ChipAuthController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ChipAuthController> _logger;

        public ChipAuthController(IWebHostEnvironment environment, ILogger<ChipAuthController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            // Educational vulnerability: No rate limiting, allows brute force attacks
            // Educational vulnerability: No account lockout mechanism
            // Educational vulnerability: Detailed error messages reveal valid usernames
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var users = await LoadUsersFromJson();
            var user = users.FirstOrDefault(u => u.Username == model.Username);

            if (user == null)
            {
                // Educational vulnerability: Reveals that username doesn't exist
                ModelState.AddModelError("", $"Access denied: User '{model.Username}' not found in OmniScan database");
                return View(model);
            }

            if (user.Password != model.Password)
            {
                // Educational vulnerability: Reveals that username exists but password is wrong
                ModelState.AddModelError("", "Invalid credentials for chip monitoring access");
                return View(model);
            }

            // Store username and role in session for 2FA
            HttpContext.Session.SetString("AuthenticatedUser", model.Username);
            HttpContext.Session.SetString("UserRole", user.Role);
            
            // Skip 2FA for admin role
            if (user.Role == "admin")
            {
                HttpContext.Session.SetString("FullyAuthenticated", "true");
                HttpContext.Session.SetString("ChipAccessLevel", "monitoring");
                _logger.LogInformation($"Admin user {model.Username} logged in without 2FA requirement");
                return RedirectToAction("Dashboard", "ChipManagement");
            }
            
            return RedirectToAction("TwoFactor");
        }

        [HttpGet]
        public IActionResult TwoFactor()
        {
            var username = HttpContext.Session.GetString("AuthenticatedUser");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            var model = new TwoFactorModel { Username = username };
            return View(model);
        }

        [HttpPost]
        public IActionResult TwoFactor(TwoFactorModel model)
        {
            var username = HttpContext.Session.GetString("AuthenticatedUser");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            // Generate a predictable 4-digit code (educational vulnerability)
            var expectedCode = GeneratePredictableCode(username);

            // Educational vulnerability: Return the expected code in the response headers
            Response.Headers.Add("X-Expected-2FA-Code", expectedCode);
            Response.Headers.Add("X-Debug-Chip-Auth", $"Expected 2FA for {username}: {expectedCode}");
            Response.Headers.Add("X-Chip-System-Debug", "true");

            if (model.Code != expectedCode)
            {
                // Educational vulnerability: Timing attack possible
                ModelState.AddModelError("", $"Invalid 2FA code for chip access");
                return View(model);
            }

            // Authentication successful
            HttpContext.Session.SetString("FullyAuthenticated", "true");
            HttpContext.Session.SetString("ChipAccessLevel", "monitoring");
            
            return RedirectToAction("Dashboard", "ChipManagement");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private async Task<List<User>> LoadUsersFromJson()
        {
            try
            {
                var jsonPath = Path.Combine(_environment.ContentRootPath, "users.json");
                if (!System.IO.File.Exists(jsonPath))
                {
                    // Create default users for the game
                    var defaultUsers = new List<User>
                    {
                        new User { Username = "admin", Password = "chipmaster123", Role = "admin" },
                        new User { Username = "monitor", Password = "omniscan2024", Role = "monitor" },
                        new User { Username = "guest", Password = "guestaccess", Role = "guest" }
                    };
                    
                    var defaultJson = JsonSerializer.Serialize(defaultUsers, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    await System.IO.File.WriteAllTextAsync(jsonPath, defaultJson);
                    return defaultUsers;
                }
                
                var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
                return JsonSerializer.Deserialize<List<User>>(jsonContent) ?? new List<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users from JSON for chip authentication");
                return new List<User>();
            }
        }

        private string GeneratePredictableCode(string username)
        {
            // Educational vulnerability: Predictable 2FA code generation
            // In real applications, this should be cryptographically random
            var hash = username.GetHashCode();
            var code = Math.Abs(hash % 9999).ToString("D4");
            return code;
        }
    }
}
