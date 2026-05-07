using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public Guid? ReferenceId { get; private set; }
    public string? ReferenceType { get; private set; }
    public bool IsRead { get; private set; } = false;
    public bool IsSent { get; private set; } = false;
    public DateTime? SentAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public int RetryCount { get; private set; } = 0;
    public string? DeliveryError { get; private set; }

    private Notification() { }

    public static Notification Create(
        string userId, NotificationType type, string title,
        string message, string channel,
        Guid? referenceId = null, string? referenceType = null)
    {
        return new Notification
        {
            UserId        = userId,
            Type          = type,
            Title         = title,
            Message       = message,
            Channel       = channel,
            ReferenceId   = referenceId,
            ReferenceType = referenceType,
            CreatedBy     = "notification-service"
        };
    }

    public void MarkAsSent()   { IsSent = true; SentAt = DateTime.UtcNow; SetUpdatedAt(); }
    public void MarkAsRead()   { IsRead = true; ReadAt = DateTime.UtcNow; SetUpdatedAt(); }
    public void RecordDeliveryFailure(string error) { RetryCount++; DeliveryError = error; SetUpdatedAt(); }
}
