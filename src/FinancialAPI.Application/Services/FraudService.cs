using FinancialAPI.Application.DTOs.Fraud;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialAPI.Application.Services;

public class FraudService : IFraudService
{
    private readonly IUnitOfWork _uow;
    private readonly IKafkaProducer<FraudAlertRaisedMessage>   _raisedProducer;
    private readonly IKafkaProducer<FraudAlertResolvedMessage> _resolvedProducer;
    private readonly KafkaSettings _kafka;
    private readonly ILogger<FraudService> _logger;

    public FraudService(
        IUnitOfWork uow,
        IKafkaProducer<FraudAlertRaisedMessage>   raisedProducer,
        IKafkaProducer<FraudAlertResolvedMessage> resolvedProducer,
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<FraudService> logger)
    {
        _uow              = uow;
        _raisedProducer   = raisedProducer;
        _resolvedProducer = resolvedProducer;
        _kafka            = kafkaSettings.Value;
        _logger           = logger;
    }

    // ── Raise Alert ───────────────────────────────────────────────────────────

    public async Task<FraudAlertResponse> RaiseAlertAsync(
        RaiseFraudAlertRequest request, CancellationToken ct = default)
    {
        // Only raise alerts for medium risk and above (>0.4)
        if (request.RiskScore <= 0.4)
            throw new ArgumentException(
                "Risk score must be above 0.4 to raise a fraud alert.");

        var alert = FraudAlert.Create(
            paymentId:         request.PaymentId,
            accountId:         request.AccountId,
            riskScore:         request.RiskScore,
            riskFactors:       request.RiskFactors,
            transactionAmount: request.TransactionAmount,
            currency:          request.Currency);

        await _uow.FraudAlerts.AddAsync(alert, ct);
        await _uow.SaveChangesAsync(ct);

        // Publish event so notification service can alert compliance team
        await _raisedProducer.ProduceAsync(
            _kafka.Topics.FraudAlerts,
            alert.Id.ToString(),
            new FraudAlertRaisedMessage
            {
                AlertId           = alert.Id,
                PaymentId         = alert.PaymentId,
                AccountId         = alert.AccountId,
                RiskScore         = alert.RiskScore,
                RiskLevel         = alert.RiskLevel.ToString(),
                RiskFactors       = alert.RiskFactors,
                TransactionAmount = alert.TransactionAmount,
                Currency          = alert.Currency.ToString(),
                PaymentBlocked    = alert.PaymentBlocked,
                CreatedAt         = alert.CreatedAt
            }, ct);

        _logger.LogWarning(
            "Fraud alert raised. AlertId: {Id}, Payment: {PaymentId}, Score: {Score:P0}, Blocked: {Blocked}",
            alert.Id, alert.PaymentId, alert.RiskScore, alert.PaymentBlocked);

        return MapToResponse(alert);
    }

    // ── Get by ID ────────────────────────────────────────────────────────────

    public async Task<FraudAlertResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var alert = await _uow.FraudAlerts.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Fraud alert {id} not found.");
        return MapToResponse(alert);
    }

    // ── Get All (paged) ───────────────────────────────────────────────────────

    public async Task<FraudAlertListResponse> GetAllAsync(
        GetFraudAlertsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _uow.FraudAlerts.GetPagedAsync(
            pageNumber: query.PageNumber,
            pageSize:   query.PageSize,
            predicate:  a =>
                (query.AccountId      == null || a.AccountId      == query.AccountId) &&
                (query.IsResolved     == null || a.IsResolved      == query.IsResolved) &&
                (query.PaymentBlocked == null || a.PaymentBlocked  == query.PaymentBlocked) &&
                (query.From           == null || a.CreatedAt       >= query.From) &&
                (query.To             == null || a.CreatedAt       <= query.To),
            orderBy:    a => a.CreatedAt,
            descending: true,
            ct:         ct);

        return new FraudAlertListResponse
        {
            Items      = items.Select(MapToResponse).ToList(),
            TotalCount = total,
            PageNumber = query.PageNumber,
            PageSize   = query.PageSize
        };
    }

    // ── Resolve as Genuine (False Positive) ───────────────────────────────────

    public async Task<FraudAlertResponse> ResolveAsGenuineAsync(
        Guid id, ReviewFraudAlertRequest request, string reviewedBy, CancellationToken ct = default)
    {
        var alert = await _uow.FraudAlerts.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Fraud alert {id} not found.");

        alert.ResolveAsGenuine(reviewedBy, request.Notes);
        _uow.FraudAlerts.Update(alert);
        await _uow.SaveChangesAsync(ct);

        await PublishResolvedEventAsync(alert, "FalsePositive", reviewedBy, ct);

        _logger.LogInformation(
            "Fraud alert marked false positive. AlertId: {Id}, ReviewedBy: {By}",
            id, reviewedBy);

        return MapToResponse(alert);
    }

    // ── Confirm Fraud ────────────────────────────────────────────────────────

    public async Task<FraudAlertResponse> ConfirmFraudAsync(
        Guid id, ReviewFraudAlertRequest request, string reviewedBy, CancellationToken ct = default)
    {
        var alert = await _uow.FraudAlerts.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Fraud alert {id} not found.");

        alert.ConfirmFraud(reviewedBy, request.Notes);
        _uow.FraudAlerts.Update(alert);
        await _uow.SaveChangesAsync(ct);

        await PublishResolvedEventAsync(alert, "ConfirmedFraud", reviewedBy, ct);

        _logger.LogWarning(
            "Fraud confirmed. AlertId: {Id}, PaymentId: {PaymentId}, ReviewedBy: {By}",
            id, alert.PaymentId, reviewedBy);

        return MapToResponse(alert);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task PublishResolvedEventAsync(
        FraudAlert alert, string resolution, string reviewedBy, CancellationToken ct)
    {
        await _resolvedProducer.ProduceAsync(
            _kafka.Topics.FraudAlerts,
            alert.Id.ToString(),
            new FraudAlertResolvedMessage
            {
                AlertId    = alert.Id,
                PaymentId  = alert.PaymentId,
                Resolution = resolution,
                ReviewedBy = reviewedBy,
                ReviewedAt = alert.ReviewedAt ?? DateTime.UtcNow
            }, ct);
    }

    private static FraudAlertResponse MapToResponse(FraudAlert a) => new()
    {
        Id                = a.Id,
        PaymentId         = a.PaymentId,
        AccountId         = a.AccountId,
        RiskScore         = a.RiskScore,
        RiskLevel         = a.RiskLevel.ToString(),
        RiskFactors       = a.RiskFactors,
        AlertDescription  = a.AlertDescription,
        TransactionAmount = a.TransactionAmount,
        Currency          = a.Currency.ToString(),
        IsResolved        = a.IsResolved,
        IsFalsePositive   = a.IsFalsePositive,
        ReviewedBy        = a.ReviewedBy,
        ReviewedAt        = a.ReviewedAt,
        ReviewNotes       = a.ReviewNotes,
        PaymentBlocked    = a.PaymentBlocked,
        CreatedAt         = a.CreatedAt,
        UpdatedAt         = a.UpdatedAt
    };
}
