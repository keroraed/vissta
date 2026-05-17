using VISSTA.Domain.Common;

namespace VISSTA.Domain.Events;

public sealed record ProductStockDepletedEvent(int ProductId, string Sku)
    : DomainEvent(DateTime.UtcNow);
