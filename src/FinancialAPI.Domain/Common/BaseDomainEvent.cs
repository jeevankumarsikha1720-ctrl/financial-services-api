namespace FinancialAPI.Domain.Common;

/// <summary>
/// Base implementation for all domain events.
/// </summary>
public abstract class BaseDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}
