using VISSTA.Domain.Common;
using VISSTA.Domain.Enums;
using VISSTA.Domain.Events;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Domain.Entities;

public sealed class Order : Entity, IAggregateRoot
{
    private readonly List<OrderItem> _orderItems = [];

    private Order()
    {
        CustomerId = string.Empty;
        ShippingAddress = new Address(string.Empty, string.Empty, string.Empty, string.Empty);
        TotalAmount = Money.Zero();
        SubtotalAmount = Money.Zero();
        DiscountAmount = Money.Zero();
        PaymentMethod = PaymentMethod.CashOnDelivery;
    }

    public Order(string customerId, Address shippingAddress)
    {
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        TotalAmount = Money.Zero();
        SubtotalAmount = Money.Zero();
        DiscountAmount = Money.Zero();
        CreatedAt = DateTime.UtcNow;
        PaymentMethod = PaymentMethod.CashOnDelivery;
    }

    public int Id { get; private set; }
    public string CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public Address ShippingAddress { get; private set; }
    public Money SubtotalAmount { get; private set; }
    public Money DiscountAmount { get; private set; }
    public Money TotalAmount { get; private set; }
    public string? CouponCode { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? PaymentProofUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void AddItem(int productId, string size, int quantity, Money unitPrice)
    {
        _orderItems.Add(new OrderItem(productId, size, quantity, unitPrice));
        RecalculateTotal();
    }

    public void MarkPlaced()
    {
        AddDomainEvent(new OrderPlacedEvent(Id, CustomerId, TotalAmount.Amount));
    }

    public void ChangeStatus(OrderStatus status)
    {
        if (Status == status)
        {
            return;
        }

        var previous = Status;
        Status = status;
        AddDomainEvent(new OrderStatusChangedEvent(Id, previous, status));
    }

    public void RecalculateTotal()
    {
        SubtotalAmount = _orderItems
            .Select(x => x.UnitPrice.Multiply(x.Quantity))
            .Aggregate(Money.Zero(), (current, next) => current.Add(next));
        TotalAmount = new Money(Math.Max(0, SubtotalAmount.Amount - DiscountAmount.Amount), SubtotalAmount.Currency);
    }

    public void ApplyDiscount(string couponCode, decimal discountAmount)
    {
        if (discountAmount <= 0)
        {
            return;
        }

        var boundedDiscount = Math.Min(discountAmount, SubtotalAmount.Amount);
        CouponCode = couponCode.Trim().ToUpperInvariant();
        DiscountAmount = new Money(boundedDiscount, SubtotalAmount.Currency);
        RecalculateTotal();
    }

    public void SetPaymentDetails(PaymentMethod method, string? proofUrl)
    {
        PaymentMethod = method;
        PaymentProofUrl = string.IsNullOrWhiteSpace(proofUrl) ? null : proofUrl;
    }
}
