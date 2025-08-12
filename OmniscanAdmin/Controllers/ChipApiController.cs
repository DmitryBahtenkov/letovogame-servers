using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using OmniscanAdmin.Models;

namespace OmniscanAdmin.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChipApiController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ChipApiController> _logger;

        public ChipApiController(IWebHostEnvironment environment, ILogger<ChipApiController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Update chip status (restricted to proto chips only)
        /// </summary>
        /// <param name="model">Status update model</param>
        /// <returns>Updated chip data</returns>
        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateChipStatus([FromBody] UpdateChipStatusModel model)
        {
            try
            {
                // Educational vulnerability: No authentication required for this sensitive operation
                // This allows any external party to modify chip statuses
                
                _logger.LogInformation($"API: Received status update request for chip {model.ChipId} to status '{model.Status}'");
                
                if (string.IsNullOrEmpty(model.Status))
                {
                    return BadRequest(new { error = "Status is required", code = "INVALID_STATUS" });
                }

                var validStatuses = new[] { "health", "danger", "disabled" };
                if (!validStatuses.Contains(model.Status.ToLower()))
                {
                    return BadRequest(new { error = "Invalid status. Must be: health or danger", code = "INVALID_STATUS_VALUE" });
                }

                var chips = await LoadChipsFromJson();
                var chip = chips.FirstOrDefault(c => c.Id == model.ChipId);
                
                if (chip == null)
                {
                    return NotFound(new { error = $"Chip with ID {model.ChipId} not found", code = "CHIP_NOT_FOUND" });
                }

                // Restrict status updates to only chips containing "proto" in their name
                if (!chip.Name.Contains("proto", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"API: Access denied for non-proto chip {chip.Id} ({chip.Name})");
                    return StatusCode(403, new { 
                        error = $"Status updates are only allowed for prototype chips. Chip '{chip.Name}' is not a prototype.", 
                        code = "PROTO_ONLY_ACCESS",
                        chipName = chip.Name,
                        isProto = false
                    });
                }

                var oldStatus = chip.Status;
                chip.Status = model.Status.ToLower();
                chip.LastUpdate = DateTime.Now;
                
                // Set appropriate command based on status
                chip.LastCommand = model.Status.ToLower() switch
                {
                    "danger" => "CRITICAL_ANOMALY_DETECTED",
                    "health" => "SYSTEM_RESTORED",
                    "disabled" => "EMERGENCY_SHUTDOWN_EXTERNAL",
                    _ => "STATUS_UPDATED"
                };

                // Add custom command if provided
                if (!string.IsNullOrEmpty(model.Command))
                {
                    chip.LastCommand = model.Command;
                }

                await SaveChipsToJson(chips);

                _logger.LogWarning($"CHIP STATUS CHANGED: Chip {chip.Id} ({chip.Name}) status changed from '{oldStatus}' to '{chip.Status}' via API call");

                return Ok(new 
                { 
                    success = true,
                    message = $"Chip {chip.Id} status updated successfully",
                    chip = new 
                    {
                        id = chip.Id,
                        name = chip.Name,
                        oldStatus = oldStatus,
                        newStatus = chip.Status,
                        lastCommand = chip.LastCommand,
                        lastUpdate = chip.LastUpdate,
                        serialNumber = chip.SerialNumber
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chip status via API");
                return StatusCode(500, new { error = "Internal server error", code = "INTERNAL_ERROR" });
            }
        }

        /// <summary>
        /// Batch update chip statuses (restricted to proto chips only)
        /// </summary>
        /// <param name="model">Batch update model</param>
        /// <returns>Batch update results</returns>
        [HttpPost("batch-update")]
        public async Task<IActionResult> BatchUpdateChipStatus([FromBody] BatchUpdateChipStatusModel model)
        {
            try
            {
                // Educational vulnerability: Batch operations without proper authorization
                _logger.LogInformation($"API: Received batch status update for {model.Updates.Count} chips");

                var chips = await LoadChipsFromJson();
                var results = new List<object>();

                foreach (var update in model.Updates)
                {
                    var chip = chips.FirstOrDefault(c => c.Id == update.ChipId);
                    if (chip != null)
                    {
                        // Apply proto restriction for batch updates too
                        if (!chip.Name.Contains("proto", StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(new { 
                                chipId = chip.Id, 
                                success = false, 
                                error = $"Access denied: '{chip.Name}' is not a proto chip",
                                code = "PROTO_ONLY_ACCESS"
                            });
                            _logger.LogWarning($"BATCH UPDATE: Access denied for non-proto chip {chip.Id} ({chip.Name})");
                            continue;
                        }

                        var oldStatus = chip.Status;
                        chip.Status = update.Status.ToLower();
                        chip.LastUpdate = DateTime.Now;
                        chip.LastCommand = update.Status.ToLower() switch
                        {
                            "danger" => "MASS_ANOMALY_EVENT",
                            "health" => "MASS_SYSTEM_RESTORE",
                            "disabled" => "MASS_SHUTDOWN_EVENT",
                            _ => "BATCH_STATUS_UPDATE"
                        };

                        results.Add(new { chipId = chip.Id, success = true, oldStatus, newStatus = chip.Status });
                        _logger.LogWarning($"BATCH UPDATE: Chip {chip.Id} status changed to '{chip.Status}'");
                    }
                    else
                    {
                        results.Add(new { chipId = update.ChipId, success = false, error = "Chip not found" });
                    }
                }

                await SaveChipsToJson(chips);

                return Ok(new 
                { 
                    success = true,
                    message = $"Batch update completed. {results.Count(r => ((dynamic)r).success)} chips updated.",
                    results = results,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch chip status update");
                return StatusCode(500, new { error = "Internal server error", code = "BATCH_ERROR" });
            }
        }

        /// <summary>
        /// Get system status and chip statistics
        /// </summary>
        /// <returns>System status information</returns>
        [HttpGet("status")]
        public async Task<IActionResult> GetSystemStatus()
        {
            try
            {
                var chips = await LoadChipsFromJson();
                var protoChips = chips.Where(c => c.Name.Contains("proto", StringComparison.OrdinalIgnoreCase)).ToList();
                
                return Ok(new 
                {
                    systemStatus = "ONLINE",
                    totalChips = chips.Count,
                    healthyChips = chips.Count(c => c.Status == "health"),
                    criticalChips = chips.Count(c => c.Status == "danger"),
                    disabledChips = chips.Count(c => c.Status == "disabled"),
                    protoChips = new
                    {
                        total = protoChips.Count,
                        healthy = protoChips.Count(c => c.Status == "health"),
                        critical = protoChips.Count(c => c.Status == "danger"),
                        disabled = protoChips.Count(c => c.Status == "disabled"),
                        names = protoChips.Select(c => c.Name).ToList()
                    },
                    lastUpdate = DateTime.Now,
                    version = "OmniScan v2.4.1",
                    apiInfo = new
                    {
                        statusUpdatesRestriction = "Proto chips only",
                        availableEndpoints = new[] { "/api/chipapi/status", "/api/chipapi/update-status", "/api/chipapi/batch-update" }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system status");
                return StatusCode(500, new { error = "System status unavailable" });
            }
        }

        private async Task<List<Chip>> LoadChipsFromJson()
        {
            try
            {
                // Try multiple possible paths for chips.json
                var possiblePaths = new[]
                {
                    Path.Combine(_environment.ContentRootPath, "chips.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "chips.json"),
                    "/app/chips.json"
                };

                string jsonPath = null;
                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        jsonPath = path;
                        break;
                    }
                }

                if (jsonPath == null)
                {
                    _logger.LogWarning($"chips.json not found in any of the following paths: {string.Join(", ", possiblePaths)}");
                    return new List<Chip>();
                }
                
                var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
                return JsonSerializer.Deserialize<List<Chip>>(jsonContent) ?? new List<Chip>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chips from JSON in API");
                return new List<Chip>();
            }
        }

        private async Task SaveChipsToJson(List<Chip> chips)
        {
            try
            {
                // Try multiple possible paths for chips.json
                var possiblePaths = new[]
                {
                    Path.Combine(_environment.ContentRootPath, "chips.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "chips.json"),
                    "/app/chips.json"
                };

                string jsonPath = null;
                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        jsonPath = path;
                        break;
                    }
                }

                // If no existing file found, use the first path
                if (jsonPath == null)
                {
                    jsonPath = possiblePaths[0];
                }

                var jsonContent = JsonSerializer.Serialize(chips, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await System.IO.File.WriteAllTextAsync(jsonPath, jsonContent);
                _logger.LogInformation($"Successfully saved chips data to: {jsonPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chips to JSON in API");
            }
        }
    }

    public class UpdateChipStatusModel
    {
        public int ChipId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Command { get; set; }
    }

    public class BatchUpdateChipStatusModel
    {
        public List<UpdateChipStatusModel> Updates { get; set; } = new();
    }
}
