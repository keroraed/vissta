using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;

namespace VISSTA.Web.Services;

public sealed record AdminNotificationDto(
    int Id,
    string Type,
    string Title,
    string Body,
    string LinkUrl,
    DateTime CreatedAt,
    bool IsRead);

public sealed class AdminNotificationStream
{
    private readonly ConcurrentDictionary<Guid, Channel<AdminNotificationDto>> _subscribers = new();

    public (Guid Id, ChannelReader<AdminNotificationDto> Reader) Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<AdminNotificationDto>();
        _subscribers[id] = channel;
        return (id, channel.Reader);
    }

    public void Unsubscribe(Guid id)
    {
        if (_subscribers.TryRemove(id, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }

    public async Task PublishAsync(AdminNotificationDto notification, CancellationToken cancellationToken = default)
    {
        foreach (var subscriber in _subscribers.Values)
        {
            await subscriber.Writer.WriteAsync(notification, cancellationToken);
        }
    }
}

public sealed class AdminNotificationService(
    IRepository<AdminNotification> notifications,
    IUnitOfWork unitOfWork,
    AdminNotificationStream stream) : IAdminNotificationService
{
    public Task NotifyNewOrderAsync(int orderId, decimal totalAmount, string currency, CancellationToken cancellationToken = default)
    {
        var amount = $"{totalAmount:N0} {currency}";
        return CreateAsync(
            "order",
            $"New order #{orderId}",
            $"A new order was placed for {amount}.",
            $"/admin/orders/{orderId}",
            cancellationToken);
    }

    public Task NotifyNewReviewAsync(int reviewId, string customerName, int productId, CancellationToken cancellationToken = default)
    {
        var name = string.IsNullOrWhiteSpace(customerName) ? "A customer" : customerName.Trim();
        return CreateAsync(
            "review",
            "New product review",
            $"{name} submitted a new review.",
            $"/admin/reviews#review-{reviewId}",
            cancellationToken);
    }

    public Task NotifyNewSubscriberAsync(string email, CancellationToken cancellationToken = default)
    {
        return CreateAsync(
            "subscriber",
            "New newsletter subscriber",
            email.Trim(),
            $"/admin/newsletter/subscribers#subscriber-{Uri.EscapeDataString(email.Trim().ToLowerInvariant())}",
            cancellationToken);
    }

    private async Task CreateAsync(string type, string title, string body, string linkUrl, CancellationToken cancellationToken)
    {
        var notification = new AdminNotification(type, title, body, linkUrl);
        await notifications.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await stream.PublishAsync(ToDto(notification), cancellationToken);
    }

    public static AdminNotificationDto ToDto(AdminNotification notification) => new(
        notification.Id,
        notification.Type,
        notification.Title,
        notification.Body,
        notification.LinkUrl,
        notification.CreatedAt,
        notification.IsRead);
}
