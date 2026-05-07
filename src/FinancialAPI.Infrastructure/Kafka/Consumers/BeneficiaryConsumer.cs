using FinancialAPI.Application.DTOs.Beneficiary;
using FinancialAPI.Application.DTOs.Notification;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialAPI.Infrastructure.Kafka.Consumers;

public class BeneficiaryConsumer : KafkaConsumerBase<BeneficiaryCreatedMessage>
{
    public BeneficiaryConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> settings,
        ILogger<BeneficiaryConsumer> logger)
        : base(scopeFactory, settings, logger, settings.Value.Topics.BeneficiaryEvents)
    { }
}

public class BeneficiaryCreatedHandler : IKafkaConsumerHandler<BeneficiaryCreatedMessage>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<BeneficiaryCreatedHandler> _logger;

    public BeneficiaryCreatedHandler(
        INotificationService notifications,
        ILogger<BeneficiaryCreatedHandler> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task HandleAsync(BeneficiaryCreatedMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Beneficiary created event received. Id: {Id}, Owner: {Owner}, Name: {Name}",
            message.BeneficiaryId, message.OwnerId, message.FullName);

        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            UserId        = message.OwnerId,
            Type          = NotificationType.BeneficiaryAdded,
            Title         = "Beneficiary Added",
            Message       = $"Beneficiary '{message.FullName}' (account: {message.AccountNumber}) has been added " +
                            $"and is pending compliance review.",
            Channel       = "InApp",
            ReferenceId   = message.BeneficiaryId,
            ReferenceType = "Beneficiary"
        }, ct);
    }
}

public class BeneficiaryVerifiedHandler : IKafkaConsumerHandler<BeneficiaryVerifiedMessage>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<BeneficiaryVerifiedHandler> _logger;

    public BeneficiaryVerifiedHandler(
        INotificationService notifications,
        ILogger<BeneficiaryVerifiedHandler> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task HandleAsync(BeneficiaryVerifiedMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Beneficiary verified. Id: {Id}, Owner: {Owner}, VerifiedBy: {By}",
            message.BeneficiaryId, message.OwnerId, message.VerifiedBy);

        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            UserId        = message.OwnerId,
            Type          = NotificationType.BeneficiaryVerified,
            Title         = "Beneficiary Verified",
            Message       = $"Your beneficiary '{message.FullName}' has been verified and is ready to activate. " +
                            $"You can now initiate payments to this beneficiary.",
            Channel       = "InApp",
            ReferenceId   = message.BeneficiaryId,
            ReferenceType = "Beneficiary"
        }, ct);
    }
}
