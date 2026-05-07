using FinancialAPI.Domain.Common;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Domain.Enums;

namespace FinancialAPI.Domain.Events;

public sealed class FraudAlertRaisedEvent : BaseDomainEvent
{
    public override string EventType => "fraud.alert_raised";
    public Guid AlertId { get; }
    public Guid PaymentId { get; }
    public string AccountId { get; }
    public double RiskScore { get; }
    public FraudRiskLevel RiskLevel { get; }
    public string[] RiskFactors { get; }
    public bool PaymentBlocked { get; }
    public decimal TransactionAmount { get; }
    public CurrencyCode Currency { get; }

    public FraudAlertRaisedEvent(FraudAlert alert)
    {
        AlertId           = alert.Id;
        PaymentId         = alert.PaymentId;
        AccountId         = alert.AccountId;
        RiskScore         = alert.RiskScore;
        RiskLevel         = alert.RiskLevel;
        RiskFactors       = alert.RiskFactors;
        PaymentBlocked    = alert.PaymentBlocked;
        TransactionAmount = alert.TransactionAmount;
        Currency          = alert.Currency;
    }
}
