using FinancialAPI.Application.DTOs.Payment;

namespace FinancialAPI.Application.Interfaces;

/// <summary>
/// Payment processing service interface.
/// Implemented by PaymentService in the Application layer.
/// </summary>
public interface IPaymentService
{
    Task<PaymentResponse> InitiateAsync(InitiatePaymentRequest request, string userId, CancellationToken ct = default);
    Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentResponse> GetByReferenceAsync(string referenceNumber, CancellationToken ct = default);
    Task<PaymentListResponse> GetAllAsync(GetPaymentsQuery query, CancellationToken ct = default);
    Task<PaymentResponse> ProcessAsync(Guid id, CancellationToken ct = default);
    Task<PaymentResponse> SettleAsync(Guid id, CancellationToken ct = default);
    Task<PaymentResponse> FailAsync(Guid id, string reason, CancellationToken ct = default);
    Task<PaymentResponse> RetryAsync(Guid id, CancellationToken ct = default);
    Task<PaymentResponse> CancelAsync(Guid id, string reason, CancellationToken ct = default);
}
