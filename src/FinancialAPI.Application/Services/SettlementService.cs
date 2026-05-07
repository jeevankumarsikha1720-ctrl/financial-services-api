using FinancialAPI.Application.DTOs.Settlement;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialAPI.Application.Services;

public class SettlementService : ISettlementService
{
    private readonly IUnitOfWork _uow;
    private readonly IKafkaProducer<SettlementCreatedMessage> _createdProducer;
    private readonly IKafkaProducer<SettlementCompletedMessage> _completedProducer;
    private readonly KafkaSettings _kafka;
    private readonly ILogger<SettlementService> _logger;

    public SettlementService(
        IUnitOfWork uow,
        IKafkaProducer<SettlementCreatedMessage> createdProducer,
        IKafkaProducer<SettlementCompletedMessage> completedProducer,
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<SettlementService> logger)
    {
        _uow              = uow;
        _createdProducer  = createdProducer;
        _completedProducer = completedProducer;
        _kafka            = kafkaSettings.Value;
        _logger           = logger;
    }

    public async Task<SettlementResponse> CreateAsync(
        CreateSettlementRequest request, string userId, CancellationToken ct = default)
    {
        var settlement = Settlement.Create(request.Currency, request.SettlementDate, userId);

        await _uow.Settlements.AddAsync(settlement, ct);
        await _uow.SaveChangesAsync(ct);

        await _createdProducer.ProduceAsync(
            _kafka.Topics.SettlementEvents,
            settlement.Id.ToString(),
            new SettlementCreatedMessage
            {
                SettlementId   = settlement.Id,
                BatchReference = settlement.BatchReference,
                Currency       = settlement.Currency,
                SettlementDate = settlement.SettlementDate,
                CreatedAt      = settlement.CreatedAt
            }, ct);

        _logger.LogInformation("Settlement created. Id: {Id}, Ref: {Ref}",
            settlement.Id, settlement.BatchReference);

        return MapToResponse(settlement);
    }

    public async Task<SettlementResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var settlement = await _uow.Settlements.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Settlement {id} not found.");
        return MapToResponse(settlement);
    }

    public async Task<SettlementListResponse> GetAllAsync(
        GetSettlementsQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _uow.Settlements.GetPagedAsync(
            pageNumber: query.PageNumber,
            pageSize:   query.PageSize,
            predicate:  s =>
                (query.Status   == null || s.Status   == query.Status)   &&
                (query.Currency == null || s.Currency == query.Currency) &&
                (query.FromDate == null || s.CreatedAt >= query.FromDate) &&
                (query.ToDate   == null || s.CreatedAt <= query.ToDate),
            orderBy:    s => s.CreatedAt,
            descending: true,
            ct:         ct);

        return new SettlementListResponse
        {
            Items      = items.Select(MapToResponse).ToList(),
            TotalCount = total,
            PageNumber = query.PageNumber,
            PageSize   = query.PageSize
        };
    }

    public async Task<SettlementResponse> AddEntryAsync(
        Guid settlementId, AddSettlementEntryRequest request, CancellationToken ct = default)
    {
        var settlement = await _uow.Settlements.GetByIdAsync(settlementId, ct)
            ?? throw new KeyNotFoundException($"Settlement {settlementId} not found.");

        settlement.AddEntry(request.PaymentId, request.GrossAmount, request.Fee);
        _uow.Settlements.Update(settlement);
        await _uow.SaveChangesAsync(ct);

        return MapToResponse(settlement);
    }

    public async Task<SettlementResponse> StartProcessingAsync(
        Guid id, CancellationToken ct = default)
    {
        var settlement = await _uow.Settlements.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Settlement {id} not found.");

        settlement.StartProcessing();
        _uow.Settlements.Update(settlement);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Settlement processing started. Id: {Id}", id);
        return MapToResponse(settlement);
    }

    public async Task<SettlementResponse> CompleteAsync(
        Guid id, CompleteSettlementRequest request, CancellationToken ct = default)
    {
        var settlement = await _uow.Settlements.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Settlement {id} not found.");

        settlement.Complete(request.ReconciliationReference);
        _uow.Settlements.Update(settlement);
        await _uow.SaveChangesAsync(ct);

        await _completedProducer.ProduceAsync(
            _kafka.Topics.SettlementEvents,
            settlement.Id.ToString(),
            new SettlementCompletedMessage
            {
                SettlementId            = settlement.Id,
                BatchReference          = settlement.BatchReference,
                TotalGrossAmount        = settlement.TotalGrossAmount,
                TotalFees               = settlement.TotalFees,
                NetSettlementAmount     = settlement.NetSettlementAmount,
                TotalTransactionCount   = settlement.TotalTransactionCount,
                SuccessfulCount         = settlement.SuccessfulCount,
                FailedCount             = settlement.FailedCount,
                ReconciliationReference = settlement.ReconciliationReference!,
                CompletedAt             = settlement.CompletedAt!.Value
            }, ct);

        _logger.LogInformation("Settlement completed. Id: {Id}, Net: {Net} {Currency}",
            id, settlement.NetSettlementAmount, settlement.Currency);

        return MapToResponse(settlement);
    }

    public async Task<SettlementResponse> ReconcileAsync(
        Guid id, ReconcileSettlementRequest request, CancellationToken ct = default)
    {
        var settlement = await _uow.Settlements.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Settlement {id} not found.");

        settlement.Reconcile(request.Notes);
        _uow.Settlements.Update(settlement);
        await _uow.SaveChangesAsync(ct);

        return MapToResponse(settlement);
    }

    public async Task<SettlementResponse> FailAsync(
        Guid id, string reason, CancellationToken ct = default)
    {
        var settlement = await _uow.Settlements.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Settlement {id} not found.");

        settlement.MarkAsFailed(reason);
        _uow.Settlements.Update(settlement);
        await _uow.SaveChangesAsync(ct);

        return MapToResponse(settlement);
    }

    private static SettlementResponse MapToResponse(Settlement s) => new()
    {
        Id                      = s.Id,
        BatchReference          = s.BatchReference,
        Status                  = s.Status.ToString(),
        Currency                = s.Currency.ToString(),
        SettlementDate          = s.SettlementDate,
        TotalGrossAmount        = s.TotalGrossAmount,
        TotalFees               = s.TotalFees,
        NetSettlementAmount     = s.NetSettlementAmount,
        TotalTransactionCount   = s.TotalTransactionCount,
        SuccessfulCount         = s.SuccessfulCount,
        FailedCount             = s.FailedCount,
        ReconciliationReference = s.ReconciliationReference,
        ReconciledAt            = s.ReconciledAt,
        Notes                   = s.Notes,
        FailureReason           = s.FailureReason,
        CompletedAt             = s.CompletedAt,
        CreatedAt               = s.CreatedAt,
        Entries                 = s.Entries.Select(e => new SettlementEntryResponse
        {
            Id            = e.Id,
            PaymentId     = e.PaymentId,
            GrossAmount   = e.GrossAmount,
            Fee           = e.Fee,
            NetAmount     = e.NetAmount,
            IsFailed      = e.IsFailed,
            FailureReason = e.FailureReason
        }).ToList()
    };
}
