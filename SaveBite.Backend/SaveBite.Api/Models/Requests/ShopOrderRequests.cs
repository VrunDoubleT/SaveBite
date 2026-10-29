namespace SaveBite.Backend.Models.Requests;

// Supply the reason required when the shop rejects a pending order.
public sealed record RejectShopOrderRequest(string Reason);
// Request the next forward-only preparation state.
public sealed record PreparationStatusRequest(string Status);
// Supply the customer's bank destination before email verification.
public sealed record RefundDestinationRequest(string BankName, string AccountNumber, string AccountHolder);
// Submit the email code used to confirm a refund destination.
public sealed record VerifyRefundDestinationRequest(string Code);
// Record the reference of a manually completed refund transfer.
public sealed record RecordRefundTransferRequest(string TransferReference, string? ProofUrl = null);

// Indicate whether the shop has received the remaining balance in cash at handover.
public sealed record CompleteShopOrderRequest(bool CashReceived = false);
