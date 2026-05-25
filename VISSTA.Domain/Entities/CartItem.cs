using VISSTA.Domain.Common;

namespace VISSTA.Domain.Entities;

public sealed class CartItem : Entity
{
    private CartItem()
    {
        Size = string.Empty;
    }

    public CartItem(int productId, string size, int quantity)
    {
        ProductId = productId;
        Size = Product.NormalizeSize(size);
        UpdateQuantity(quantity);
    }

    public int Id { get; private set; }
    public int CartId { get; private set; }
    public Cart? Cart { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public string Size { get; private set; }
    public int Quantity { get; private set; }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        Quantity = quantity;
    }
}
