var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8003");

var app = builder.Build();

app.MapGet("/", async (HttpContext context) =>
{
    var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "index.html");
    if (File.Exists(htmlPath))
    {
        var content = await File.ReadAllTextAsync(htmlPath);
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(content);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("OmniScan Landing Page not found");
    }
});

app.MapGet("/health", () => 
{
    return Results.Json(new { status = "healthy", service = "omniscan-site", port = 8003, features = new[] { "monitoring", "chip" } });
});

Console.WriteLine("OmniScan Landing Server starting on http://localhost:8003");
Console.WriteLine("Service: Medical Chip Monitoring - omniscan chip landing active");

app.Run();