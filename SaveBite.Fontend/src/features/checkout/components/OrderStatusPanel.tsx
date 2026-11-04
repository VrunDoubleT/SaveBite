import { CheckCircle2, Clock3, CreditCard, Package, ShieldAlert, XCircle } from "lucide-react";
import type { CheckoutOrder } from "../types/checkout.types";

const orderStates: Record<string, { label: string; color: string; description: string }> = {
  AwaitingPayment: {
    label: "Awaiting payment",
    color: "bg-accent-100 text-accent-700",
    description:
      "Complete your online payment or deposit within 120 seconds to submit this order to the shop.",
  },
  Pending: {
    label: "Pending",
    color: "bg-accent-100 text-accent-700",
    description: "Waiting for the shop to confirm your order.",
  },
  Confirmed: {
    label: "Confirmed",
    color: "bg-secondary-100 text-secondary-700",
    description: "The shop has confirmed your order.",
  },
  Preparing: {
    label: "Preparing",
    color: "bg-secondary-100 text-secondary-700",
    description: "The shop is preparing your items.",
  },
  ReadyForPickup: {
    label: "Ready for pickup",
    color: "bg-primary-100 text-primary-900",
    description: "Your order is ready. Collect it before the pickup deadline.",
  },
  Completed: {
    label: "Completed",
    color: "bg-primary-100 text-primary-900",
    description: "Your order has been completed.",
  },
  Cancelled: {
    label: "Cancelled",
    color: "bg-danger-soft text-danger-strong",
    description: "This order is cancelled. Payments and deposits already paid are non-refundable.",
  },
  Expired: {
    label: "Expired",
    color: "bg-neutral-200 text-text-secondary",
    description: "This order has expired and is no longer available for pickup.",
  },
  Rejected: {
    label: "Rejected",
    color: "bg-danger-soft text-danger-strong",
    description: "The shop rejected this order. Collected amounts are due for a full refund.",
  },
  NoShow: {
    label: "No show",
    color: "bg-danger-soft text-danger-strong",
    description: "This order was marked as not collected. Customer cancellation is unavailable.",
  },
};

export function OrderStatusPanel({
  order,
  compact = false,
}: {
  order: CheckoutOrder;
  compact?: boolean;
}) {
  const state = orderStates[order.status] ?? {
    label: "Unknown",
    color: "bg-neutral-100 text-text-secondary",
    description: "Refresh the order to check its latest status.",
  };
  const paid = order.paymentStatus === "Succeeded";
  const zero = order.requiredOnlineAmount === 0;
  const payment = order.paymentReviewRequired
    ? { label: "Review required", color: "bg-danger-soft text-danger-strong" }
    : zero && !paid
      ? { label: "No online payment required", color: "bg-primary-100 text-primary-900" }
      : paid
        ? {
            label:
              (order.paidAmount ?? 0) >= order.totalAmount && order.totalAmount > 0
                ? "Paid in full"
                : zero
                  ? "No online payment required"
                  : order.paymentMethod === "PayAtShop"
                    ? "Deposit paid"
                    : "Paid in full online",
            color: "bg-primary-100 text-primary-900",
          }
        : {
            label:
              (
                {
                  Pending: "Awaiting online payment",
                  Failed: "Payment failed",
                  Cancelled: "Payment cancelled",
                  Expired: "Payment expired",
                } as Record<string, string>
              )[order.paymentStatus] ?? "Payment status unavailable",
            color:
              order.paymentStatus === "Failed" || order.paymentStatus === "Expired"
                ? "bg-danger-soft text-danger-strong"
                : "bg-accent-100 text-accent-700",
          };
  const PaymentIcon = order.paymentReviewRequired
    ? ShieldAlert
    : paid
      ? CheckCircle2
      : order.paymentStatus === "Cancelled" ||
          order.paymentStatus === "Failed" ||
          order.paymentStatus === "Expired"
        ? XCircle
        : Clock3;
  if (compact)
    return (
      <div className="flex flex-wrap gap-x-4 gap-y-2 text-caption">
        <section aria-label="Order status" className="flex flex-wrap items-center gap-1.5">
          <span className="text-text-secondary">Order</span>
          <span className={`rounded-md px-2 py-1 font-semibold ${state.color}`}>{state.label}</span>
        </section>
        <section aria-label="Payment status" className="flex flex-wrap items-center gap-1.5">
          <span className="text-text-secondary">Payment</span>
          <span
            className={`inline-flex items-center gap-1 rounded-md px-2 py-1 font-semibold ${payment.color}`}
          >
            <PaymentIcon aria-hidden="true" size={12} />
            {payment.label}
          </span>
        </section>
      </div>
    );
  return (
    <div className="grid min-w-0 gap-3 sm:grid-cols-2">
      <section
        aria-label="Order status"
        className="min-w-0 rounded-md border border-border-default bg-neutral-50 p-4"
      >
        <p className="mb-2 flex items-center gap-2 text-caption font-semibold uppercase tracking-wide text-text-secondary">
          <Package aria-hidden="true" size={15} />
          Order status
        </p>
        <span className={`inline-flex rounded-md px-2.5 py-1 text-body font-bold ${state.color}`}>
          {state.label}
        </span>
        {!compact && <p className="mt-2 text-body text-text-secondary">{state.description}</p>}
      </section>
      <section
        aria-label="Payment status"
        className="min-w-0 rounded-md border border-border-default bg-bg-surface p-4"
      >
        <p className="mb-2 flex items-center gap-2 text-caption font-semibold uppercase tracking-wide text-text-secondary">
          <CreditCard aria-hidden="true" size={15} />
          Payment status
        </p>
        <span
          className={`inline-flex items-center gap-1.5 rounded-md px-2.5 py-1 text-body font-bold ${payment.color}`}
        >
          <PaymentIcon aria-hidden="true" size={15} className="shrink-0" />
          {payment.label}
        </span>
        {!compact && (
          <p className="mt-2 text-body text-text-secondary">
            {order.paymentReviewRequired
              ? "Contact the shop to review the payment. This does not change the order status."
              : paid && order.status === "Cancelled"
                ? "Your payment remains recorded. Customer cancellations are non-refundable."
                : paid && order.paymentMethod === "PayAtShop" && order.remainingAtShop > 0
                  ? "Your online deposit is confirmed. The remaining balance is due at pickup."
                  : paid && !zero
                    ? "Online payment is confirmed. Payment confirmation is separate from shop confirmation."
                    : zero
                      ? "No online deposit is required for this order."
                      : "Payment confirmation is separate from the order's preparation and pickup status."}
          </p>
        )}
      </section>
    </div>
  );
}
