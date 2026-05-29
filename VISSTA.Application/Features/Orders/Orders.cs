using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VISSTA.Application.DTOs;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Domain.Enums;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Application.Features.Orders;

public sealed record PlaceOrderCommand(
    string CustomerId,
    string SessionId,
    string Street,
    string City,
    string Governorate,
    string PostalCode,
    string Country,
    PaymentMethod PaymentMethod,
    string? PaymentProofUrl,
    string PaymentToken,
    string? CouponCode,
    string? CustomerName,
    string? CustomerEmail,
    string? CustomerPhone) : IRequest<int>;
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
    IRepository<Customer> customers,
    IRepository<Coupon> coupons,
    IPaymentService payments,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<OrderHandlers> logger) :
    IRequestHandler<PlaceOrderCommand, int>,
    IRequestHandler<CancelOrderCommand, bool>,
    IRequestHandler<UpdateOrderStatusCommand, bool>,
    IRequestHandler<GetOrderHistoryQuery, IReadOnlyCollection<OrderSummaryDto>>,
    IRequestHandler<GetAllOrdersQuery, IReadOnlyCollection<OrderSummaryDto>>,
    IRequestHandler<GetOrderByIdQuery, OrderDetailDto?>
{
    public async Task<int> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = customers.Query().FirstOrDefault(x => x.Id == request.CustomerId);
        if (customer is null)
        {
            var displayName = !string.IsNullOrWhiteSpace(request.CustomerName)
                ? request.CustomerName
                : request.CustomerId.StartsWith("guest:", StringComparison.OrdinalIgnoreCase)
                    ? "Guest Checkout"
                    : "VISSTA Customer";
            var phone = request.CustomerPhone ?? string.Empty;
            await customers.AddAsync(new Customer(request.CustomerId, displayName, phone, request.CustomerEmail), cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.CustomerName) || !string.IsNullOrWhiteSpace(request.CustomerEmail) || !string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            var name = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName : customer.FullName;
            var email = !string.IsNullOrWhiteSpace(request.CustomerEmail) ? request.CustomerEmail : customer.Email;
            var phone = !string.IsNullOrWhiteSpace(request.CustomerPhone) ? request.CustomerPhone : customer.PhoneNumber;
            customer.UpdateProfile(name, phone, email, customer.DefaultAddress);
            customers.Update(customer);
        }

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

            var itemPrice = new Money(item.Product.EffectivePrice, item.Product.Price.Currency);
            order.AddItem(item.ProductId, item.Size, item.Quantity, itemPrice);
            item.Product.RecordSale(item.Quantity, item.Size);
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

        order.SetPaymentDetails(request.PaymentMethod, request.PaymentProofUrl);

        if (request.PaymentMethod is not (PaymentMethod.CashOnDelivery or PaymentMethod.InstaPayWallet))
        {
            var payment = await payments.ChargeAsync(order.TotalAmount.Amount, order.TotalAmount.Currency, request.PaymentToken, cancellationToken);
            if (!payment.Succeeded)
            {
                throw new InvalidOperationException(payment.FailureReason ?? "Payment failed.");
            }
        }

        order.ChangeStatus(OrderStatus.Pending);
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

        var previousStatus = order.Status;
        order.ChangeStatus(request.Status);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.Status == OrderStatus.Confirmed && previousStatus != OrderStatus.Confirmed)
        {
            try
            {
                var orderCustomer = order.Customer;
                var customerName = orderCustomer?.FullName ?? "Customer";
                var firstName = customerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Customer";
                var customerEmail = orderCustomer?.Email ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(customerEmail))
                {
                    var addr = order.ShippingAddress;
                    var shippingAddress = $"{addr.Street}, {addr.City}, {addr.Governorate} {addr.PostalCode}";

                    var httpContext = httpContextAccessor.HttpContext;
                    var trackingUrl = httpContext is not null
                        ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Account/Orders/{order.Id}"
                        : $"/Account/Orders/{order.Id}";

                    var baseUrl = httpContext is not null
                        ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
                        : "https://vissta.com";

                    var shippingCost = order.TotalAmount.Amount >= 500 ? 0m : 50m;

                    var emailDto = new OrderConfirmationEmailDto(
                        ToEmail: customerEmail,
                        CustomerFirstName: firstName,
                        OrderNumber: $"#VST-{order.Id:D5}",
                        OrderDate: order.CreatedAt,
                        PaymentSummary: GetPaymentSummary(order),
                        EstimatedDelivery: "3–5 Business Days",
                        Lines: order.OrderItems.Select(oi =>
                        {
                            var relativeUrl = oi.Product?.Images
                                .OrderBy(image => image.DisplayOrder)
                                .FirstOrDefault(image => image.IsPrimary)?.Url
                                ?? oi.Product?.Images.OrderBy(image => image.DisplayOrder).FirstOrDefault()?.Url
                                ?? "/assets/product-white-polo.webp";

                            var absoluteUrl = relativeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                ? relativeUrl
                                : $"{baseUrl}{relativeUrl}";

                            return new OrderLineDto(
                                ProductName: oi.Product?.Name ?? "VISSTA Product",
                                Variant: $"Size {oi.Size}",
                                Quantity: oi.Quantity,
                                UnitPrice: oi.UnitPrice.Amount,
                                Currency: oi.UnitPrice.Currency,
                                ImageUrl: absoluteUrl
                            );
                        }).ToList(),
                        Subtotal: order.SubtotalAmount.Amount,
                        ShippingCost: shippingCost,
                        Total: order.TotalAmount.Amount + shippingCost,
                        Currency: order.TotalAmount.Currency,
                        ShippingAddress: shippingAddress,
                        OrderTrackingUrl: trackingUrl,
                        DiscountAmount: order.DiscountAmount.Amount,
                        CouponCode: order.CouponCode
                    );

                    await emailService.SendOrderConfirmationAsync(emailDto);
                }
                else
                {
                    logger.LogWarning("Cannot send order confirmation email for Order {OrderId} because customer email is empty.", order.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send order confirmation email for Order {OrderId}", order.Id);
            }
        }

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
            GetPaymentSummary(order),
            order.PaymentProofUrl,
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
                    x.UnitPrice.Amount * x.Quantity,
                    x.Size);
            }).ToList(),
            order.CustomerId,
            order.Customer == null ? "VISSTA Customer" : order.Customer.FullName,
            order.Customer == null ? string.Empty : order.Customer.PhoneNumber,
            order.ShippingAddress.Country);
    }

    private static string GetPaymentSummary(Order order) => order.PaymentMethod switch
    {
        PaymentMethod.CashOnDelivery => "Cash on Delivery",
        PaymentMethod.InstaPayWallet => "InstaPay / Cash Wallet",
        _ => "Payment"
    };
}
