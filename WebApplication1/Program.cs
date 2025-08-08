using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// GET / - получение линий из файла
app.MapGet("/", async () =>
{
    const string filePath = "logs.jsonl";
    
    if (!File.Exists(filePath))
    {
        return Results.NotFound("Файл не найден");
    }
    
    try
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        return Results.Ok(lines);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ошибка чтения файла: {ex.Message}");
    }
});

// POST /log - создание записи в логе с произвольными параметрами
app.MapPost("/log", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        
        // Создаем метаинформацию о запросе
        var requestMeta = new
        {
            path = request.Path.Value ?? string.Empty,
            method = request.Method,
            content = body
        };
        
        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };
        
        var requestMetaJson = JsonSerializer.Serialize(requestMeta, jsonOptions);
        var requestMetaBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(requestMetaJson));
        
        var logEntry = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow.ToString("O"),
            ["requestMeta"] = requestMetaBase64,
            ["data"] = body.Length > 0 ? JsonSerializer.Deserialize<object>(body) : new { }
        };
        
        const string logFilePath = "logs.jsonl";
        var logLine = JsonSerializer.Serialize(logEntry, jsonOptions);
        await File.AppendAllTextAsync(logFilePath, logLine + Environment.NewLine);
        
        return Results.Ok(new { message = "Log write successfully", timestamp = logEntry["timestamp"] });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ошибка записи лога: {ex.Message}");
    }
});

app.Run();
