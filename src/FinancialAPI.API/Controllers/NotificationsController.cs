using FinancialAPI.Application.DTOs.Notification;
using FinancialAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialAPI.API.Controllers;

/// <summary>
/// Notifications — query, mark-as-read, and manage in-app notifications.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger              = logger;
    }

    /// <summary>Get paginated notifications with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationListResponse), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetNotificationsQuery query, CancellationToken ct)
        => Ok(await _notificationService.GetAllAsync(query, ct));

    /// <summary>Get all unread notifications for the current user.</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(NotificationListResponse), 200)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var query  = new GetNotificationsQuery { UserId = userId, PageSize = 50 };
        return Ok(await _notificationService.GetAllAsync(query, ct));
    }

    /// <summary>Get a single notification by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _notificationService.GetByIdAsync(id, ct));

    /// <summary>Mark a notification as read.</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
        => Ok(await _notificationService.MarkAsReadAsync(id, ct));

    /// <summary>Mark all notifications as read for the current user.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var count  = await _notificationService.MarkAllAsReadAsync(userId, ct);
        return Ok(new { MarkedAsRead = count });
    }
}
