using FinancialAPI.Application.DTOs.Notification;
using FinancialAPI.Application.DTOs.Payment;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialAPI.Infrastructure.Kafka.Consumers;

/// <summary>
/// Background service that consumes messages from the "payment-events" topic.
/// </summary>
public class PaymentInitiatedConsumer : KafkaConsumerBase<PaymentInitiatedMessage>
{
    public PaymentInitiatedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> settings,
        ILogger<PaymentInitiatedConsumer> logger)
        : base(scopeFactory, settings, logger, settings.Value.Topics.PaymentEvents)
    { }
}

/// <summary>
/// Handles PaymentInitiatedMessage — creates an InApp notification for the sender.
/// </summary>
public class PaymentInitiatedHandler : IKafkaConsumerHandler<PaymentInitiatedMessage>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<PaymentInitiatedHandler> _logger;

    public PaymentInitiatedHandler(
        INotificationService notifications,
        ILogger<PaymentInitiatedHandler> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task HandleAsync(PaymentInitiatedMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Processing payment initiated event. PaymentId: {Id}, Amount: {Amount} {Currency}",
            message.PaymentId, message.Amount, message.Currency);

        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            UserId        = message.SenderAccountId,
            Type          = NotificationType.PaymentInitiated,
            Title         = "Payment Initiated",
            Message       = $"Your payment of {message.Amount:N2} {message.Currency} has been initiated successfully. Reference: {message.ReferenceNumber}.",
            Channel       = "InApp",
            ReferenceId   = message.PaymentId,
            ReferenceType = "Payment"
        }, ct);
    }
}

/// <summary>
/// Handles PaymentStatusChangedMessage — notifies the user of status updates.
/// </summary>
public class PaymentStatusChangedHandler : IKafkaConsumerHandler<PaymentStatusChangedMessage>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<PaymentStatusChangedHandler> _logger;

    public PaymentStatusChangedHandler(
        INotificationService notifications,
        ILogger<PaymentStatusChangedHandler> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task HandleAsync(PaymentStatusChangedMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Payment status changed. Id: {Id}, {Old} → {New}",
            message.PaymentId, message.OldStatus, message.NewStatus);

        var (type, title, body) = message.NewStatus switch
        {
            "Settled"    => (NotificationType.PaymentSettled,
                             "Payment Settled",
                             $"Your payment {message.PaymentId} has been settled successfully."),
            "Failed"     => (NotificationType.PaymentFailed,
                             "Payment Failed",
                             $"Your payment {message.PaymentId} could not be processed. Please try again or contact support."),
            "OnHold"     => (NotificationType.PaymentOnHold,
                             "Payment On Hold",
                             $"Your payment {message.PaymentId} has been placed on hold for review."),
            _            => (NotificationType.SystemAlert,
                             $"Payment Status Update",
                             $"Payment {message.PaymentId} status changed from {message.OldStatus} to {message.NewStatus}.")
        };

        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            UserId        = message.OwnerId,
            Type          = type,
            Title         = title,
            Message       = body,
            Channel       = "InApp",
            ReferenceId   = message.PaymentId,
            ReferenceType = "Payment"
        }, ct);
    }
}
