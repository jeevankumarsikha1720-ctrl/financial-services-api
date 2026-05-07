using FinancialAPI.Application.DTOs.Settlement;

namespace FinancialAPI.Application.Interfaces;

public interface ISettlementService
{
    Task<SettlementResponse> CreateAsync(CreateSettlementRequest request, string userId, CancellationToken ct = default);
    Task<SettlementResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SettlementListResponse> GetAllAsync(GetSettlementsQuery query, CancellationToken ct = default);
    Task<SettlementResponse> AddEntryAsync(Guid settlementId, AddSettlementEntryRequest request, CancellationToken ct = default);
    Task<SettlementResponse> StartProcessingAsync(Guid id, CancellationToken ct = default);
    Task<SettlementResponse> CompleteAsync(Guid id, CompleteSettlementRequest request, CancellationToken ct = default);
    Task<SettlementResponse> ReconcileAsync(Guid id, ReconcileSettlementRequest request, CancellationToken ct = default);
    Task<SettlementResponse> FailAsync(Guid id, string reason, CancellationToken ct = default);
}
