using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Application.DTOs.Notification;

// ── Response ──────────────────────────────────────────────────────────────────

public class NotificationResponse
{
    public Guid    Id            { get; set; }
    public string  UserId        { get; set; } = string.Empty;
    public string  Type          { get; set; } = string.Empty;
    public string  Title         { get; set; } = string.Empty;
    public string  Message       { get; set; } = string.Empty;
    public string  Channel       { get; set; } = string.Empty;
    public Guid?   ReferenceId   { get; set; }
    public string? ReferenceType { get; set; }
    public bool    IsRead        { get; set; }
    public bool    IsSent        { get; set; }
    public DateTime? SentAt      { get; set; }
    public DateTime? ReadAt      { get; set; }
    public int     RetryCount    { get; set; }
    public string? DeliveryError { get; set; }
    public DateTime CreatedAt    { get; set; }
}

public class NotificationListResponse
{
    public List<NotificationResponse> Items { get; set; } = [];
    public int  TotalCount                  { get; set; }
    public int  PageNumber                  { get; set; }
    public int  PageSize                    { get; set; }
    public int  UnreadCount                 { get; set; }
}

// ── Requests ─────────────────────────────────────────────────────────────────

/// <summary>Internal request — called from Kafka handlers, not from the HTTP controller.</summary>
public class CreateNotificationRequest
{
    public string          UserId        { get; set; } = string.Empty;
    public NotificationType Type         { get; set; }
    public string          Title        { get; set; } = string.Empty;
    public string          Message      { get; set; } = string.Empty;
    public string          Channel      { get; set; } = "InApp";
    public Guid?           ReferenceId   { get; set; }
    public string?         ReferenceType { get; set; }
}

// ── Query ────────────────────────────────────────────────────────────────────

public class GetNotificationsQuery
{
    public string? UserId   { get; set; }
    public string? Type     { get; set; }
    public string? Channel  { get; set; }
    public bool?   IsRead   { get; set; }
    public bool?   IsSent   { get; set; }
    public int PageNumber   { get; set; } = 1;
    public int PageSize     { get; set; } = 20;
}
