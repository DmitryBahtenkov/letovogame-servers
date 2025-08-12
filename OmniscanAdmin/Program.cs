var builder = WebApplication.CreateBuilder(args);

// Configure URLs for Docker
builder.WebHost.UseUrls("http://0.0.0.0:8002");

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "OmniScan Chip Management API",
        Version = "v1",
        Description = "API for managing OmniScan medical chip status and monitoring"
    });
});

// Add session support for authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Add custom headers for nmap fingerprinting
app.Use(async (context, next) =>
{
    // Custom headers to identify this as chip monitoring system
    context.Response.Headers.Add("X-Service-Type", "chip-monitoring-system");
    context.Response.Headers.Add("X-Technology", "omniscan-monitoring");
    context.Response.Headers.Add("Server", "OmniScan-Chip-Controller/1.0");
    
    await next();
});

// Skip HTTPS redirection in production for Docker
// app.UseHttpsRedirection(); 

// Enable Swagger in all environments for cybersecurity education
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OmniScan API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ChipAuth}/{action=Login}/{id?}");

app.Run();