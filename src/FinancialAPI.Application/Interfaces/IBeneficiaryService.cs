using FinancialAPI.Application.DTOs.Beneficiary;

namespace FinancialAPI.Application.Interfaces;

public interface IBeneficiaryService
{
    Task<BeneficiaryResponse> CreateAsync(CreateBeneficiaryRequest request, string userId, CancellationToken ct = default);
    Task<BeneficiaryResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BeneficiaryListResponse> GetAllAsync(GetBeneficiariesQuery query, CancellationToken ct = default);
    Task<BeneficiaryResponse> UpdateAsync(Guid id, UpdateBeneficiaryRequest request, string userId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, string userId, CancellationToken ct = default);
    Task<BeneficiaryResponse> SubmitForReviewAsync(Guid id, CancellationToken ct = default);
    Task<BeneficiaryResponse> VerifyAsync(Guid id, string verifiedBy, CancellationToken ct = default);
    Task<BeneficiaryResponse> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<BeneficiaryResponse> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<BeneficiaryResponse> SuspendAsync(Guid id, CancellationToken ct = default);
}
