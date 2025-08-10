using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VulnerableLogin.Models;

namespace VulnerableLogin.Controllers
{
    public class AuthController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IWebHostEnvironment environment, ILogger<AuthController> logger)
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
            // Vulnerability: No rate limiting, allows brute force attacks
            // Vulnerability: No account lockout mechanism
            // Vulnerability: Detailed error messages reveal valid usernames
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var users = await LoadUsersFromJson();
            var user = users.FirstOrDefault(u => u.Username == model.Username);

            if (user == null)
            {
                // Vulnerability: Reveals that username doesn't exist
                ModelState.AddModelError("", $"Username '{model.Username}' not found");
                return View(model);
            }

            if (user.Password != model.Password)
            {
                // Vulnerability: Reveals that username exists but password is wrong
                ModelState.AddModelError("", "Invalid password for this user");
                return View(model);
            }

            // Store username in session for 2FA
            HttpContext.Session.SetString("AuthenticatedUser", model.Username);
            
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

            // Generate a predictable 4-digit code (vulnerability for educational purposes)
            var expectedCode = GeneratePredictableCode(username);

            // Vulnerability: Return the expected code in the response headers for dev tools inspection
            Response.Headers.Add("X-Expected-2FA-Code", expectedCode);
            Response.Headers.Add("X-Debug-Info", $"Expected code for {username}: {expectedCode}");

            if (model.Code != expectedCode)
            {
                // Vulnerability: Expose the correct code in error message
                ModelState.AddModelError("", $"Invalid code.");
                return View(model);
            }

            // Authentication successful
            HttpContext.Session.SetString("FullyAuthenticated", "true");
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var isAuthenticated = HttpContext.Session.GetString("FullyAuthenticated");
            if (isAuthenticated != "true")
            {
                return RedirectToAction("Login");
            }

            var username = HttpContext.Session.GetString("AuthenticatedUser");
            ViewBag.Username = username;
            return View();
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
                var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
                return JsonSerializer.Deserialize<List<User>>(jsonContent) ?? new List<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users from JSON");
                return new List<User>();
            }
        }

        private string GeneratePredictableCode(string username)
        {
            // Vulnerability: Predictable 2FA code generation for educational purposes
            // In real applications, this should be cryptographically random
            var hash = username.GetHashCode();
            var code = Math.Abs(hash % 9999).ToString("D4");
            return code;
        }
    }
}
