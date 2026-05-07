using FinancialAPI.Application.DTOs.Notification;
using FinancialAPI.Application.DTOs.Settlement;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialAPI.Infrastructure.Kafka.Consumers;

public class SettlementConsumer : KafkaConsumerBase<SettlementCompletedMessage>
{
    public SettlementConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> settings,
        ILogger<SettlementConsumer> logger)
        : base(scopeFactory, settings, logger, settings.Value.Topics.SettlementEvents)
    { }
}

public class SettlementCompletedHandler : IKafkaConsumerHandler<SettlementCompletedMessage>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<SettlementCompletedHandler> _logger;

    public SettlementCompletedHandler(
        INotificationService notifications,
        ILogger<SettlementCompletedHandler> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task HandleAsync(SettlementCompletedMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Settlement completed event received. Id: {Id}, Net: {Net}, Transactions: {Count}",
            message.SettlementId, message.NetSettlementAmount, message.TotalTransactionCount);

        // System-level notification (settlement is a back-office operation)
        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            UserId        = "system",
            Type          = NotificationType.SettlementCompleted,
            Title         = "Settlement Batch Completed",
            Message       = $"Settlement batch '{message.BatchReference}' completed. " +
                            $"Net amount: {message.NetSettlementAmount:N2}. " +
                            $"Transactions: {message.SuccessfulCount} succeeded, {message.FailedCount} failed. " +
                            $"Reconciliation ref: {message.ReconciliationReference}.",
            Channel       = "InApp",
            ReferenceId   = message.SettlementId,
            ReferenceType = "Settlement"
        }, ct);
    }
}
