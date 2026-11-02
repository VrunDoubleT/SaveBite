import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import type { Refund } from "../types/refund.types";
import { isRefundActive } from "./refundStatus";

export function refundEligibility(order: CheckoutOrder, refunds: Refund[], now: number) {
  const completed = order.status === "Completed";
  const deadline = order.completedAt ? Date.parse(order.completedAt) + 6 * 60 * 60 * 1000 : null;
  const remaining = Math.max(
    0,
    (order.paidAmount ?? 0) - refunds.reduce((sum, refund) => sum + refund.transferredAmount, 0),
  );
  const hasActive = refunds.some(isRefundActive);
  const hasRequested = refunds.some((refund) => refund.type === "CustomerRequest");
  const eligible =
    completed &&
    deadline !== null &&
    now <= deadline &&
    remaining > 0 &&
    !hasActive &&
    !hasRequested;
  const reason = !completed
    ? "Refund requests are available only for completed orders."
    : hasRequested
      ? "This order already has a refund request. A second request cannot be submitted."
      : hasActive
        ? "An active refund is already being processed for this order."
        : remaining <= 0
          ? "There is no paid balance left to refund."
          : deadline === null
            ? "The completion time is unavailable. Contact support to check refund eligibility."
            : "The 6-hour refund request window has closed.";
  return { completed, deadline, hasActive, hasRequested, eligible, reason };
}
