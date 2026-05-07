using FinancialAPI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FinancialAPI.Application.DTOs.Fraud;

// ── Response ──────────────────────────────────────────────────────────────────

public class FraudAlertResponse
{
    public Guid     Id                { get; set; }
    public Guid     PaymentId         { get; set; }
    public string   AccountId         { get; set; } = string.Empty;
    public double   RiskScore         { get; set; }
    public string   RiskLevel         { get; set; } = string.Empty;  // None/Low/Medium/High/Critical
    public string[] RiskFactors       { get; set; } = [];
    public string   AlertDescription  { get; set; } = string.Empty;
    public decimal  TransactionAmount { get; set; }
    public string   Currency          { get; set; } = string.Empty;
    public bool     IsResolved        { get; set; }
    public bool     IsFalsePositive   { get; set; }
    public string?  ReviewedBy        { get; set; }
    public DateTime? ReviewedAt       { get; set; }
    public string?  ReviewNotes       { get; set; }
    public bool     PaymentBlocked    { get; set; }
    public DateTime CreatedAt         { get; set; }
    public DateTime? UpdatedAt        { get; set; }
}

public class FraudAlertListResponse
{
    public List<FraudAlertResponse> Items { get; set; } = [];
    public int  TotalCount                { get; set; }
    public int  PageNumber                { get; set; }
    public int  PageSize                  { get; set; }
}

// ── Requests ─────────────────────────────────────────────────────────────────

public class RaiseFraudAlertRequest
{
    [Required] public Guid   PaymentId         { get; set; }
    [Required] public string AccountId         { get; set; } = string.Empty;
    [Range(0, 1)] public double RiskScore      { get; set; }
    [Required] public string[] RiskFactors     { get; set; } = [];
    [Range(0.01, double.MaxValue)]
    public decimal TransactionAmount           { get; set; }
    [Required] public CurrencyCode Currency    { get; set; }
}

public class ReviewFraudAlertRequest
{
    [Required] public string Notes { get; set; } = string.Empty;
}

// ── Query ────────────────────────────────────────────────────────────────────

public class GetFraudAlertsQuery
{
    public string?  AccountId      { get; set; }
    public string?  RiskLevel      { get; set; }
    public bool?    IsResolved     { get; set; }
    public bool?    PaymentBlocked { get; set; }
    public DateTime? From          { get; set; }
    public DateTime? To            { get; set; }
    public int  PageNumber         { get; set; } = 1;
    public int  PageSize           { get; set; } = 20;
}
