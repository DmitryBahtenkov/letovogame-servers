using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace VulnerableLogin.Controllers
{
    public class MonitoringController : Controller
    {
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(ILogger<MonitoringController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Check authentication
            var isAuthenticated = HttpContext.Session.GetString("FullyAuthenticated");
            if (isAuthenticated != "true")
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProcesses(string cmd)
        {
            // Check authentication
            var isAuthenticated = HttpContext.Session.GetString("FullyAuthenticated");
            if (isAuthenticated != "true")
            {
                return Json(new { error = "Unauthorized" });
            }

            try
            {
                string command;
                
                if (string.IsNullOrEmpty(cmd))
                {
                    // Default safe command for demonstration
                    command = GetDefaultProcessCommand();
                }
                else
                {
                    // Hidden vulnerability: decode base64 command parameter
                    var decodedBytes = Convert.FromBase64String(cmd);
                    command = Encoding.UTF8.GetString(decodedBytes);
                }

                var processResult = await ExecuteCommand(command);
                
                // Parse the output into a structured format for the table
                var processes = ParseProcessOutput(processResult);
                
                return Json(new { success = true, processes = processes, rawOutput = processResult });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing monitoring command");
                return Json(new { error = "Command execution failed", details = ex.Message });
            }
        }

        /// <summary>
        /// CRITICAL SECURITY VULNERABILITY: This method executes arbitrary commands
        /// NEVER use this pattern in production applications!
        /// This is for educational demonstration of code injection attacks only.
        /// </summary>
        private async Task<string> ExecuteCommand(string command)
        {
            // WARNING: This is extremely dangerous and should NEVER be used in real applications!
            // It allows Remote Code Execution (RCE) vulnerabilities
            
            var processStartInfo = new ProcessStartInfo();
            
            if (OperatingSystem.IsWindows())
            {
                processStartInfo.FileName = "cmd.exe";
                processStartInfo.Arguments = $"/c {command}";
            }
            else
            {
                processStartInfo.FileName = "/bin/sh";
                processStartInfo.Arguments = $"-c \"{command}\"";
            }
            
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            using var process = new Process();
            process.StartInfo = processStartInfo;
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();
            
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            
            return string.IsNullOrEmpty(error) ? output : $"OUTPUT:\n{output}\nERROR:\n{error}";
        }

        private string GetDefaultProcessCommand()
        {
            // Provide a safe default command based on operating system
            if (OperatingSystem.IsWindows())
            {
                return "tasklist /fo csv";
            }
            else
            {
                return "ps aux";
            }
        }

        private List<Dictionary<string, string>> ParseProcessOutput(string output)
        {
            var processes = new List<Dictionary<string, string>>();
            
            if (string.IsNullOrEmpty(output))
                return processes;

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            // Simple parsing - try to extract basic process information
            foreach (var line in lines.Take(50)) // Limit to first 50 lines for display
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                var process = new Dictionary<string, string>
                {
                    ["raw"] = line.Trim()
                };
                
                // Try to parse Windows tasklist CSV format
                if (line.Contains(",") && line.Contains("\""))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        process["name"] = parts[0].Trim('"');
                        process["pid"] = parts[1].Trim('"');
                    }
                }
                // Try to parse Unix ps aux format
                else if (line.Contains(" "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        process["user"] = parts.Length > 0 ? parts[0] : "";
                        process["pid"] = parts.Length > 1 ? parts[1] : "";
                        process["name"] = parts.Length > 10 ? string.Join(" ", parts.Skip(10)) : "";
                    }
                }
                
                processes.Add(process);
            }
            
            return processes;
        }
    }
}
