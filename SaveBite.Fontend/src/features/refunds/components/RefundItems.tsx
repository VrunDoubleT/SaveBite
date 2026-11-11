import { RefundEvidenceGallery } from "./RefundEvidenceGallery";
import type { Refund } from "../types/refund.types";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";

export function RefundItems({ refund }: { refund: Refund }) {
  if (!refund.items?.length) return null;
  return (
    <section className="min-w-0 rounded-lg border border-border-default bg-bg-surface p-4 shadow-sm sm:p-5">
      <h3 className="mb-2 text-body font-semibold">Items requested for refund</h3>
      <ul className="divide-y divide-border-default">
        {refund.items.map((item) => (
          <li key={item.orderItemId} className="py-2 text-body">
            <div className="flex flex-wrap justify-between gap-3">
              <span className="min-w-0 flex-1 [overflow-wrap:anywhere]">
                {item.productName}
                <span className="block text-caption text-text-secondary">
                  {item.variantName} · {item.quantity} of {item.purchasedQuantity} purchased
                </span>
              </span>
              <strong className="break-words tabular-nums text-primary-700">
                {money(item.amount)}
              </strong>
            </div>
            {item.reason && (
              <p className="mt-2 whitespace-pre-wrap break-words text-body text-text-secondary">
                <span className="font-medium text-text-primary">Reason: </span>
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
      <p className="mt-2 border-t border-border-default pt-2 text-caption text-text-secondary">
        Order value after recorded refunds: {money(refund.orderNetAmount)}
      </p>
    </section>
  );
}
