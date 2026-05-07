using FinancialAPI.Application.DTOs.Notification;

namespace FinancialAPI.Application.Interfaces;

public interface INotificationService
{
    /// <summary>Create and persist a notification (called from Kafka handlers).</summary>
    Task<NotificationResponse> CreateAsync(
        CreateNotificationRequest request, CancellationToken ct = default);

    /// <summary>Get a single notification by ID.</summary>
    Task<NotificationResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get paginated notifications with optional filters.</summary>
    Task<NotificationListResponse> GetAllAsync(
        GetNotificationsQuery query, CancellationToken ct = default);

    /// <summary>Mark a notification as read.</summary>
    Task<NotificationResponse> MarkAsReadAsync(Guid id, CancellationToken ct = default);

    /// <summary>Mark all unread notifications for a user as read.</summary>
    Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default);
}
