using VISSTA.Domain.Common;

namespace VISSTA.Domain.Entities;

public sealed class WishlistItem : Entity
{
    private WishlistItem()
    {
        CustomerId = string.Empty;
    }

    public WishlistItem(string customerId, int productId)
    {
        CustomerId = customerId;
        ProductId = productId;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string CustomerId { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
