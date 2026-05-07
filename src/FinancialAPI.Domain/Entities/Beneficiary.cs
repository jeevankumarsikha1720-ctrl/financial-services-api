using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Domain.Events;

namespace FinancialAPI.Domain.Entities;

public class Beneficiary : BaseEntity
{
    public string OwnerId { get; private set; } = string.Empty;
    public string Nickname { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;

    public string AccountNumber { get; private set; } = string.Empty;
    public string? IBAN { get; private set; }
    public string SwiftBic { get; private set; } = string.Empty;
    public string BankName { get; private set; } = string.Empty;
    public string BankCountryCode { get; private set; } = string.Empty;
    public CurrencyCode PreferredCurrency { get; private set; }

    public BeneficiaryStatus Status { get; private set; }
    public bool IsVerified { get; private set; } = false;
    public DateTime? VerifiedAt { get; private set; }
    public string? VerifiedBy { get; private set; }
    public string? RejectionReason { get; private set; }

    private Beneficiary() { }

    public static Beneficiary Create(
    string ownerId, string firstName, string lastName,
    string email, string phoneNumber, string accountNumber,
    string? iban, string swiftBic, string bankName,
    string bankCountryCode, CurrencyCode preferredCurrency,
    string nickname, string createdBy)
    {
        var b = new Beneficiary
        {
            OwnerId = ownerId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            AccountNumber = accountNumber,
            IBAN = iban,
            SwiftBic = swiftBic,
            BankName = bankName,
            BankCountryCode = bankCountryCode,
            PreferredCurrency = preferredCurrency,
            Nickname = nickname,
            Status = BeneficiaryStatus.Pending,
            CreatedBy = createdBy
        };

        b.AddDomainEvent(new BeneficiaryCreatedEvent(b));
        return b;
    }

    public void SubmitForReview()
    {
        if (Status != BeneficiaryStatus.Pending)
            throw new InvalidOperationException("Only pending beneficiaries can be submitted for review.");
        Status = BeneficiaryStatus.UnderReview;
        SetUpdatedAt();
    }

    public void Verify(string verifiedBy)
    {
        if (Status != BeneficiaryStatus.UnderReview)
            throw new InvalidOperationException("Only beneficiaries under review can be verified.");
        Status     = BeneficiaryStatus.Verified;
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        VerifiedBy = verifiedBy;
        SetUpdatedAt(verifiedBy);
        AddDomainEvent(new BeneficiaryStatusChangedEvent(Id, BeneficiaryStatus.UnderReview, BeneficiaryStatus.Verified));
    }

    public void Activate()
    {
        if (Status != BeneficiaryStatus.Verified)
            throw new InvalidOperationException("Only verified beneficiaries can be activated.");
        Status = BeneficiaryStatus.Active;
        SetUpdatedAt();
    }

    public void Reject(string reason)
    {
        Status          = BeneficiaryStatus.Rejected;
        RejectionReason = reason;
        SetUpdatedAt();
    }

    public void Suspend()
    {
        if (Status != BeneficiaryStatus.Active)
            throw new InvalidOperationException("Only active beneficiaries can be suspended.");
        Status = BeneficiaryStatus.Suspended;
        SetUpdatedAt();
    }

    public void Update(
        string firstName, string lastName, string email,
        string phoneNumber, string nickname, string updatedBy)
    {
        FirstName   = firstName;
        LastName    = lastName;
        Email       = email;
        PhoneNumber = phoneNumber;
        Nickname    = nickname;
        // Bank detail changes reset verification
        Status     = BeneficiaryStatus.Pending;
        IsVerified = false;
        VerifiedAt = null;
        SetUpdatedAt(updatedBy);
        AddDomainEvent(new BeneficiaryUpdatedEvent(Id));
    }

    public static bool IsValidIBAN(string? iban)
        => !string.IsNullOrWhiteSpace(iban) && iban.Length >= 15 && iban.Length <= 34
           && char.IsLetter(iban[0]) && char.IsLetter(iban[1]);

    public static bool IsValidSwiftBic(string swiftBic)
        => !string.IsNullOrWhiteSpace(swiftBic) && swiftBic.Length is 8 or 11;
}
