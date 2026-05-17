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
        Code = code;
        DiscountType = discountType;
        Value = value;
        ExpiryDate = expiryDate;
        MaxUses = maxUses;
    }

    public int Id { get; private set; }
    public string Code { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal Value { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public int MaxUses { get; private set; }
    public int UsedCount { get; private set; }
}
