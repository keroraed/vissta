using VISSTA.Domain.Common;
using VISSTA.Domain.Events;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Domain.Entities;

public sealed class Product : Entity, IAggregateRoot
{
    private readonly List<ProductImage> _images = [];
    private readonly List<Review> _reviews = [];

    private readonly List<ProductSizeStock> _sizeStocks = [];

    private Product()
    {
        Name = string.Empty;
        Slug = string.Empty;
        Description = string.Empty;
        SKU = string.Empty;
        Price = Money.Zero();
    }

    public Product(string name, string slug, string description, Money price, int stock, string sku, int categoryId, bool isFeatured = false)
    {
        Name = name;
        Slug = slug;
        Description = description;
        Price = price;
        SKU = sku;
        CategoryId = categoryId;
        IsActive = true;
        IsFeatured = isFeatured;
        Stock = stock;
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int Stock { get; private set; }
    public string SKU { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool ShowOnHomePage { get; private set; }
    /// <summary>"Percentage" or "Fixed"</summary>
    public string? DiscountType { get; private set; }
    /// <summary>Discount value — percent (0–100) when type is Percentage, absolute amount when Fixed</summary>
    public decimal? DiscountValue { get; private set; }
    public int UnitsSold { get; private set; }
    public int CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();
    public IReadOnlyCollection<ProductSizeStock> SizeStocks => _sizeStocks.AsReadOnly();

    // Computed helpers (not persisted)
    public bool HasDiscount => DiscountValue is > 0;
    public decimal EffectivePrice => HasDiscount
        ? DiscountType == "Fixed"
            ? Math.Max(0, Math.Round(Price.Amount - DiscountValue!.Value, 2))
            : Math.Round(Price.Amount * (1 - DiscountValue!.Value / 100m), 2)
        : Price.Amount;
    public decimal SavedAmount => Price.Amount - EffectivePrice;

    public void Update(string name, string slug, string description, Money price, int categoryId, bool isActive, bool isFeatured, bool showOnHomePage, string? discountType = null, decimal? discountValue = null)
    {
        Name = name;
        Slug = slug;
        Description = description;
        Price = price;
        CategoryId = categoryId;
        IsActive = isActive;
        IsFeatured = isFeatured;
        ShowOnHomePage = showOnHomePage;
        SetDiscount(discountType, discountValue);
    }

    public void SetDiscount(string? discountType, decimal? discountValue)
    {
        if (discountValue is null or 0)
        {
            DiscountType = null;
            DiscountValue = null;
            return;
        }

        if (discountType == "Percentage" && discountValue is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(discountValue), "Percentage discount must be between 0 and 100.");
        }
        if (discountType == "Fixed" && discountValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountValue), "Fixed discount cannot be negative.");
        }

        DiscountType = discountType;
        DiscountValue = discountValue;
    }

    public void SetShowOnHomePage(bool value)
    {
        ShowOnHomePage = value;
    }

    public void AddSizeStock(int sizeId, int stock, bool isAvailable)
    {
        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock));
        }
        var sizeStock = new ProductSizeStock(Id, sizeId, stock, isAvailable);
        _sizeStocks.Add(sizeStock);
        UpdateTotalStock();
    }

    public void UpdateSizeStocks(IEnumerable<(int SizeId, int Stock, bool IsAvailable)> stocks)
    {
        foreach (var item in stocks)
        {
            var existing = _sizeStocks.FirstOrDefault(x => x.SizeId == item.SizeId);
            if (existing is not null)
            {
                existing.Update(item.Stock, item.IsAvailable);
            }
            else
            {
                _sizeStocks.Add(new ProductSizeStock(Id, item.SizeId, item.Stock, item.IsAvailable));
            }
        }
        UpdateTotalStock();
    }

    public void UpdateTotalStock()
    {
        Stock = _sizeStocks.Where(x => x.IsAvailable).Sum(x => x.Stock);
    }

    public int GetStockForSize(string size)
    {
        var normalized = NormalizeSize(size);
        return _sizeStocks.FirstOrDefault(x => x.Size != null && x.Size.Name.ToUpperInvariant() == normalized)?.Stock ?? 0;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void RecordSale(int quantity, string size)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        var normalized = NormalizeSize(size);
        var sizeStock = _sizeStocks.FirstOrDefault(x => x.Size != null && x.Size.Name.ToUpperInvariant() == normalized);
        if (sizeStock is null || !sizeStock.IsAvailable || sizeStock.Stock < quantity)
        {
            throw new InvalidOperationException("Insufficient stock or size not available.");
        }

        sizeStock.Update(sizeStock.Stock - quantity, sizeStock.IsAvailable);
        Stock -= quantity;
        UnitsSold += quantity;

        if (Stock == 0)
        {
            AddDomainEvent(new ProductStockDepletedEvent(Id, SKU));
        }
    }

    public static string NormalizeSize(string size)
    {
        return size.Trim().ToUpperInvariant();
    }

    public void ReplaceImages(IEnumerable<string> urls)
    {
        _images.Clear();
        var index = 0;
        foreach (var url in urls)
        {
            var trimmed = url.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            _images.Add(new ProductImage(Id, trimmed, index == 0, index));
            index++;
        }
    }
}
