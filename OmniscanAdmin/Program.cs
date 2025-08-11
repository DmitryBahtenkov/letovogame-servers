var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ChipAuth}/{action=Login}/{id?}");

// Configure to run on port 8002
app.Urls.Add("http://localhost:8002");
app.Urls.Add("https://localhost:8003");

app.Run();