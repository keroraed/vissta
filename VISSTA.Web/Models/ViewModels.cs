using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VISSTA.Application.DTOs;
using VISSTA.Domain.Enums;

namespace VISSTA.Web.Models;

public sealed record HomeViewModel(IReadOnlyCollection<ProductListDto> FeaturedProducts, IReadOnlyCollection<ProductListDto> NewCollectionProducts, IReadOnlyCollection<CategoryDto> Categories);

public sealed record ShopViewModel(
    IReadOnlyCollection<ProductListDto> Products,
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Sort,
    string? Search);

public sealed record ProductDetailViewModel(ProductDetailDto Product, IReadOnlyCollection<ProductListDto> RelatedProducts);

public sealed record CollectionViewModel(
    IReadOnlyCollection<CategoryDto> AllCategories,
    CategoryDto? ActiveCategory,
    IReadOnlyCollection<ProductListDto> Products);


public sealed record CartViewModel(CartDto Cart);

public sealed class CheckoutViewModel
{
    [BindNever]
    public CartDto Cart { get; set; } = new(0, [], 0, "EGP", 0);
    [MaxLength(180)]
    public string? GuestName { get; set; }

    [EmailAddress]
    public string? GuestEmail { get; set; }

    [Phone, MaxLength(32)]
    public string? GuestPhone { get; set; }
    public ShippingAddressInput ShippingAddress { get; set; } = new();
    [BindNever]
    public ShippingAddressInput? SavedAddress { get; set; }
    public bool UseSavedAddress { get; set; }
    public string? CouponCode { get; set; }
    public string PaymentToken { get; set; } = "mock";
    public bool HasSavedAddress => SavedAddress?.IsComplete == true;
}

public sealed class ShippingAddressInput
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? Governorate { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; } = "Egypt";
    public bool IsComplete => !string.IsNullOrWhiteSpace(Street)
        && !string.IsNullOrWhiteSpace(City)
        && !string.IsNullOrWhiteSpace(Governorate)
        && !string.IsNullOrWhiteSpace(Country);
}

public sealed record OrderConfirmationViewModel(int OrderId, OrderDetailDto? Order);

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(240)]
    public string Street { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Governorate { get; set; } = string.Empty;

    [MaxLength(32)]
    public string PostalCode { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Country { get; set; } = "Egypt";
}

public sealed class ProfileViewModel
{
    [Required, MaxLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MaxLength(240)]
    public string Street { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Governorate { get; set; } = string.Empty;

    [MaxLength(32)]
    public string PostalCode { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Country { get; set; } = "Egypt";

    public bool HasAddress => !string.IsNullOrWhiteSpace(Street)
        && !string.IsNullOrWhiteSpace(City)
        && !string.IsNullOrWhiteSpace(Governorate)
        && !string.IsNullOrWhiteSpace(Country);
}

public sealed record AccountOrdersViewModel(IReadOnlyCollection<OrderSummaryDto> Orders);

public sealed record AdminDashboardViewModel(
    decimal Revenue,
    decimal GrossRevenue,
    decimal Discounts,
    decimal AverageOrderValue,
    int OrdersCount,
    int PendingOrdersCount,
    int LowStockCount,
    int ActiveCouponsCount,
    IReadOnlyCollection<OrderSummaryDto> RecentOrders,
    IReadOnlyCollection<ProductListDto> TopProducts,
    IReadOnlyCollection<ProductListDto> LowStockProducts,
    IReadOnlyCollection<ReviewDto> RecentReviews);

public sealed record AdminProductsViewModel(IReadOnlyCollection<ProductListDto> Products);

public sealed class AdminStockSettingsViewModel
{
    [Range(1, 999)]
    public int LowStockThreshold { get; set; } = 5;
}

public sealed class AdminProductFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyCollection<ProductImageDto> ExistingImages { get; set; } = Array.Empty<ProductImageDto>();
    public int[] RemoveImageIds { get; set; } = Array.Empty<int>();
    public IFormFile[]? ImageFiles { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int StockS { get; set; }
    public int StockM { get; set; }
    public int StockL { get; set; }
    public int StockXL { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int CategoryId { get; set; } = 2;
    public IReadOnlyCollection<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
}

public sealed record AdminOrdersViewModel(IReadOnlyCollection<OrderSummaryDto> Orders, OrderStatus? Status);

public sealed record AdminOrderDetailViewModel(OrderDetailDto Order);

public sealed record AdminCustomersViewModel(IReadOnlyCollection<ProfileViewModel> Customers);

public sealed record AdminReviewItemViewModel(ReviewDto Review, string CustomerEmail);

public sealed record AdminReviewsViewModel(IReadOnlyCollection<AdminReviewItemViewModel> Reviews);

public sealed record AdminCouponsViewModel(IReadOnlyCollection<CouponDto> Coupons);

public sealed record AdminCategoriesViewModel(IReadOnlyCollection<CategoryDto> Categories);

public sealed class AdminCategoryFormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
    public bool RemoveImage { get; set; }
    public IReadOnlyCollection<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
}

public sealed class AdminCouponFormViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    [Range(0.01, 100000)]
    public decimal Value { get; set; }

    [DataType(DataType.Date)]
    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.Date.AddMonths(1);

    [Range(1, 100000)]
    public int MaxUses { get; set; } = 100;

    public bool IsActive { get; set; } = true;
}

public sealed record NotificationViewModel(string? Message, string Type = "info");

// ─── OTP Password Reset ViewModels ───────────────────────────────────────────

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ForgotPasswordConfirmationViewModel
{
    public string Email { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
}

public sealed class VerifyOtpViewModel
{
    public string Email { get; set; } = string.Empty;   // hidden field

    [Required, StringLength(6, MinimumLength = 6), RegularExpression("^[0-9]{6}$", ErrorMessage = "Enter exactly 6 digits.")]
    public string Otp { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    public Guid OtpId { get; set; }   // hidden field
    public string Email { get; set; } = string.Empty;   // hidden field

    [Required, MinLength(8), DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$", 
        ErrorMessage = "Password must contain uppercase, lowercase, a number, and a special character.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
