using VISSTA.Domain.Common;

namespace VISSTA.Domain.Entities;

public sealed class ProductSizeStock : Entity
{
    private ProductSizeStock()
    {
    }

    public ProductSizeStock(int productId, int sizeId, int stock, bool isAvailable)
    {
        ProductId = productId;
        SizeId = sizeId;
        Stock = stock;
        IsAvailable = isAvailable;
    }

    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public int SizeId { get; private set; }
    public Size? Size { get; private set; }
    public int Stock { get; private set; }
    public bool IsAvailable { get; private set; }

    public void Update(int stock, bool isAvailable)
    {
        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock));
        }
        Stock = stock;
        IsAvailable = isAvailable;
    }
}
