using VISSTA.Domain.Common;
using VISSTA.Domain.Events;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Domain.Entities;

public sealed class Product : Entity, IAggregateRoot
{
    private readonly List<ProductImage> _images = [];
    private readonly List<Review> _reviews = [];

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
        SetSizeStocks(stock, 0, 0, 0);
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int Stock { get; private set; }
    public int StockS { get; private set; }
    public int StockM { get; private set; }
    public int StockL { get; private set; }
    public int StockXL { get; private set; }
    public string SKU { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public int UnitsSold { get; private set; }
    public int CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

    public void Update(string name, string slug, string description, Money price, int categoryId, bool isActive, bool isFeatured)
    {
        Name = name;
        Slug = slug;
        Description = description;
        Price = price;
        CategoryId = categoryId;
        IsActive = isActive;
        IsFeatured = isFeatured;
    }

    public void UpdateStock(int stock)
    {
        SetSizeStocks(stock, 0, 0, 0);
    }

    public void SetSizeStocks(int stockS, int stockM, int stockL, int stockXL)
    {
        if (stockS < 0 || stockM < 0 || stockL < 0 || stockXL < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockS));
        }

        StockS = stockS;
        StockM = stockM;
        StockL = stockL;
        StockXL = stockXL;
        Stock = stockS + stockM + stockL + stockXL;
        if (Stock <= 0)
        {
            AddDomainEvent(new ProductStockDepletedEvent(Id, SKU));
        }
    }

    public int GetStockForSize(string size) => NormalizeSize(size) switch
    {
        "S" => StockS,
        "M" => StockM,
        "L" => StockL,
        "XL" => StockXL,
        _ => 0
    };

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

        var normalizedSize = NormalizeSize(size);
        if (GetStockForSize(normalizedSize) < quantity)
        {
            throw new InvalidOperationException("Insufficient stock.");
        }

        switch (normalizedSize)
        {
            case "S":
                StockS -= quantity;
                break;
            case "M":
                StockM -= quantity;
                break;
            case "L":
                StockL -= quantity;
                break;
            case "XL":
                StockXL -= quantity;
                break;
            default:
                throw new InvalidOperationException("Invalid size.");
        }

        Stock -= quantity;
        UnitsSold += quantity;

        if (Stock == 0)
        {
            AddDomainEvent(new ProductStockDepletedEvent(Id, SKU));
        }
    }

    public static string NormalizeSize(string size)
    {
        var normalized = size.Trim().ToUpperInvariant();
        return normalized is "S" or "M" or "L" or "XL" ? normalized : throw new ArgumentOutOfRangeException(nameof(size));
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
