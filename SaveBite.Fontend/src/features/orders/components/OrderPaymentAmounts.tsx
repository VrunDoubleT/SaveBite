import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";

/** Keep the list and receipt consistent, including the existing legacy-response fallbacks. */
export function OrderPaymentAmounts({ order }: { order: CheckoutOrder }) {
  const paid =
    order.paidAmount ?? (order.paymentStatus === "Succeeded" ? order.requiredOnlineAmount : 0);
  const outstanding =
    order.outstandingAmount ??
    (["Cancelled", "Expired", "NoShow", "Rejected"].includes(order.status)
      ? 0
      : Math.max(0, order.totalAmount - paid));
  return (
    <dl aria-label="Payment amounts" className="grid grid-cols-3 gap-2">
      {[
        {
          label: "Order total",
          amount: order.totalAmount,
          tone: "bg-neutral-50 text-text-primary",
        },
        { label: "Amount paid", amount: paid, tone: "bg-primary-50 text-primary-700" },
        {
          label: "Amount due",
          amount: outstanding,
          tone:
            outstanding > 0 ? "bg-accent-50 text-accent-700" : "bg-neutral-50 text-text-primary",
        },
      ].map(({ label, amount, tone }) => (
        <div key={label} className={`min-w-0 rounded-md p-2.5 sm:p-3 ${tone}`}>
          <dt className="text-caption font-medium">{label}</dt>
          <dd className="mt-1 break-words text-body font-bold tabular-nums">{money(amount)}</dd>
        </div>
      ))}
    </dl>
  );
}
