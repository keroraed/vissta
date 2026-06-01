using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Web.Services;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/notifications")]
public sealed class NotificationsController(
    IRepository<AdminNotification> notifications,
    IUnitOfWork unitOfWork,
    AdminNotificationStream stream) : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var recent = await notifications.QueryReadOnly()
            .OrderByDescending(x => x.CreatedAt)
            .Take(12)
            .ToListAsync(cancellationToken);
        var items = recent.Select(AdminNotificationService.ToDto).ToList();

        var unreadCount = await notifications.QueryReadOnly()
            .CountAsync(x => !x.IsRead, cancellationToken);

        return Json(new { unreadCount, items });
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        var subscription = stream.Subscribe();
        try
        {
            await Response.WriteAsync("event: connected\n", cancellationToken);
            await Response.WriteAsync("data: {}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            await foreach (var notification in subscription.Reader.ReadAllAsync(cancellationToken))
            {
                var json = JsonSerializer.Serialize(notification, JsonOptions);
                await Response.WriteAsync("event: notification\n", cancellationToken);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        finally
        {
            stream.Unsubscribe(subscription.Id);
        }
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(id, cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        notification.MarkRead();
        notifications.Update(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var unread = await notifications.Query()
            .Where(x => !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkRead();
            notifications.Update(notification);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}
