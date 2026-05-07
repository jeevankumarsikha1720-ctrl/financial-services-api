namespace FinancialAPI.Domain.Enums;

public enum PaymentStatus
{
    Pending     = 1,
    Processing  = 2,
    Settled     = 3,
    Failed      = 4,
    Cancelled   = 5,
    Reversed    = 6,
    OnHold      = 7
}
