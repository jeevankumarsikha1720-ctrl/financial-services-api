namespace FinancialAPI.Domain.Enums;

public enum NotificationType
{
    PaymentInitiated    = 1,
    PaymentSettled      = 2,
    PaymentFailed       = 3,
    PaymentOnHold       = 4,
    SettlementCompleted = 5,
    BeneficiaryAdded    = 6,
    BeneficiaryVerified = 7,
    FraudAlertRaised    = 8,
    LedgerEntryPosted   = 9,
    SystemAlert         = 10
}
