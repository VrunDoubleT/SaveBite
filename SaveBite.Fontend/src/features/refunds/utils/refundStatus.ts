export const refundStatusLabels: Record<string, string> = {
  Overdue: "Overdue — payment required",
  AwaitingCustomerDetails: "Waiting for customer",
  PendingVerification: "Needs shop review",
  AwaitingTransfer: "Ready to transfer",
  Transferred: "Transferred",
  Completed: "Completed",
  Rejected: "Declined",
  Withdrawn: "Withdrawn",
};

// Closed requests cannot accept customer details or shop processing.
export function isRefundActive(refund: { status: string }): boolean {
  return ["AwaitingCustomerDetails", "PendingVerification", "AwaitingTransfer", "Overdue"].includes(
    refund.status,
  );
}