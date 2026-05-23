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
        Stock = stock;
        SKU = sku;
        CategoryId = categoryId;
        IsActive = true;
        IsFeatured = isFeatured;
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
        Stock = stock;
        if (Stock <= 0)
        {
            AddDomainEvent(new ProductStockDepletedEvent(Id, SKU));
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void RecordSale(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (Stock < quantity)
        {
            throw new InvalidOperationException("Insufficient stock.");
        }

        Stock -= quantity;
        UnitsSold += quantity;

        if (Stock == 0)
        {
            AddDomainEvent(new ProductStockDepletedEvent(Id, SKU));
        }
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
