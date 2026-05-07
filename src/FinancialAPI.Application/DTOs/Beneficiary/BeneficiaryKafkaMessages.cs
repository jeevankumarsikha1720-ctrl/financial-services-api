namespace FinancialAPI.Application.DTOs.Beneficiary;

public record BeneficiaryCreatedMessage
{
    public Guid BeneficiaryId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string SwiftBic { get; init; } = string.Empty;
    public string BankCountryCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record BeneficiaryVerifiedMessage
{
    public Guid BeneficiaryId { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string VerifiedBy { get; init; } = string.Empty;
    public DateTime VerifiedAt { get; init; }
}
