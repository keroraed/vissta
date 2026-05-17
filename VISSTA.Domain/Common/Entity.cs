namespace VISSTA.Domain.Common;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public interface IAggregateRoot
{
}

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}

public abstract record DomainEvent(DateTime OccurredAtUtc) : IDomainEvent;
