using VISSTA.Domain.Common;

namespace VISSTA.Domain.Entities;

public sealed class Cart : Entity, IAggregateRoot
{
    private readonly List<CartItem> _cartItems = [];

    private Cart()
    {
        SessionId = string.Empty;
    }

    public Cart(string? customerId, string? sessionId)
    {
        CustomerId = customerId;
        SessionId = sessionId;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public string? SessionId { get; private set; }
    public IReadOnlyCollection<CartItem> CartItems => _cartItems.AsReadOnly();
    public DateTime CreatedAt { get; private set; }

    public void AddItem(int productId, int quantity)
    {
        var item = _cartItems.FirstOrDefault(x => x.ProductId == productId);
        if (item is null)
        {
            _cartItems.Add(new CartItem(productId, quantity));
            return;
        }

        item.UpdateQuantity(item.Quantity + quantity);
    }

    public void RemoveItem(int cartItemId)
    {
        var item = _cartItems.FirstOrDefault(x => x.Id == cartItemId);
        if (item is not null)
        {
            _cartItems.Remove(item);
        }
    }

    public void Clear() => _cartItems.Clear();
}
