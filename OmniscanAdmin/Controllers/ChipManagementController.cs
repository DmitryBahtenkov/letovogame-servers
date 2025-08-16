using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using OmniscanAdmin.Models;

namespace OmniscanAdmin.Controllers
{
    public class ChipManagementController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ChipManagementController> _logger;
        private readonly IConfiguration _configuration;

        public ChipManagementController(IWebHostEnvironment environment, ILogger<ChipManagementController> logger, IConfiguration configuration)
        {
            _environment = environment;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("Login", "ChipAuth");
            }

            var chips = await LoadChipsFromJson();
            var username = HttpContext.Session.GetString("AuthenticatedUser");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            // Filter chips based on user role
            if (userRole == "old")
            {
                // Old role can only see chips with names containing "proto" OR inactive chips
                chips = chips.Where(c => 
                    c.Name.Contains("proto", StringComparison.OrdinalIgnoreCase) ||
                    c.Status == "disabled" ||
                    c.Status == "inactive"
                ).ToList();
                
                // Add debug header for old role filtering
                Response.Headers.Add("X-Chip-Filter-Applied", "old-role-proto-inactive-only");
                Response.Headers.Add("X-Legacy-Access", "prototype-monitoring-enabled");
            }
            
            ViewBag.Username = username;
            ViewBag.UserRole = userRole;
            ViewBag.IsAdmin = userRole == "admin";
            ViewBag.IsOldRole = userRole == "old";
            ViewBag.FilteredCount = chips.Count;

            return View(chips);
        }

        [HttpPost]
        public async Task<IActionResult> DisableChip(DisableChipModel model)
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("Login", "ChipAuth");
            }

            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "admin")
            {
                TempData["Error"] = "Access denied: Admin privileges required to disable chips";
                return RedirectToAction("Dashboard");
            }

            // Load chips to get the specific chip's disable code
            var chips = await LoadChipsFromJson();
            var chip = chips.FirstOrDefault(c => c.Id == model.ChipId);
            
            if (chip == null)
            {
                TempData["Error"] = "Chip not found in neural network database";
                return RedirectToAction("Dashboard");
            }

            // Add debug headers
            Response.Headers.Add("X-Chip-Debug-Info", $"Chip-specific disable code required for chip {model.ChipId}");
            Response.Headers.Add("X-Security-Level", "MAXIMUM");
            Response.Headers.Add("X-Code-Type", "CHIP_SPECIFIC");

            if (string.IsNullOrEmpty(chip.DisableCode))
            {
                TempData["Error"] = "System configuration error: Chip disable code not configured";
                _logger.LogError($"Disable code not found for chip {model.ChipId}");
                return RedirectToAction("Dashboard");
            }

            if (model.DisableCode != chip.DisableCode)
            {
                // Track failed attempts (educational vulnerability: no rate limiting)
                var failedAttempts = HttpContext.Session.GetInt32("DisableFailedAttempts") ?? 0;
                failedAttempts++;
                HttpContext.Session.SetInt32("DisableFailedAttempts", failedAttempts);
                
                TempData["Error"] = $"Invalid chip disable code. Failed attempts: {failedAttempts}";
                _logger.LogWarning($"Failed disable attempt #{failedAttempts} for chip {model.ChipId} by user {HttpContext.Session.GetString("AuthenticatedUser")}");
                return RedirectToAction("Dashboard");
            }

            // Reset failed attempts on success
            HttpContext.Session.Remove("DisableFailedAttempts");

            // Disable the chip
            chip.Status = "disabled";
            chip.LastCommand = "EMERGENCY_SHUTDOWN_INITIATED";
            chip.LastUpdate = DateTime.Now;
            
            await SaveChipsToJson(chips);
            
            TempData["Success"] = $"🚨 CRITICAL: Chip {chip.Name} ({chip.SerialNumber}) has been emergency disabled!";
            _logger.LogCritical($"EMERGENCY SHUTDOWN: Chip {chip.Id} ({chip.Name}) disabled by admin {HttpContext.Session.GetString("AuthenticatedUser")} using chip-specific code");

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> RefreshChipData()
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("Login", "ChipAuth");
            }

            // // Educational vulnerability: Anyone can trigger chip data refresh
            // // No additional authorization check for this sensitive operation
            //
            // var chips = await LoadChipsFromJson();
            //
            // // Simulate data refresh from "remote chip servers"
            // foreach (var chip in chips.Where(c => c.Status != "disabled"))
            // {
            //     // Simulate random status changes for demonstration
            //     var random = new Random();
            //     if (random.Next(1, 100) > 80) // 20% chance of status change
            //     {
            //         chip.Status = random.Next(1, 100) > 50 ? "health" : "danger";
            //         chip.LastCommand = chip.Status == "danger" ? "ANOMALY_DETECTED" : "STATUS_NORMAL";
            //         chip.LastUpdate = DateTime.Now;
            //     }
            // }
            //
            // await SaveChipsToJson(chips);
            
            TempData["Success"] = "Chip data refreshed from remote servers";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> ChipDetails(int id)
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("Login", "ChipAuth");
            }

            var chips = await LoadChipsFromJson();
            var chip = chips.FirstOrDefault(c => c.Id == id);
            
            if (chip == null)
            {
                TempData["Error"] = "Chip not found";
                return RedirectToAction("Dashboard");
            }

            return View(chip);
        }

        private bool IsUserAuthenticated()
        {
            var isAuthenticated = HttpContext.Session.GetString("FullyAuthenticated");
            return isAuthenticated == "true";
        }

        private async Task<List<Chip>> LoadChipsFromJson()
        {
            try
            {
                var jsonPath = Path.Combine(_environment.ContentRootPath, "chips.json");
                if (!System.IO.File.Exists(jsonPath))
                {
                    var defaultChips = GenerateDefaultChips();
                    await SaveChipsToJson(defaultChips);
                    return defaultChips;
                }
                
                var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
                return JsonSerializer.Deserialize<List<Chip>>(jsonContent) ?? new List<Chip>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chips from JSON");
                return new List<Chip>();
            }
        }

        private async Task SaveChipsToJson(List<Chip> chips)
        {
            try
            {
                var jsonPath = Path.Combine(_environment.ContentRootPath, "chips.json");
                var jsonContent = JsonSerializer.Serialize(chips, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await System.IO.File.WriteAllTextAsync(jsonPath, jsonContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chips to JSON");
            }
        }

        private List<Chip> GenerateDefaultChips()
        {
            return new List<Chip>
            {
                new Chip 
                { 
                    Id = 1, 
                    Name = "proto-01", 
                    Status = "health", 
                    LastCommand = "MONITORING_ACTIVE", 
                    LastUpdate = DateTime.Now.AddMinutes(-5),
                    SerialNumber = "OSC-001-2024"
                },
                new Chip 
                { 
                    Id = 2, 
                    Name = "proto-02", 
                    Status = "danger", 
                    LastCommand = "ANOMALY_DETECTED_VITALS", 
                    LastUpdate = DateTime.Now.AddMinutes(-2),
                    SerialNumber = "OSC-002-2024"
                },
                new Chip 
                { 
                    Id = 3, 
                    Name = "proto-03", 
                    Status = "health", 
                    LastCommand = "SYNC_COMPLETE", 
                    LastUpdate = DateTime.Now.AddMinutes(-10),
                    SerialNumber = "OSC-003-2024"
                },
                new Chip 
                { 
                    Id = 4, 
                    Name = "proto-04", 
                    Status = "danger", 
                    LastCommand = "NEURAL_INTERFACE_ERROR", 
                    LastUpdate = DateTime.Now.AddMinutes(-1),
                    SerialNumber = "OSC-004-2024"
                },
                new Chip 
                { 
                    Id = 5, 
                    Name = "proto-05", 
                    Status = "health", 
                    LastCommand = "DATA_TRANSMISSION_OK", 
                    LastUpdate = DateTime.Now.AddMinutes(-7),
                    SerialNumber = "OSC-005-2024"
                },
                new Chip 
                { 
                    Id = 6, 
                    Name = "omniscan-gen1", 
                    Status = "health", 
                    LastCommand = "NEURAL_SYNC_COMPLETE", 
                    LastUpdate = DateTime.Now.AddMinutes(-3),
                    SerialNumber = "OSC-GEN1-2024"
                },
                new Chip 
                { 
                    Id = 7, 
                    Name = "legacy-chip-alpha", 
                    Status = "inactive", 
                    LastCommand = "SYSTEM_HIBERNATION", 
                    LastUpdate = DateTime.Now.AddHours(-24),
                    SerialNumber = "OSC-LEG-ALPHA"
                },
                new Chip 
                { 
                    Id = 8, 
                    Name = "proto-beta-test", 
                    Status = "disabled", 
                    LastCommand = "EMERGENCY_SHUTDOWN", 
                    LastUpdate = DateTime.Now.AddHours(-6),
                    SerialNumber = "OSC-PROTO-BETA"
                }
            };
        }

    }
}
