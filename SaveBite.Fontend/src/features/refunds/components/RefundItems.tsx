import { RefundEvidenceGallery } from "./RefundEvidenceGallery";
import type { Refund } from "../types/refund.types";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";

export function RefundItems({ refund }: { refund: Refund }) {
  if (!refund.items?.length) return null;
  return (
    <section className="rounded-lg border border-neutral-200 bg-white p-3">
      <h3 className="mb-2 text-sm font-semibold">Items requested for refund</h3>
      <ul className="divide-y divide-neutral-100">
        {refund.items.map((item) => (
          <li key={item.orderItemId} className="py-3 text-sm">
            <div className="flex justify-between gap-3">
              <span>
                {item.productName}
                <span className="block text-xs text-neutral-500">
                  {item.variantName} · {item.quantity} of {item.purchasedQuantity} purchased
                </span>
              </span>
              <strong className="shrink-0">{money(item.amount)}</strong>
            </div>
            {item.reason && (
              <p className="mt-2 whitespace-pre-wrap break-words text-sm text-neutral-600">
                <span className="font-medium text-neutral-800">Reason: </span>
                {item.reason}
              </p>
            )}
            <RefundEvidenceGallery
              urls={item.evidenceUrls || []}
              label={`Evidence for ${item.productName}`}
            />
          </li>
        ))}
      </ul>
      <p className="mt-2 border-t border-neutral-100 pt-2 text-xs text-neutral-500">
        Order value after recorded refunds: {money(refund.orderNetAmount)}
      </p>
    </section>
  );
}
