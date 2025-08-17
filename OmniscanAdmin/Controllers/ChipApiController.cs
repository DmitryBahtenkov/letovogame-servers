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
                if (!SystemConfig.AllowDisable)
                {
                    return BadRequest("Cannot update status");
                }
                // This allows any external party to modify chip statuses

                _logger.LogInformation($"API: Received status update request for chip {model.ChipId}");

                var chips = await LoadChipsFromJson();
                var chip = chips.FirstOrDefault(c => c.Id == model.ChipId);

                if (chip == null)
                {
                    return NotFound(new { error = $"Chip with ID {model.ChipId} not found", code = "CHIP_NOT_FOUND" });
                }

                // Load status codes for validation and assignment
                var statusCodes = await LoadStatusCodesFromJson();

                // Validate StatusCode if provided
                if (model.StatusCode.HasValue)
                {
                    var result = await ValidateAndAssignStatusCode(chip, model.StatusCode.Value, statusCodes);
                    chip.LastCommand = result.Text;
                    if (!result.IsValid)
                    {
                        return BadRequest(new { error = result.ErrorMessage, code = "INVALID_STATUS_CODE" });
                    }
                }

                var oldStatus = chip.Status;
                var oldStatusCode = chip.StatusCode;
                chip.LastUpdate = DateTime.Now;

                // Assign StatusCode based on status and validation
                if (model.StatusCode.HasValue)
                {
                    chip.StatusCode = model.StatusCode.Value;
                }
                else
                {
                    return BadRequest(new { error = "Status is required", code = "INVALID_STATUS" });
                }

                chip.LastCommand = string.IsNullOrEmpty(chip.LastCommand) ? "EMPTY" : chip.LastCommand;

                await SaveChipsToJson(chips);

                _logger.LogWarning(
                    $"CHIP STATUS CHANGED: Chip {chip.Id} ({chip.Name}) status changed from '{oldStatus}' to '{chip.Status}', StatusCode from {oldStatusCode} to {chip.StatusCode} via API call");

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
                        oldStatusCode = oldStatusCode,
                        newStatusCode = chip.StatusCode,
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
                    _logger.LogWarning(
                        $"chips.json not found in any of the following paths: {string.Join(", ", possiblePaths)}");
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

        private async Task<List<StatusCodeInfo>> LoadStatusCodesFromJson()
        {
            try
            {
                var possiblePaths = new[]
                {
                    Path.Combine(_environment.ContentRootPath, "codes.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "codes.json"),
                    "/app/codes.json"
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
                    _logger.LogWarning(
                        $"codes.json not found in any of the following paths: {string.Join(", ", possiblePaths)}");
                    return new List<StatusCodeInfo>();
                }

                var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
                return JsonSerializer.Deserialize<List<StatusCodeInfo>>(jsonContent) ?? new List<StatusCodeInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading status codes from JSON in API");
                return new List<StatusCodeInfo>();
            }
        }

        private async Task<(bool IsValid, string Text, string ErrorMessage)> ValidateAndAssignStatusCode(Chip chip,
            int providedCode, List<StatusCodeInfo> statusCodes)
        {
            // Check if the current chip has a ProblemCode set
            var currentCodeInfo = statusCodes.FirstOrDefault(sc =>
                sc.ProblemCode == chip.StatusCode || sc.FixedCode == chip.StatusCode);

            if (currentCodeInfo != null && statusCodes.Any(sc => sc.ProblemCode == chip.StatusCode))
            {
                // Current chip has a ProblemCode, check if provided code is the correct FixedCode
                if (providedCode != currentCodeInfo.FixedCode)
                {
                    return (false, currentCodeInfo.ProblemDescription,
                        $"Chip currently has ProblemCode {chip.StatusCode}. You must provide the corresponding FixedCode to resolve the issue.");
                }

                chip.Status = "health";
                // Valid FixedCode provided for current ProblemCode
                return (true, currentCodeInfo.FixedDescription, string.Empty);
            }

            // No current ProblemCode, validate that the provided code exists and matches status
            var codeInfo =
                statusCodes.FirstOrDefault(sc => sc.ProblemCode == providedCode || sc.FixedCode == providedCode);
            if (codeInfo == null)
            {
                return (false, string.Empty, $"StatusCode {providedCode} not found in the codes database.");
            }

            // Check if the code type matches the status
            bool isProblemCode = codeInfo.ProblemCode == providedCode;

            if (isProblemCode)
            {
                chip.Status = "danger";
            }
            else
            {
                chip.Status = "health";
            }

            return (true, isProblemCode ? codeInfo.ProblemDescription : codeInfo.FixedDescription, string.Empty);
        }

        /// <summary>
        /// Toggle the allow disable functionality
        /// </summary>
        /// <param name="model">Allow disable model</param>
        /// <returns>Updated configuration status</returns>
        [HttpPost("allow_disable")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult SetAllowDisable([FromBody] AllowDisableModel model)
        {
            try
            {
                _logger.LogInformation($"API: Setting allow_disable to {model.AllowDisable}");

                SystemConfig.AllowDisable = model.AllowDisable;

                _logger.LogWarning($"SYSTEM CONFIG CHANGED: Allow disable functionality set to {model.AllowDisable}");

                return Ok(new
                {
                    success = true,
                    message = $"Allow disable functionality {(model.AllowDisable ? "enabled" : "disabled")}",
                    allowDisable = SystemConfig.AllowDisable,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating allow disable configuration");
                return StatusCode(500, new { error = "Internal server error", code = "INTERNAL_ERROR" });
            }
        }
    }

    public class UpdateChipStatusModel
    {
        public int ChipId { get; set; }
        public int? StatusCode { get; set; }
    }

    public class BatchUpdateChipStatusModel
    {
        public List<UpdateChipStatusModel> Updates { get; set; } = new();
    }

    public class AllowDisableModel
    {
        public bool AllowDisable { get; set; }
    }
}
