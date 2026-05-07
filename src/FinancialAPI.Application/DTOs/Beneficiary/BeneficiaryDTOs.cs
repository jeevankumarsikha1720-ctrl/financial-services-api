using System.ComponentModel.DataAnnotations;
using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Application.DTOs.Beneficiary;

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateBeneficiaryRequest
{
    [Required] public string FirstName { get; init; } = string.Empty;
    [Required] public string LastName { get; init; } = string.Empty;
    [Required][EmailAddress] public string Email { get; init; } = string.Empty;
    [Phone] public string PhoneNumber { get; init; } = string.Empty;
    [Required] public string AccountNumber { get; init; } = string.Empty;
    public string? IBAN { get; init; }
    [Required][StringLength(11, MinimumLength = 8)] public string SwiftBic { get; init; } = string.Empty;
    [Required] public string BankName { get; init; } = string.Empty;
    [Required][StringLength(2, MinimumLength = 2)] public string BankCountryCode { get; init; } = string.Empty;
    [Required] public CurrencyCode PreferredCurrency { get; init; }
    [Required] public string Nickname { get; init; } = string.Empty;
}

public record UpdateBeneficiaryRequest
{
    [Required] public string FirstName { get; init; } = string.Empty;
    [Required] public string LastName { get; init; } = string.Empty;
    [Required][EmailAddress] public string Email { get; init; } = string.Empty;
    [Phone] public string PhoneNumber { get; init; } = string.Empty;
    [Required] public string Nickname { get; init; } = string.Empty;
}

public record VerifyBeneficiaryRequest
{
    [Required] public string Notes { get; init; } = string.Empty;
}

public record RejectBeneficiaryRequest
{
    [Required] public string Reason { get; init; } = string.Empty;
}

public record GetBeneficiariesQuery
{
    public string? OwnerId { get; init; }
    public BeneficiaryStatus? Status { get; init; }
    public string? BankCountryCode { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record BeneficiaryResponse
{
    public Guid Id { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string? IBAN { get; init; }
    public string SwiftBic { get; init; } = string.Empty;
    public string BankName { get; init; } = string.Empty;
    public string BankCountryCode { get; init; } = string.Empty;
    public string PreferredCurrency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public string? VerifiedBy { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record BeneficiaryListResponse
{
    public IReadOnlyList<BeneficiaryResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
