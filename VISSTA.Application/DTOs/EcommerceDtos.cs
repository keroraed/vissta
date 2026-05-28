namespace VISSTA.Application.DTOs;

public sealed record ProductImageDto(int Id, string Url, bool IsPrimary, int DisplayOrder);

public sealed record ProductSizeStockDto(string Size, int Stock);

public sealed record ProductListDto(
    int Id,
    string Name,
    string Slug,
    decimal Price,
    decimal EffectivePrice,
    string Currency,
    string ImageUrl,
    string CategoryName,
    bool IsFeatured,
    bool ShowOnHomePage,
    string? DiscountType,
    decimal? DiscountValue,
    int Stock,
    bool IsActive);

public sealed record ProductDetailDto(
    int Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal EffectivePrice,
    decimal SavedAmount,
    string? DiscountType,
    decimal? DiscountValue,
    string Currency,
    int Stock,
    string Sku,
    int CategoryId,
    string CategoryName,
    bool IsActive,
    bool IsFeatured,
    bool ShowOnHomePage,
    IReadOnlyCollection<ProductSizeStockDto> SizeStocks,
    IReadOnlyCollection<ProductImageDto> Images,
    IReadOnlyCollection<ReviewDto> Reviews);

public sealed record CategoryDto(int Id, string Name, string Slug, int? ParentCategoryId, string? ImageUrl = null);

public sealed record CartItemDto(int Id, int ProductId, string ProductName, string Slug, string ImageUrl, decimal UnitPrice, string Currency, int Quantity, decimal LineTotal, string Size = "");

public sealed record CartDto(int Id, IReadOnlyCollection<CartItemDto> Items, decimal Subtotal, string Currency, int Count);

public sealed record OrderSummaryDto(
    int Id,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    decimal DiscountAmount = 0,
    string? CouponCode = null,
    string CustomerName = "VISSTA Customer",
    string CustomerPhone = "",
    int ItemCount = 0);

public sealed record OrderDetailDto(
    int Id,
    string Status,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    string Street,
    string City,
    string Governorate,
    string PostalCode,
    string? CouponCode,
    IReadOnlyCollection<OrderItemDto> Items,
    string CustomerId = "",
    string CustomerName = "VISSTA Customer",
    string CustomerPhone = "",
    string Country = "");

public sealed record OrderItemDto(int ProductId, string ProductName, string ImageUrl, int Quantity, decimal UnitPrice, decimal LineTotal, string Size = "");

public sealed record CouponDto(
    int Id,
    string Code,
    string DiscountType,
    decimal Value,
    DateTime ExpiryDate,
    int MaxUses,
    int UsedCount,
    bool IsActive,
    bool IsValid);

public sealed record ReviewDto(
    int Id,
    string CustomerName,
    int Rating,
    string Body,
    DateTime CreatedAt,
    int ProductId = 0,
    string ProductName = "",
    string ProductSlug = "",
    bool IsApproved = false,
    string CustomerId = "",
    string CustomerPhone = "");

public sealed record SearchSuggestionDto(int Id, string Name, string Slug, string ImageUrl, decimal Price);
