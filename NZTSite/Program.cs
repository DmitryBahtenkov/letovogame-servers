var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:8004");

var app = builder.Build();

app.UseStaticFiles();

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
        await context.Response.WriteAsync("NZT Landing Page not found");
    }
});

app.MapGet("/health", () => 
{
    return Results.Json(new { status = "cognitive_enhanced", service = "nzt-site", port = 8004, features = new[] { "nootropic", "enhancement" } });
});

app.MapGet("/cognitive-status", () =>
{
    return Results.Json(new { 
        cognitive_enhancement = "active", 
        iq_boost = "+300%",
        productivity = "maximum", 
        brain_power = "unlimited",
        status = "NZT-48 active"
    });
});

Console.WriteLine("NZT Cognitive Enhancement Server starting on http://localhost:8004");
Console.WriteLine("Service: Nootropic Enhancement Platform - NZT cognitive boosters active");

app.Run();