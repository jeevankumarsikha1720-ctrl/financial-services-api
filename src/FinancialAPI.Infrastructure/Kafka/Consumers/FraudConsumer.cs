using FinancialAPI.Application.DTOs.Fraud;
using FinancialAPI.Application.DTOs.Notification;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialAPI.Infrastructure.Kafka.Consumers;

/// <summary>
/// Background consumer for the fraud-alerts Kafka topic.
/// Handles both FraudAlertRaised and FraudAlertResolved messages.
/// </summary>
public class FraudAlertRaisedConsumer : KafkaConsumerBase<FraudAlertRaisedMessage>
{
    public FraudAlertRaisedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> settings,
        ILogger<FraudAlertRaisedConsumer> logger)
        : base(scopeFactory, settings, logger, settings.Value.Topics.FraudAlerts)
    { }
}

public class FraudAlertRaisedHandler : IKafkaConsumerHandler<FraudAlertRaisedMessage>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<FraudAlertRaisedHandler> _logger;

    public FraudAlertRaisedHandler(
        INotificationService notifications,
        ILogger<FraudAlertRaisedHandler> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task HandleAsync(FraudAlertRaisedMessage message, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Fraud alert received. AlertId: {Id}, Payment: {PaymentId}, " +
            "Score: {Score:P0}, Level: {Level}, Blocked: {Blocked}, Factors: [{Factors}]",
            message.AlertId, message.PaymentId, message.RiskScore,
            message.RiskLevel, message.PaymentBlocked,
            string.Join(", ", message.RiskFactors));

        var blockedNote = message.PaymentBlocked
            ? " Your payment has been BLOCKED pending investigation."
            : " Your payment is still being processed.";

        // Notify the account holder
        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            UserId        = message.AccountId,
            Type          = NotificationType.FraudAlertRaised,
            Title         = $"Fraud Alert — {message.RiskLevel} Risk",
            Message       = $"A {message.RiskLevel.ToLower()} risk flag was raised on payment {message.PaymentId} " +
                            $"(score: {message.RiskScore:P0}). Factors: {string.Join(", ", message.RiskFactors)}.{blockedNote}",
            Channel       = "InApp",
            ReferenceId   = message.AlertId,
            ReferenceType = "FraudAlert"
        }, ct);

        // Also create a compliance-team notification (routed to "compliance" queue in production)
        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            UserId        = "compliance-team",
            Type          = NotificationType.FraudAlertRaised,
            Title         = $"[COMPLIANCE] {message.RiskLevel} Fraud Alert",
            Message       = $"Alert {message.AlertId} | Payment {message.PaymentId} | " +
                            $"Account: {message.AccountId} | Amount: {message.TransactionAmount:N2} {message.Currency} | " +
                            $"Score: {message.RiskScore:P0} | Blocked: {message.PaymentBlocked}",
            Channel       = "InApp",
            ReferenceId   = message.AlertId,
            ReferenceType = "FraudAlert"
        }, ct);
    }
}

public class FraudAlertResolvedHandler : IKafkaConsumerHandler<FraudAlertResolvedMessage>
{
    private readonly ILogger<FraudAlertResolvedHandler> _logger;

    public FraudAlertResolvedHandler(ILogger<FraudAlertResolvedHandler> logger)
        => _logger = logger;

    public async Task HandleAsync(FraudAlertResolvedMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Fraud alert resolved. AlertId: {Id}, Payment: {PaymentId}, " +
            "Resolution: {Resolution}, By: {By}",
            message.AlertId, message.PaymentId, message.Resolution, message.ReviewedBy);

        await Task.CompletedTask;
    }
}
