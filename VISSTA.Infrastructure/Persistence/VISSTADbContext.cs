using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VISSTA.Domain.Entities;
using VISSTA.Infrastructure.Identity;

namespace VISSTA.Infrastructure.Persistence;

public sealed class VISSTADbContext(DbContextOptions<VISSTADbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new PasswordResetOtpConfiguration());
        builder.ApplyConfiguration(new NewsletterSubscriberConfiguration());

        builder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(140).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasOne(x => x.ParentCategory).WithMany(x => x.Children).HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.Metadata.FindNavigation(nameof(Category.Children))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.Metadata.FindNavigation(nameof(Category.Products))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(180).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.SKU).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.SKU).IsUnique();
            entity.Property(x => x.Stock).IsRequired();
            entity.Property(x => x.StockS).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.StockM).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.StockL).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.StockXL).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.IsFeatured).HasDefaultValue(false);
            entity.Property(x => x.ShowOnHomePage).HasDefaultValue(false);
            entity.Property(x => x.DiscountType).HasMaxLength(20);
            entity.Property(x => x.DiscountValue).HasColumnType("decimal(18,2)");
            entity.OwnsOne(x => x.Price, money =>
            {
                money.Property(x => x.Amount).HasColumnName("PriceAmount").HasPrecision(18, 2);
                money.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).HasDefaultValue("EGP");
            });
            entity.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId);
            entity.Metadata.FindNavigation(nameof(Product.Images))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.Metadata.FindNavigation(nameof(Product.Reviews))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Url).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(32);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.OwnsOne(x => x.DefaultAddress, address =>
            {
                address.Property(x => x.Street).HasMaxLength(240);
                address.Property(x => x.City).HasMaxLength(120);
                address.Property(x => x.Governorate).HasMaxLength(120);
                address.Property(x => x.PostalCode).HasMaxLength(32);
                address.Property(x => x.Country).HasMaxLength(80);
            });
            entity.Metadata.FindNavigation(nameof(Customer.Orders))?.SetPropertyAccessMode(PropertyAccessMode.Field);
            entity.Metadata.FindNavigation(nameof(Customer.Reviews))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.OwnsOne(x => x.ShippingAddress, address =>
            {
                address.Property(x => x.Street).HasMaxLength(240).IsRequired();
                address.Property(x => x.City).HasMaxLength(120).IsRequired();
                address.Property(x => x.Governorate).HasMaxLength(120).IsRequired();
                address.Property(x => x.PostalCode).HasMaxLength(32);
                address.Property(x => x.Country).HasMaxLength(80).IsRequired();
            });
            entity.OwnsOne(x => x.TotalAmount, money =>
            {
                money.Property(x => x.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
                money.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            entity.OwnsOne(x => x.SubtotalAmount, money =>
            {
                money.Property(x => x.Amount).HasColumnName("SubtotalAmount").HasPrecision(18, 2);
                money.Property(x => x.Currency).HasColumnName("SubtotalCurrency").HasMaxLength(3);
            });
            entity.OwnsOne(x => x.DiscountAmount, money =>
            {
                money.Property(x => x.Amount).HasColumnName("DiscountAmount").HasPrecision(18, 2);
                money.Property(x => x.Currency).HasColumnName("DiscountCurrency").HasMaxLength(3);
            });
            entity.Property(x => x.CouponCode).HasMaxLength(40);
            entity.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.Metadata.FindNavigation(nameof(Order.OrderItems))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Size).HasMaxLength(8).IsRequired().HasDefaultValue("M");
            entity.OwnsOne(x => x.UnitPrice, money =>
            {
                money.Property(x => x.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2);
                money.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            entity.HasOne(x => x.Order).WithMany(x => x.OrderItems).HasForeignKey(x => x.OrderId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerId).HasMaxLength(450);
            entity.Property(x => x.SessionId).HasMaxLength(120);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.Metadata.FindNavigation(nameof(Cart.CartItems))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Size).HasMaxLength(8).IsRequired().HasDefaultValue("M");
            entity.HasOne(x => x.Cart).WithMany(x => x.CartItems).HasForeignKey(x => x.CartId);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Review>(entity =>
        {
            entity.ToTable("Reviews");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Body).HasMaxLength(1500).IsRequired();
            entity.HasOne(x => x.Product).WithMany(x => x.Reviews).HasForeignKey(x => x.ProductId);
            entity.HasOne(x => x.Customer).WithMany(x => x.Reviews).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Coupon>(entity =>
        {
            entity.ToTable("Coupons");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Value).HasPrecision(18, 2);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });



        builder.Entity<WishlistItem>(entity =>
        {
            entity.ToTable("WishlistItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerId).HasMaxLength(450).IsRequired();
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AppSetting>(entity =>
        {
            entity.ToTable("AppSettings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(400).IsRequired();
            entity.HasIndex(x => x.Key).IsUnique();
        });

        SeedCatalog(builder);
    }

    private static void SeedCatalog(ModelBuilder builder)
    {
        builder.Entity<Category>().HasData(
            new { Id = 2, Name = "Men's", Slug = "men", ParentCategoryId = (int?)null, ImageUrl = (string?)null },
            new { Id = 4, Name = "T-shirt", Slug = "t-shirt", ParentCategoryId = (int?)2, ImageUrl = (string?)null },
            new { Id = 5, Name = "Shirts", Slug = "shirts", ParentCategoryId = (int?)2, ImageUrl = (string?)null },
            new { Id = 6, Name = "Bottoms", Slug = "bottoms", ParentCategoryId = (int?)2, ImageUrl = (string?)null });

        var products = new[]
        {
            // T-shirt (CategoryId = 4)
            new { Id = 1,  Name = "Textured Knit Polo",    Slug = "textured-knit-polo",    Description = "A breathable cream knit polo with understated structure.",      Stock = 34, SKU = "VIS-MEN-POLO-001",  CategoryId = 4, IsActive = true,  IsFeatured = true,  UnitsSold = 46, PriceAmount = 750m,  Currency = "EGP" },
            new { Id = 2,  Name = "Ribbed Grey Polo",       Slug = "ribbed-grey-polo",       Description = "A soft ribbed knit in a refined graphite tone.",               Stock = 28, SKU = "VIS-MEN-POLO-002",  CategoryId = 4, IsActive = true,  IsFeatured = true,  UnitsSold = 39, PriceAmount = 700m,  Currency = "EGP" },
            new { Id = 3,  Name = "Modern Knit Polo",       Slug = "modern-knit-polo",       Description = "Clean lines, open collar, and an easy old-money drape.",       Stock = 41, SKU = "VIS-MEN-POLO-003",  CategoryId = 4, IsActive = true,  IsFeatured = true,  UnitsSold = 44, PriceAmount = 700m,  Currency = "EGP" },
            new { Id = 4,  Name = "Contrast Knit Tee",      Slug = "contrast-knit-tee",      Description = "A dark elevated tee with quiet contrast texture.",             Stock = 22, SKU = "VIS-MEN-TEE-001",   CategoryId = 4, IsActive = true,  IsFeatured = false, UnitsSold = 23, PriceAmount = 650m,  Currency = "EGP" },
            new { Id = 7,  Name = "Pearl Knit Top",         Slug = "pearl-knit-top",         Description = "Soft pearl-toned knitwear with a sculpted neckline.",          Stock = 30, SKU = "VIS-MEN-TOP-001",   CategoryId = 4, IsActive = true,  IsFeatured = false, UnitsSold = 32, PriceAmount = 720m,  Currency = "EGP" },
            new { Id = 12, Name = "Heritage Tote",          Slug = "heritage-tote",          Description = "Structured canvas with navy trim and daily utility.",           Stock = 12, SKU = "VIS-MEN-TOTE-001",  CategoryId = 4, IsActive = true,  IsFeatured = false, UnitsSold = 15, PriceAmount = 1250m, Currency = "EGP" },
            // Shirts (CategoryId = 5)
            new { Id = 5,  Name = "Ivory Resort Shirt",     Slug = "ivory-resort-shirt",     Description = "A relaxed summer shirt for sharp, unforced dressing.",         Stock = 18, SKU = "VIS-MEN-SHIRT-001", CategoryId = 5, IsActive = true,  IsFeatured = false, UnitsSold = 18, PriceAmount = 880m,  Currency = "EGP" },
            new { Id = 8,  Name = "Champagne Linen Shirt",  Slug = "champagne-linen-shirt",  Description = "Light linen with a softened golden hue.",                      Stock = 24, SKU = "VIS-MEN-SHIRT-002", CategoryId = 5, IsActive = true,  IsFeatured = false, UnitsSold = 27, PriceAmount = 890m,  Currency = "EGP" },
            new { Id = 11, Name = "Silk Pocket Square",     Slug = "silk-pocket-square",     Description = "A small flash of cream silk for considered outfits.",          Stock = 60, SKU = "VIS-MEN-SILK-001",  CategoryId = 5, IsActive = true,  IsFeatured = false, UnitsSold = 17, PriceAmount = 320m,  Currency = "EGP" },
            // Bottoms (CategoryId = 6)
            new { Id = 6,  Name = "Navy Tailored Trouser",  Slug = "navy-tailored-trouser",  Description = "A minimal trouser cut for movement and polish.",               Stock = 16, SKU = "VIS-MEN-PANT-001",  CategoryId = 6, IsActive = true,  IsFeatured = false, UnitsSold = 14, PriceAmount = 1050m, Currency = "EGP" },
            new { Id = 9,  Name = "Cream Tailored Trouser", Slug = "cream-tailored-trouser", Description = "A clean tapered cut in a refined cream tone.",                 Stock = 14, SKU = "VIS-MEN-PANT-002",  CategoryId = 6, IsActive = true,  IsFeatured = false, UnitsSold = 12, PriceAmount = 980m,  Currency = "EGP" },
            new { Id = 10, Name = "Gold Edge Belt",         Slug = "gold-edge-belt",         Description = "Smooth leather, restrained hardware, precise finish.",         Stock = 50, SKU = "VIS-MEN-BELT-001",  CategoryId = 6, IsActive = true,  IsFeatured = false, UnitsSold = 21, PriceAmount = 540m,  Currency = "EGP" },
        };


        builder.Entity<Product>().HasData(products.Select(x => new
        {
            x.Id,
            x.Name,
            x.Slug,
            x.Description,
            x.Stock,
            StockS = x.Stock,
            StockM = 0,
            StockL = 0,
            StockXL = 0,
            x.SKU,
            x.CategoryId,
            x.IsActive,
            x.IsFeatured,
            x.UnitsSold
        }));
        builder.Entity<Product>().OwnsOne(x => x.Price).HasData(products.Select(x => new { ProductId = x.Id, Amount = x.PriceAmount, x.Currency }));

        var images = new[]
        {
            new { Id = 1, ProductId = 1, Url = "/assets/product-white-polo.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 2, ProductId = 2, Url = "/assets/product-grey-polo.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 3, ProductId = 3, Url = "/assets/product-knit-polo.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 4, ProductId = 4, Url = "/assets/product-black-tee.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 5, ProductId = 5, Url = "/assets/knit-white.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 6, ProductId = 6, Url = "/assets/folded-clothes.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 7, ProductId = 7, Url = "/assets/knit-white.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 8, ProductId = 8, Url = "/assets/premium-folded.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 9, ProductId = 9, Url = "/assets/folded-clothes.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 10, ProductId = 10, Url = "/assets/knit-grey.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 11, ProductId = 11, Url = "/assets/premium-folded.webp", IsPrimary = true, DisplayOrder = 1 },
            new { Id = 12, ProductId = 12, Url = "/assets/folded-clothes.webp", IsPrimary = true, DisplayOrder = 1 }
        };

        builder.Entity<ProductImage>().HasData(images);
    }
}
