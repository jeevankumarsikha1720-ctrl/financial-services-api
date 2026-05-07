using FinancialAPI.Application.DTOs.Fraud;

namespace FinancialAPI.Application.Interfaces;

public interface IFraudService
{
    /// <summary>Raise a new fraud alert for a payment.</summary>
    Task<FraudAlertResponse> RaiseAlertAsync(
        RaiseFraudAlertRequest request, CancellationToken ct = default);

    /// <summary>Get a fraud alert by ID.</summary>
    Task<FraudAlertResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get paginated fraud alerts with filters.</summary>
    Task<FraudAlertListResponse> GetAllAsync(GetFraudAlertsQuery query, CancellationToken ct = default);

    /// <summary>Mark alert as false positive — unblock payment.</summary>
    Task<FraudAlertResponse> ResolveAsGenuineAsync(
        Guid id, ReviewFraudAlertRequest request, string reviewedBy, CancellationToken ct = default);

    /// <summary>Confirm alert as real fraud — keep payment blocked.</summary>
    Task<FraudAlertResponse> ConfirmFraudAsync(
        Guid id, ReviewFraudAlertRequest request, string reviewedBy, CancellationToken ct = default);
}
