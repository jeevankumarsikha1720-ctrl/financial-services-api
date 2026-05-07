using FinancialAPI.Application.DTOs.Notification;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FinancialAPI.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUnitOfWork uow, ILogger<NotificationService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<NotificationResponse> CreateAsync(
        CreateNotificationRequest request, CancellationToken ct = default)
    {
        var notification = Notification.Create(
            userId:        request.UserId,
            type:          request.Type,
            title:         request.Title,
            message:       request.Message,
            channel:       request.Channel,
            referenceId:   request.ReferenceId,
            referenceType: request.ReferenceType);

        // Mark as sent immediately for InApp channel
        if (request.Channel.Equals("InApp", StringComparison.OrdinalIgnoreCase))
            notification.MarkAsSent();

        await _uow.Notifications.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Notification created. Id: {Id}, User: {User}, Type: {Type}, Channel: {Channel}",
            notification.Id, notification.UserId, notification.Type, notification.Channel);

        return MapToResponse(notification);
    }

    // ── Get by ID ────────────────────────────────────────────────────────────

    public async Task<NotificationResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var n = await _uow.Notifications.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Notification {id} not found.");
        return MapToResponse(n);
    }

    // ── Get All (paged) ───────────────────────────────────────────────────────

    public async Task<NotificationListResponse> GetAllAsync(
        GetNotificationsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _uow.Notifications.GetPagedAsync(
            pageNumber: query.PageNumber,
            pageSize:   query.PageSize,
            predicate:  n =>
                (query.UserId  == null || n.UserId  == query.UserId)  &&
                (query.IsRead  == null || n.IsRead   == query.IsRead) &&
                (query.IsSent  == null || n.IsSent   == query.IsSent) &&
                (query.Channel == null || n.Channel  == query.Channel),
            orderBy:    n => n.CreatedAt,
            descending: true,
            ct:         ct);

        // Count unread for this user
        int unread = 0;
        if (!string.IsNullOrEmpty(query.UserId))
        {
            var (_, unreadTotal) = await _uow.Notifications.GetPagedAsync(
                pageNumber: 1,
                pageSize:   1,
                predicate:  n => n.UserId == query.UserId && !n.IsRead,
                orderBy:    n => n.CreatedAt,
                descending: false,
                ct:         ct);
            unread = unreadTotal;
        }

        return new NotificationListResponse
        {
            Items       = items.Select(MapToResponse).ToList(),
            TotalCount  = total,
            PageNumber  = query.PageNumber,
            PageSize    = query.PageSize,
            UnreadCount = unread
        };
    }

    // ── Mark as Read ──────────────────────────────────────────────────────────

    public async Task<NotificationResponse> MarkAsReadAsync(Guid id, CancellationToken ct = default)
    {
        var n = await _uow.Notifications.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Notification {id} not found.");

        n.MarkAsRead();
        _uow.Notifications.Update(n);
        await _uow.SaveChangesAsync(ct);

        return MapToResponse(n);
    }

    // ── Mark All as Read ─────────────────────────────────────────────────────

    public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        var (unread, _) = await _uow.Notifications.GetPagedAsync(
            pageNumber: 1,
            pageSize:   500,    // batch cap — production would use pagination
            predicate:  n => n.UserId == userId && !n.IsRead,
            orderBy:    n => n.CreatedAt,
            descending: false,
            ct:         ct);

        int count = 0;
        foreach (var n in unread)
        {
            n.MarkAsRead();
            _uow.Notifications.Update(n);
            count++;
        }

        if (count > 0)
            await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Marked {Count} notifications as read for user {UserId}", count, userId);

        return count;
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static NotificationResponse MapToResponse(Notification n) => new()
    {
        Id            = n.Id,
        UserId        = n.UserId,
        Type          = n.Type.ToString(),
        Title         = n.Title,
        Message       = n.Message,
        Channel       = n.Channel,
        ReferenceId   = n.ReferenceId,
        ReferenceType = n.ReferenceType,
        IsRead        = n.IsRead,
        IsSent        = n.IsSent,
        SentAt        = n.SentAt,
        ReadAt        = n.ReadAt,
        RetryCount    = n.RetryCount,
        DeliveryError = n.DeliveryError,
        CreatedAt     = n.CreatedAt
    };
}
