using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OmniscanLogs;

class Program
{
    private static readonly string LogsFilePath = "logs.txt";
    private static readonly int Port = 8001;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("OmniScan Logs Server starting on port 8001...");
        
        if (!File.Exists(LogsFilePath))
        {
            await InitializeLogsFile();
        }
        
        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        
        Console.WriteLine($"TCP Server listening on port {Port}");
        Console.WriteLine("Banner: OmniScan Logs System - omniscan logs monitoring active");
        
        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }
    
    private static async Task InitializeLogsFile()
    {
        var initialLogs = new[]
        {
            "NAME=proto-001;STATUS=health;COMMAND=system_startup_complete",
            "NAME=proto-002;STATUS=health;COMMAND=scanner_calibration_ok",
            "NAME=proto-003;STATUS=danger;COMMAND=unauthorized_access_attempt",
            "NAME=proto-004;STATUS=health;COMMAND=data_backup_completed",
            "NAME=proto-005;STATUS=danger;COMMAND=anomaly_detected_sector_7"
        };
        
        await File.WriteAllLinesAsync(LogsFilePath, initialLogs);
        Console.WriteLine($"Initialized {LogsFilePath} with sample data");
    }
    
    private static async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                await writer.WriteLineAsync("OmniScan Logs System v2.1 - omniscan logs monitoring active");
                await writer.WriteLineAsync("Commands: READ | WRITE:KEY=value;");
                
                string? command;
                while ((command = await reader.ReadLineAsync()) != null)
                {
                    command = command.Trim();
                    
                    if (command.Equals("READ", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleReadCommand(writer);
                    }
                    else if (command.StartsWith("WRITE:", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleWriteCommand(command, writer);
                    }
                    else
                    {
                        await writer.WriteLineAsync("ERROR: Unknown command. Use READ or WRITE:KEY=value;");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
    }
    
    private static async Task HandleReadCommand(StreamWriter writer)
    {
        try
        {
            if (File.Exists(LogsFilePath))
            {
                var logs = await File.ReadAllLinesAsync(LogsFilePath);
                await writer.WriteLineAsync($"LOGS_COUNT:{logs.Length}");
                foreach (var log in logs)
                {
                    await writer.WriteLineAsync(log);
                }
                await writer.WriteLineAsync("END_LOGS");
            }
            else
            {
                await writer.WriteLineAsync("ERROR: Logs file not found");
            }
        }
        catch (Exception ex)
        {
            await writer.WriteLineAsync($"ERROR: {ex.Message}");
        }
    }
    
    private static async Task HandleWriteCommand(string command, StreamWriter writer)
    {
        try
        {
            var data = command.Substring(6);
            
            if (string.IsNullOrWhiteSpace(data))
            {
                await writer.WriteLineAsync("ERROR: No data provided for WRITE command");
                return;
            }
            
            if (!data.Contains('=') || !data.EndsWith(';'))
            {
                await writer.WriteLineAsync("ERROR: Invalid format. Use WRITE:KEY=value;");
                return;
            }
            
            await File.AppendAllTextAsync(LogsFilePath, data.TrimEnd(';') + Environment.NewLine);
            await writer.WriteLineAsync("OK: Log entry added");
            
            Console.WriteLine($"New log entry: {data.TrimEnd(';')}");
        }
        catch (Exception ex)
        {
            await writer.WriteLineAsync($"ERROR: {ex.Message}");
        }
    }
}