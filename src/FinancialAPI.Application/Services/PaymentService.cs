using FinancialAPI.Application.DTOs.Payment;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialAPI.Application.Services;

/// <summary>
/// Orchestrates the full payment lifecycle:
///   1. Validate → 2. Save to DB → 3. Publish Kafka event
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IKafkaProducer<PaymentInitiatedMessage> _initiatedProducer;
    private readonly IKafkaProducer<PaymentStatusChangedMessage> _statusProducer;
    private readonly KafkaSettings _kafka;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork uow,
        IKafkaProducer<PaymentInitiatedMessage> initiatedProducer,
        IKafkaProducer<PaymentStatusChangedMessage> statusProducer,
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<PaymentService> logger)
    {
        _uow              = uow;
        _initiatedProducer = initiatedProducer;
        _statusProducer   = statusProducer;
        _kafka            = kafkaSettings.Value;
        _logger           = logger;
    }

    // ── Initiate ──────────────────────────────────────────────────────────
    public async Task<PaymentResponse> InitiateAsync(
        InitiatePaymentRequest request, string userId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Initiating payment. Sender: {Sender}, Amount: {Amount} {Currency}",
            request.SenderAccountId, request.Amount, request.Currency);

        // Verify beneficiary exists and is active
        var beneficiary = await _uow.Beneficiaries.GetByIdAsync(request.BeneficiaryId, ct)
            ?? throw new KeyNotFoundException($"Beneficiary {request.BeneficiaryId} not found.");

        if (beneficiary.Status != BeneficiaryStatus.Active)
            throw new InvalidOperationException(
                $"Beneficiary is not active. Current status: {beneficiary.Status}");

        // Create domain entity
        var payment = Payment.Create(
            senderAccountId:          request.SenderAccountId,
            senderName:               request.SenderName,
            beneficiaryId:            request.BeneficiaryId,
            beneficiaryAccountNumber: request.BeneficiaryAccountNumber,
            beneficiaryBankCode:      request.BeneficiaryBankCode,
            amount:                   request.Amount,
            currency:                 request.Currency,
            type:                     request.Type,
            description:              request.Description,
            createdBy:                userId);

        await _uow.Payments.AddAsync(payment, ct);
        await _uow.SaveChangesAsync(ct);

        // Publish to Kafka
        var message = new PaymentInitiatedMessage
        {
            PaymentId                = payment.Id,
            ReferenceNumber          = payment.ReferenceNumber,
            Amount                   = payment.Amount,
            Currency                 = payment.Currency,
            PaymentType              = payment.Type,
            SenderAccountId          = payment.SenderAccountId,
            SenderName               = payment.SenderName,
            BeneficiaryId            = payment.BeneficiaryId,
            BeneficiaryAccountNumber = payment.BeneficiaryAccountNumber,
            BeneficiaryBankCode      = payment.BeneficiaryBankCode,
            InitiatedAt              = payment.CreatedAt
        };

        await _initiatedProducer.ProduceAsync(
            _kafka.Topics.PaymentEvents,
            payment.Id.ToString(),
            message, ct);

        _logger.LogInformation(
            "Payment initiated. Id: {Id}, Ref: {Ref}",
            payment.Id, payment.ReferenceNumber);

        return MapToResponse(payment);
    }

    // ── Queries ───────────────────────────────────────────────────────────
    public async Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await _uow.Payments.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Payment {id} not found.");
        return MapToResponse(payment);
    }

    public async Task<PaymentResponse> GetByReferenceAsync(
        string referenceNumber, CancellationToken ct = default)
    {
        var payment = await _uow.Payments.FirstOrDefaultAsync(
            p => p.ReferenceNumber == referenceNumber, ct)
            ?? throw new KeyNotFoundException($"Payment '{referenceNumber}' not found.");
        return MapToResponse(payment);
    }

    public async Task<PaymentListResponse> GetAllAsync(
        GetPaymentsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _uow.Payments.GetPagedAsync(
            pageNumber: query.PageNumber,
            pageSize:   query.PageSize,
            predicate:  p =>
                (query.SenderAccountId == null || p.SenderAccountId == query.SenderAccountId) &&
                (query.Status == null || p.Status == query.Status) &&
                (query.Type   == null || p.Type   == query.Type)   &&
                (query.FromDate == null || p.CreatedAt >= query.FromDate) &&
                (query.ToDate   == null || p.CreatedAt <= query.ToDate),
            orderBy:    p => p.CreatedAt,
            descending: true,
            ct:         ct);

        return new PaymentListResponse
        {
            Items       = items.Select(MapToResponse).ToList(),
            TotalCount  = total,
            PageNumber  = query.PageNumber,
            PageSize    = query.PageSize
        };
    }

    // ── State Transitions ─────────────────────────────────────────────────
    public async Task<PaymentResponse> ProcessAsync(Guid id, CancellationToken ct = default)
        => await TransitionAsync(id, p => p.MarkAsProcessing(), ct);

    public async Task<PaymentResponse> SettleAsync(Guid id, CancellationToken ct = default)
        => await TransitionAsync(id, p => p.MarkAsSettled(), ct);

    public async Task<PaymentResponse> FailAsync(
        Guid id, string reason, CancellationToken ct = default)
        => await TransitionAsync(id, p => p.MarkAsFailed(reason), ct);

    public async Task<PaymentResponse> RetryAsync(Guid id, CancellationToken ct = default)
        => await TransitionAsync(id, p => p.IncrementRetry(), ct);

    public async Task<PaymentResponse> CancelAsync(
        Guid id, string reason, CancellationToken ct = default)
        => await TransitionAsync(id, p => p.Cancel(reason), ct);

    // ── Private Helpers ───────────────────────────────────────────────────
    private async Task<PaymentResponse> TransitionAsync(
        Guid id,
        Action<Payment> transition,
        CancellationToken ct)
    {
        var payment = await _uow.Payments.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Payment {id} not found.");

        var oldStatus = payment.Status.ToString();
        transition(payment);

        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync(ct);

        // Publish status change event
        await _statusProducer.ProduceAsync(
            _kafka.Topics.PaymentEvents,
            payment.Id.ToString(),
            new PaymentStatusChangedMessage
            {
                PaymentId       = payment.Id,
                ReferenceNumber = payment.ReferenceNumber,
                OwnerId         = payment.CreatedBy,
                SenderAccountId = payment.SenderAccountId,
                OldStatus       = oldStatus,
                NewStatus       = payment.Status.ToString(),
                FailureReason   = payment.FailureReason,
                CanRetry        = payment.CanRetry(),
                ChangedAt       = DateTime.UtcNow
            }, ct);

        return MapToResponse(payment);
    }

    private static PaymentResponse MapToResponse(Payment p) => new()
    {
        Id                       = p.Id,
        ReferenceNumber          = p.ReferenceNumber,
        Amount                   = p.Amount,
        Currency                 = p.Currency.ToString(),
        Type                     = p.Type.ToString(),
        Status                   = p.Status.ToString(),
        SenderAccountId          = p.SenderAccountId,
        SenderName               = p.SenderName,
        BeneficiaryId            = p.BeneficiaryId,
        BeneficiaryAccountNumber = p.BeneficiaryAccountNumber,
        BeneficiaryBankCode      = p.BeneficiaryBankCode,
        Description              = p.Description,
        FailureReason            = p.FailureReason,
        RetryCount               = p.RetryCount,
        FraudScore               = p.FraudScore,
        IsFlagged                = p.IsFlagged,
        ProcessedAt              = p.ProcessedAt,
        SettledAt                = p.SettledAt,
        CreatedAt                = p.CreatedAt,
        UpdatedAt                = p.UpdatedAt
    };
}
