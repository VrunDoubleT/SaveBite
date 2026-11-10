namespace SaveBite.Backend.Models.Enums;

public enum StatementChangeType
{
    InitialIssuance,
    PartialRefundDeduction,
    FullRefundDeduction,
    PriorPeriodCreditApplied,
    PaymentPaused,
    PaymentResumed
}