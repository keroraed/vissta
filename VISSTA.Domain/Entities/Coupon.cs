using VISSTA.Domain.Common;
using VISSTA.Domain.Enums;

namespace VISSTA.Domain.Entities;

public sealed class Coupon : Entity, IAggregateRoot
{
    private Coupon()
    {
        Code = string.Empty;
    }

    public Coupon(string code, DiscountType discountType, decimal value, DateTime expiryDate, int maxUses)
    {
        Code = NormalizeCode(code);
        DiscountType = discountType;
        Value = value;
        ExpiryDate = expiryDate;
        MaxUses = maxUses;
        IsActive = true;
    }

    public int Id { get; private set; }
    public string Code { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal Value { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public int MaxUses { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsValid(DateTime utcNow) =>
        IsActive && ExpiryDate >= utcNow && UsedCount < MaxUses;

    public decimal CalculateDiscount(decimal subtotal)
    {
        if (subtotal <= 0)
        {
            return 0;
        }

        var discount = DiscountType == DiscountType.Percentage
            ? subtotal * (Value / 100m)
            : Value;

        return Math.Min(subtotal, Math.Round(discount, 2, MidpointRounding.AwayFromZero));
    }

    public void Update(string code, DiscountType discountType, decimal value, DateTime expiryDate, int maxUses, bool isActive)
    {
        Code = NormalizeCode(code);
        DiscountType = discountType;
        Value = value;
        ExpiryDate = expiryDate;
        MaxUses = maxUses;
        IsActive = isActive;
    }

    public void MarkUsed()
    {
        if (UsedCount >= MaxUses)
        {
            throw new InvalidOperationException("Coupon usage limit has been reached.");
        }

        UsedCount++;
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
