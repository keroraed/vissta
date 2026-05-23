using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.Enums;

namespace VISSTA.Application.Features.Coupons;

public sealed record GetCouponsQuery : IRequest<IReadOnlyCollection<CouponDto>>;
public sealed record GetCouponByIdQuery(int Id) : IRequest<CouponDto?>;
public sealed record CreateCouponCommand(string Code, DiscountType DiscountType, decimal Value, DateTime ExpiryDate, int MaxUses, bool IsActive) : IRequest<int>;
public sealed record UpdateCouponCommand(int Id, string Code, DiscountType DiscountType, decimal Value, DateTime ExpiryDate, int MaxUses, bool IsActive) : IRequest<bool>;
public sealed record DeleteCouponCommand(int Id) : IRequest<bool>;

public sealed class CouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CouponCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.MaxUses).GreaterThan(0);
        RuleFor(x => x.ExpiryDate).GreaterThan(DateTime.UtcNow.Date);
        When(x => x.DiscountType == DiscountType.Percentage, () =>
        {
            RuleFor(x => x.Value).LessThanOrEqualTo(100);
        });
    }
}

public sealed class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
{
    public UpdateCouponCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.MaxUses).GreaterThan(0);
        RuleFor(x => x.ExpiryDate).GreaterThan(DateTime.UtcNow.Date);
        When(x => x.DiscountType == DiscountType.Percentage, () =>
        {
            RuleFor(x => x.Value).LessThanOrEqualTo(100);
        });
    }
}

public sealed class CouponHandlers(
    IRepository<Coupon> coupons,
    IUnitOfWork unitOfWork) :
    IRequestHandler<GetCouponsQuery, IReadOnlyCollection<CouponDto>>,
    IRequestHandler<GetCouponByIdQuery, CouponDto?>,
    IRequestHandler<CreateCouponCommand, int>,
    IRequestHandler<UpdateCouponCommand, bool>,
    IRequestHandler<DeleteCouponCommand, bool>
{
    public async Task<IReadOnlyCollection<CouponDto>> Handle(GetCouponsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var items = coupons.QueryReadOnly()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.ExpiryDate)
            .ToList();

        return await Task.FromResult<IReadOnlyCollection<CouponDto>>(items.Select(x => ToDto(x, now)).ToList());
    }

    public async Task<CouponDto?> Handle(GetCouponByIdQuery request, CancellationToken cancellationToken)
    {
        var coupon = await coupons.GetByIdAsync(request.Id, cancellationToken);
        return coupon is null ? null : ToDto(coupon, DateTime.UtcNow);
    }

    public async Task<int> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = new Coupon(request.Code, request.DiscountType, request.Value, request.ExpiryDate, request.MaxUses);
        coupon.Update(request.Code, request.DiscountType, request.Value, request.ExpiryDate, request.MaxUses, request.IsActive);
        await coupons.AddAsync(coupon, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return coupon.Id;
    }

    public async Task<bool> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await coupons.GetByIdAsync(request.Id, cancellationToken);
        if (coupon is null)
        {
            return false;
        }

        coupon.Update(request.Code, request.DiscountType, request.Value, request.ExpiryDate, request.MaxUses, request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await coupons.GetByIdAsync(request.Id, cancellationToken);
        if (coupon is null)
        {
            return false;
        }

        coupons.Remove(coupon);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CouponDto ToDto(Coupon coupon, DateTime now) =>
        new(
            coupon.Id,
            coupon.Code,
            coupon.DiscountType.ToString(),
            coupon.Value,
            coupon.ExpiryDate,
            coupon.MaxUses,
            coupon.UsedCount,
            coupon.IsActive,
            coupon.IsValid(now));
}
