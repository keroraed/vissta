using VISSTA.Domain.Common;
using VISSTA.Domain.Enums;

namespace VISSTA.Domain.Events;

public sealed record OrderStatusChangedEvent(int OrderId, OrderStatus PreviousStatus, OrderStatus NewStatus)
    : DomainEvent(DateTime.UtcNow);
