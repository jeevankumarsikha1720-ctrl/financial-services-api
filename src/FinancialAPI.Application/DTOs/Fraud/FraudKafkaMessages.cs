namespace FinancialAPI.Application.DTOs.Fraud;

/// <summary>Published when a fraud alert is raised above threshold.</summary>
public class FraudAlertRaisedMessage
{
    public Guid     AlertId           { get; set; }
    public Guid     PaymentId         { get; set; }
    public string   AccountId         { get; set; } = string.Empty;
    public double   RiskScore         { get; set; }
    public string   RiskLevel         { get; set; } = string.Empty;
    public string[] RiskFactors       { get; set; } = [];
    public decimal  TransactionAmount { get; set; }
    public string   Currency          { get; set; } = string.Empty;
    public bool     PaymentBlocked    { get; set; }
    public DateTime CreatedAt         { get; set; }
}

/// <summary>Published when a compliance officer resolves a fraud alert.</summary>
public class FraudAlertResolvedMessage
{
    public Guid     AlertId      { get; set; }
    public Guid     PaymentId    { get; set; }
    public string   Resolution   { get; set; } = string.Empty;  // "FalsePositive" | "ConfirmedFraud"
    public string   ReviewedBy   { get; set; } = string.Empty;
    public DateTime ReviewedAt   { get; set; }
}
