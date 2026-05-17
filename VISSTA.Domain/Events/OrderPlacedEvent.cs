using VISSTA.Domain.Common;

namespace VISSTA.Domain.Events;

public sealed record OrderPlacedEvent(int OrderId, string CustomerId, decimal TotalAmount)
    : DomainEvent(DateTime.UtcNow);
