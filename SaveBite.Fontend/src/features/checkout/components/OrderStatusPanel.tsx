import { CheckCircle2, Clock3, CreditCard, Package, ShieldAlert, XCircle } from "lucide-react";
import type { CheckoutOrder } from "../types/checkout.types";

const orderStates: Record<string, { label: string; color: string; description: string }> = {
  AwaitingPayment: {
    label: "Awaiting payment",
    color: "bg-orange-100 text-orange-900",
    description: "Complete your online payment or deposit within 120 seconds to submit this order to the shop.",
  },
  Pending: {
    label: "Pending",
    color: "bg-amber-100 text-amber-900",
    description: "Waiting for the shop to confirm your order.",
  },
  Confirmed: {
    label: "Confirmed",
    color: "bg-blue-100 text-blue-900",
    description: "The shop has confirmed your order.",
  },
  Preparing: {
    label: "Preparing",
    color: "bg-violet-100 text-violet-900",
    description: "The shop is preparing your items.",
  },
  ReadyForPickup: {
    label: "Ready for pickup",
    color: "bg-emerald-100 text-emerald-900",
    description: "Your order is ready. Collect it before the pickup deadline.",
  },
  Completed: {
    label: "Completed",
    color: "bg-emerald-100 text-emerald-900",
    description: "Your order has been completed.",
  },
  Cancelled: {
    label: "Cancelled",
    color: "bg-red-100 text-red-900",
    description: "This order is cancelled. Payments and deposits already paid are non-refundable.",
  },
  Expired: {
    label: "Expired",
    color: "bg-neutral-200 text-neutral-700",
    description: "This order has expired and is no longer available for pickup.",
  },
  Rejected: {
    label: "Rejected",
    color: "bg-red-100 text-red-900",
    description: "The shop rejected this order. Collected amounts are due for a full refund.",
  },
  NoShow: {
    label: "No show",
    color: "bg-red-100 text-red-900",
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
    color: "bg-neutral-100 text-neutral-700",
    description: "Refresh the order to check its latest status.",
  };
  const paid = order.paymentStatus === "Succeeded";
  const zero = order.requiredOnlineAmount === 0;
  const payment = order.paymentReviewRequired
    ? { label: "Review required", color: "bg-red-100 text-red-900" }
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
          color: "bg-emerald-100 text-emerald-900",
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
              ? "bg-red-100 text-red-900"
              : "bg-amber-100 text-amber-900",
        };
  const PaymentIcon = order.paymentReviewRequired
    ? ShieldAlert
    : paid
      ? CheckCircle2
      : order.paymentStatus === "Cancelled" || order.paymentStatus === "Failed" || order.paymentStatus === "Expired"
        ? XCircle
        : Clock3;
  if (compact)
    return (
      <div className="flex flex-wrap gap-x-4 gap-y-2 text-xs">
        <section aria-label="Order status" className="flex flex-wrap items-center gap-1.5">
          <span className="text-neutral-500">Order</span>
          <span className={`rounded-md px-2 py-1 font-semibold ${state.color}`}>{state.label}</span>
        </section>
        <section aria-label="Payment status" className="flex flex-wrap items-center gap-1.5">
          <span className="text-neutral-500">Payment</span>
          <span
            className={`inline-flex items-center gap-1 rounded-md px-2 py-1 font-semibold ${payment.color}`}
          >
            <PaymentIcon size={12} />
            {payment.label}
          </span>
        </section>
      </div>
    );
  return (
    <div className="grid min-w-0 gap-3 sm:grid-cols-2">
      <section
        aria-label="Order status"
        className="min-w-0 rounded-xl border border-neutral-200 bg-neutral-50 p-4"
      >
        <p className="mb-2 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-neutral-500">
          <Package size={15} />
          Order status
        </p>
        <span className={`inline-flex rounded-lg px-2.5 py-1 text-sm font-bold ${state.color}`}>
          {state.label}
        </span>
        {!compact && (
          <p className="mt-2 text-sm text-neutral-600">
            {state.description}
          </p>
        )}
      </section>
      <section
        aria-label="Payment status"
        className="min-w-0 rounded-xl border border-neutral-200 bg-white p-4"
      >
        <p className="mb-2 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-neutral-500">
          <CreditCard size={15} />
          Payment status
        </p>
        <span
          className={`inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-sm font-bold ${payment.color}`}
        >
          <PaymentIcon size={15} className="shrink-0" />
          {payment.label}
        </span>
        {!compact && (
          <p className="mt-2 text-sm text-neutral-600">
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
