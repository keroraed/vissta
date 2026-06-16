using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VISSTA.Domain.Entities;
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

            // Seed initial sizes
            if (!await db.Sizes.AnyAsync())
            {
                var sizes = new List<Size>
                {
                    new("S", 1),
                    new("M", 2),
                    new("L", 3),
                    new("XL", 4),
                    new("2XL", 5),
                    new("3XL", 6)
                };
                await db.Sizes.AddRangeAsync(sizes);
                await db.SaveChangesAsync();
            }

            // Seed initial size stocks for products if they don't have any
            var defaultSizes = await db.Sizes.ToListAsync();
            var sizeS = defaultSizes.FirstOrDefault(x => x.Name == "S");
            var sizeM = defaultSizes.FirstOrDefault(x => x.Name == "M");
            var sizeL = defaultSizes.FirstOrDefault(x => x.Name == "L");
            var sizeXL = defaultSizes.FirstOrDefault(x => x.Name == "XL");
            var size2XL = defaultSizes.FirstOrDefault(x => x.Name == "2XL");
            var size3XL = defaultSizes.FirstOrDefault(x => x.Name == "3XL");

            if (sizeS != null && sizeM != null && sizeL != null && sizeXL != null && size2XL != null && size3XL != null)
            {
                var products = await db.Products.Include(p => p.SizeStocks).ToListAsync();
                foreach (var product in products)
                {
                    if (!product.SizeStocks.Any())
                    {
                        db.ProductSizeStocks.Add(new ProductSizeStock(product.Id, sizeS.Id, product.Stock, true));
                        db.ProductSizeStocks.Add(new ProductSizeStock(product.Id, sizeM.Id, 0, true));
                        db.ProductSizeStocks.Add(new ProductSizeStock(product.Id, sizeL.Id, 0, true));
                        db.ProductSizeStocks.Add(new ProductSizeStock(product.Id, sizeXL.Id, 0, true));
                        db.ProductSizeStocks.Add(new ProductSizeStock(product.Id, size2XL.Id, 0, false));
                        db.ProductSizeStocks.Add(new ProductSizeStock(product.Id, size3XL.Id, 0, false));
                    }
                }
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed.");
            throw;
        }
    }
}
