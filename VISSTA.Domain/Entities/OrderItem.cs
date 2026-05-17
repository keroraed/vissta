using VISSTA.Domain.Common;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Domain.Entities;

public sealed class OrderItem : Entity
{
    private OrderItem()
    {
        UnitPrice = Money.Zero();
    }

    public OrderItem(int productId, int quantity, Money unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public Order? Order { get; private set; }
    public int ProductId { get; private set; }
    public Product? Product { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
}
