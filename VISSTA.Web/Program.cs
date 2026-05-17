using Microsoft.AspNetCore.RateLimiting;
using VISSTA.Application.Common;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Common;
using VISSTA.Infrastructure;
using VISSTA.Infrastructure.Persistence;
using VISSTA.Web.Middleware;
using VISSTA.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".VISSTA.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();
builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.MinifyCssFiles("css/site.css", "css/admin.css");
    pipeline.MinifyJsFiles("js/cart.js", "js/search.js", "js/animations.js", "js/site.js");
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

builder.Services.AddDomainServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseWebOptimizer();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseResponseCaching();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Configuration.GetValue("Database:InitializeOnStartup", false))
{
    await DbInitializer.InitializeAsync(app.Services);
}

app.Run();
