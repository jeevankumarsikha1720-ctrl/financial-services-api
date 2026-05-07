namespace FinancialAPI.Domain.Common;

/// <summary>
/// Marker interface for all domain events.
/// Domain events represent something that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}
