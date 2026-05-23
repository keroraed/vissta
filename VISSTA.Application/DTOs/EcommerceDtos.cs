namespace VISSTA.Application.DTOs;

public sealed record ProductImageDto(int Id, string Url, bool IsPrimary, int DisplayOrder);

public sealed record ProductListDto(
    int Id,
    string Name,
    string Slug,
    decimal Price,
    string Currency,
    string ImageUrl,
    string CategoryName,
    bool IsFeatured,
    int Stock);

public sealed record ProductDetailDto(
    int Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    string Currency,
    int Stock,
    string Sku,
    string CategoryName,
    IReadOnlyCollection<ProductImageDto> Images,
    IReadOnlyCollection<ReviewDto> Reviews);

public sealed record CategoryDto(int Id, string Name, string Slug, int? ParentCategoryId);

public sealed record CartItemDto(int Id, int ProductId, string ProductName, string Slug, string ImageUrl, decimal UnitPrice, string Currency, int Quantity, decimal LineTotal);

public sealed record CartDto(int Id, IReadOnlyCollection<CartItemDto> Items, decimal Subtotal, string Currency, int Count);

public sealed record OrderSummaryDto(int Id, string Status, decimal TotalAmount, string Currency, DateTime CreatedAt);

public sealed record OrderDetailDto(
    int Id,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    string Street,
    string City,
    string Governorate,
    string PostalCode,
    IReadOnlyCollection<OrderItemDto> Items);

public sealed record OrderItemDto(int ProductId, string ProductName, string ImageUrl, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record ReviewDto(int Id, string CustomerName, int Rating, string Body, DateTime CreatedAt);

public sealed record SearchSuggestionDto(int Id, string Name, string Slug, string ImageUrl, decimal Price);
