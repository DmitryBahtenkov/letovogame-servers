
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCP
{
    class Program
    {
        private static readonly int Port = 8080;
        private static readonly string FilePath = "data.txt";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("TCP File Server starting...");
            Console.WriteLine($"Port: {Port}");
            Console.WriteLine($"File: {FilePath}");
            
            if (!File.Exists(FilePath))
            {
                Console.WriteLine($"File {FilePath} not found. Creating sample file...");
                await CreateSampleFile();
            }
            
            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            
            Console.WriteLine($"Server listening on port {Port}. Press Ctrl+C to stop.");
            
            while (true)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
                    
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }
        
        private static async Task HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                
                Console.WriteLine("Waiting for command...");
                
                string command = await reader.ReadLineAsync();
                if (command == null)
                {
                    Console.WriteLine("Client sent empty command");
                    return;
                }
                
                Console.WriteLine($"Received command: {command}");
                
                if (command.Equals("READ", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleReadCommand(stream);
                }
                else if (command.StartsWith("write:", StringComparison.OrdinalIgnoreCase))
                {
                    string content = command.Substring(6); // Remove "write:" prefix
                    await HandleWriteCommand(stream, content);
                }
                else
                {
                    await SendResponse(stream, "ERROR: Unknown command. Use 'READ' or 'WRITE:your_text'\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
        
        private static async Task HandleReadCommand(NetworkStream stream)
        {
            Console.WriteLine("Processing READ command...");
            
            if (!File.Exists(FilePath))
            {
                await SendResponse(stream, "ERROR: File not found\n");
                return;
            }
            
            await SendResponse(stream, "OK: Reading file content\n");
            
            using (StreamReader fileReader = new StreamReader(FilePath, Encoding.UTF8))
            {
                string line;
                int lineNumber = 1;
                
                while ((line = await fileReader.ReadLineAsync()) != null)
                {
                    string message = $"Line {lineNumber}: {line}\n";
                    await SendResponse(stream, message);
                    Console.WriteLine($"Sent line {lineNumber}: {line}");
                    
                    lineNumber++;
                    await Task.Delay(500);
                }
                
                await SendResponse(stream, "--- END OF FILE ---\n");
                Console.WriteLine("File read completed.");
            }
        }
        
        private static async Task HandleWriteCommand(NetworkStream stream, string content)
        {
            Console.WriteLine($"Processing WRITE command with content: {content}");
            
            try
            {
                await File.AppendAllTextAsync(FilePath, content + Environment.NewLine, Encoding.UTF8);
                await SendResponse(stream, $"OK: Written '{content}' to file\n");
                Console.WriteLine($"Successfully wrote: {content}");
            }
            catch (Exception ex)
            {
                await SendResponse(stream, $"ERROR: Failed to write to file - {ex.Message}\n");
                Console.WriteLine($"Failed to write: {ex.Message}");
            }
        }
        
        private static async Task SendResponse(NetworkStream stream, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }
        
        private static async Task CreateSampleFile()
        {
            string[] sampleLines = {
                "Это первая строка файла",
                "Вторая строка содержит важную информацию",
                "Третья строка для тестирования TCP передачи",
                "Четвертая строка с секретными данными: password123",
                "Пятая строка завершает наш тестовый файл"
            };
            
            await File.WriteAllLinesAsync(FilePath, sampleLines, Encoding.UTF8);
            Console.WriteLine($"Sample file '{FilePath}' created.");
        }
    }
}
