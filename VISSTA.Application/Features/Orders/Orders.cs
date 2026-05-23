using FluentValidation;
using MediatR;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.Enums;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Application.Features.Orders;

public sealed record PlaceOrderCommand(string CustomerId, string SessionId, string Street, string City, string Governorate, string PostalCode, string Country, string PaymentToken, string? CouponCode) : IRequest<int>;
public sealed record CancelOrderCommand(int OrderId, string CustomerId) : IRequest<bool>;
public sealed record UpdateOrderStatusCommand(int OrderId, OrderStatus Status) : IRequest<bool>;
public sealed record GetOrderHistoryQuery(string CustomerId) : IRequest<IReadOnlyCollection<OrderSummaryDto>>;
public sealed record GetAllOrdersQuery(OrderStatus? Status = null) : IRequest<IReadOnlyCollection<OrderSummaryDto>>;
public sealed record GetOrderByIdQuery(int OrderId, string? CustomerId = null) : IRequest<OrderDetailDto?>;

public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Governorate).NotEmpty();
    }
}

public sealed class OrderHandlers(
    IOrderRepository orders,
    ICartRepository carts,
    IRepository<Coupon> coupons,
    IPaymentService payments,
    IUnitOfWork unitOfWork) :
    IRequestHandler<PlaceOrderCommand, int>,
    IRequestHandler<CancelOrderCommand, bool>,
    IRequestHandler<UpdateOrderStatusCommand, bool>,
    IRequestHandler<GetOrderHistoryQuery, IReadOnlyCollection<OrderSummaryDto>>,
    IRequestHandler<GetAllOrdersQuery, IReadOnlyCollection<OrderSummaryDto>>,
    IRequestHandler<GetOrderByIdQuery, OrderDetailDto?>
{
    public async Task<int> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var cart = await carts.GetActiveCartAsync(request.CustomerId, request.SessionId, cancellationToken);
        if (cart is null || cart.CartItems.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        var order = new Order(request.CustomerId, new Address(request.Street, request.City, request.Governorate, request.PostalCode, request.Country));
        foreach (var item in cart.CartItems)
        {
            if (item.Product is null)
            {
                continue;
            }

            order.AddItem(item.ProductId, item.Quantity, item.Product.Price);
            item.Product.RecordSale(item.Quantity);
        }

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var normalizedCode = request.CouponCode.Trim().ToUpperInvariant();
            var coupon = coupons.Query()
                .FirstOrDefault(x => x.Code == normalizedCode);

            if (coupon is null || !coupon.IsValid(DateTime.UtcNow))
            {
                throw new InvalidOperationException("Coupon code is invalid or expired.");
            }

            var discount = coupon.CalculateDiscount(order.SubtotalAmount.Amount);
            order.ApplyDiscount(coupon.Code, discount);
            coupon.MarkUsed();
        }

        var payment = await payments.ChargeAsync(order.TotalAmount.Amount, order.TotalAmount.Currency, request.PaymentToken, cancellationToken);
        if (!payment.Succeeded)
        {
            throw new InvalidOperationException(payment.FailureReason ?? "Payment failed.");
        }

        order.ChangeStatus(OrderStatus.Confirmed);
        await orders.AddAsync(order, cancellationToken);
        cart.Clear();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        order.MarkPlaced();
        return order.Id;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null || order.CustomerId != request.CustomerId)
        {
            return false;
        }

        order.ChangeStatus(OrderStatus.Cancelled);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        order.ChangeStatus(request.Status);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<IReadOnlyCollection<OrderSummaryDto>> Handle(GetOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = orders.QueryReadOnly()
            .Where(x => x.CustomerId == request.CustomerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new OrderSummaryDto(
                x.Id,
                x.Status.ToString(),
                x.TotalAmount.Amount,
                x.TotalAmount.Currency,
                x.CreatedAt,
                x.DiscountAmount.Amount,
                x.CouponCode,
                x.Customer == null ? "VISSTA Customer" : x.Customer.FullName,
                x.Customer == null ? string.Empty : x.Customer.PhoneNumber,
                x.OrderItems.Sum(item => item.Quantity)))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<OrderSummaryDto>>(history);
    }

    public Task<IReadOnlyCollection<OrderSummaryDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = orders.QueryReadOnly();
        if (request.Status is not null)
        {
            query = query.Where(x => x.Status == request.Status);
        }

        var history = query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new OrderSummaryDto(
                x.Id,
                x.Status.ToString(),
                x.TotalAmount.Amount,
                x.TotalAmount.Currency,
                x.CreatedAt,
                x.DiscountAmount.Amount,
                x.CouponCode,
                x.Customer == null ? "VISSTA Customer" : x.Customer.FullName,
                x.Customer == null ? string.Empty : x.Customer.PhoneNumber,
                x.OrderItems.Sum(item => item.Quantity)))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<OrderSummaryDto>>(history);
    }

    public async Task<OrderDetailDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null || (request.CustomerId is not null && order.CustomerId != request.CustomerId))
        {
            return null;
        }

        return new OrderDetailDto(
            order.Id,
            order.Status.ToString(),
            order.SubtotalAmount.Amount,
            order.DiscountAmount.Amount,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAt,
            order.ShippingAddress.Street,
            order.ShippingAddress.City,
            order.ShippingAddress.Governorate,
            order.ShippingAddress.PostalCode,
            order.CouponCode,
            order.OrderItems.Select(x =>
            {
                var imageUrl = x.Product?.Images
                    .OrderBy(image => image.DisplayOrder)
                    .FirstOrDefault(image => image.IsPrimary)?.Url
                    ?? x.Product?.Images.OrderBy(image => image.DisplayOrder).FirstOrDefault()?.Url
                    ?? "/assets/product-white-polo.webp";

                return new OrderItemDto(
                    x.ProductId,
                    x.Product?.Name ?? "VISSTA Product",
                    imageUrl,
                    x.Quantity,
                    x.UnitPrice.Amount,
                    x.UnitPrice.Amount * x.Quantity);
            }).ToList(),
            order.CustomerId,
            order.Customer == null ? "VISSTA Customer" : order.Customer.FullName,
            order.Customer == null ? string.Empty : order.Customer.PhoneNumber,
            order.ShippingAddress.Country);
    }
}
