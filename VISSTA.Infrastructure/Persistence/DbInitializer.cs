using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VISSTA.Infrastructure.Identity;

namespace VISSTA.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("VISSTA.DbInitializer");

        try
        {
            var db = provider.GetRequiredService<VISSTADbContext>();
            await db.Database.MigrateAsync();

            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { "Admin", "Customer" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await userManager.FindByEmailAsync("admin@vissta.com");
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin@vissta.com",
                    Email = "admin@vissta.com",
                    EmailConfirmed = true,
                    FullName = "VISSTA Admin"
                };

                var createResult = await userManager.CreateAsync(admin, "Admin@123!");
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to seed admin user: {string.Join(", ", createResult.Errors.Select(x => x.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var roleResult = await userManager.AddToRoleAsync(admin, "Admin");
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to assign Admin role: {string.Join(", ", roleResult.Errors.Select(x => x.Description))}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed.");
            throw;
        }
    }
}
