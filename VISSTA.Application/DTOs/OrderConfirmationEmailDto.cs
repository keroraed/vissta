namespace VISSTA.Application.DTOs;

public sealed record OrderConfirmationEmailDto(
    string ToEmail,
    string CustomerFirstName,
    string OrderNumber,
    DateTime OrderDate,
    string PaymentSummary,
    string EstimatedDelivery,
    IReadOnlyList<OrderLineDto> Lines,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string Currency,
    string ShippingAddress,
    string OrderTrackingUrl);

public sealed record OrderLineDto(
    string ProductName,
    string Variant,
    int Quantity,
    decimal UnitPrice,
    string Currency);
