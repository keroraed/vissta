using VISSTA.Application.DTOs;
using VISSTA.Domain.Entities;
using VISSTA.Domain.Enums;
using VISSTA.Domain.ValueObjects;

namespace VISSTA.Application.Interfaces;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    IQueryable<T> QueryReadOnly();
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public interface IOrderRepository : IRepository<Order>
{
}

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetActiveCartAsync(string? customerId, string sessionId, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task SendOrderConfirmationAsync(OrderConfirmationEmailDto dto);
    Task SendShippingUpdateAsync(string toEmail, int orderId, OrderStatus status, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
    Task SendPasswordResetOtpAsync(string toEmail, string otp);
    Task SendNewsletterWelcomeAsync(string toEmail, string unsubscribeToken);
}

public interface IPaymentService
{
    Task<PaymentResult> ChargeAsync(decimal amount, string currency, string paymentToken, CancellationToken cancellationToken = default);
}

public sealed record PaymentResult(bool Succeeded, string TransactionId, string? FailureReason = null);

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string SessionId { get; }
    bool IsAuthenticated { get; }
}

public interface ICurrencyFormatter
{
    string Format(Money money);
}
