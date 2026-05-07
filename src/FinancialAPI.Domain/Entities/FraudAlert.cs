using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Enums;
using FinancialAPI.Domain.Events;

namespace FinancialAPI.Domain.Entities;

public class FraudAlert : BaseEntity
{
    public Guid PaymentId { get; private set; }
    public string AccountId { get; private set; } = string.Empty;
    public double RiskScore { get; private set; }
    public FraudRiskLevel RiskLevel { get; private set; }
    public string[] RiskFactors { get; private set; } = [];
    public string AlertDescription { get; private set; } = string.Empty;
    public decimal TransactionAmount { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public bool IsResolved { get; private set; } = false;
    public bool IsFalsePositive { get; private set; } = false;
    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNotes { get; private set; }
    public bool PaymentBlocked { get; private set; } = false;

    private FraudAlert() { }

    public static FraudAlert Create(
        Guid paymentId, string accountId, double riskScore,
        string[] riskFactors, decimal transactionAmount,
        CurrencyCode currency, string createdBy = "fraud-detection-engine")
    {
        var alert = new FraudAlert
        {
            PaymentId         = paymentId,
            AccountId         = accountId,
            RiskScore         = riskScore,
            RiskLevel         = CalculateRiskLevel(riskScore),
            RiskFactors       = riskFactors,
            AlertDescription  = $"Risk score {riskScore:P0}. Factors: {string.Join(", ", riskFactors)}.",
            TransactionAmount = transactionAmount,
            Currency          = currency,
            PaymentBlocked    = riskScore >= 0.8,
            CreatedBy         = createdBy
        };
        alert.AddDomainEvent(new FraudAlertRaisedEvent(alert));
        return alert;
    }

    public void ResolveAsGenuine(string reviewedBy, string notes)
    {
        IsFalsePositive = true;
        ReviewedBy      = reviewedBy;
        ReviewedAt      = DateTime.UtcNow;
        ReviewNotes     = notes;
        PaymentBlocked  = false;
        SetUpdatedAt(reviewedBy);
    }

    public void ConfirmFraud(string reviewedBy, string notes)
    {
        IsResolved     = true;
        ReviewedBy     = reviewedBy;
        ReviewedAt     = DateTime.UtcNow;
        ReviewNotes    = notes;
        PaymentBlocked = true;
        SetUpdatedAt(reviewedBy);
    }

    private static FraudRiskLevel CalculateRiskLevel(double score) => score switch
    {
        <= 0.20 => FraudRiskLevel.None,
        <= 0.40 => FraudRiskLevel.Low,
        <= 0.60 => FraudRiskLevel.Medium,
        <= 0.80 => FraudRiskLevel.High,
        _       => FraudRiskLevel.Critical
    };
}
