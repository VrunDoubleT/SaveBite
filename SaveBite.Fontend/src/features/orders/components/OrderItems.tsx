import { CustomerActionButton } from "@/shared/components/CustomerMutationControls";
import { CheckCircle2, Package, Star } from "lucide-react";
import { MediaImage } from "@/shared/components/ui";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";

type OrderItem = CheckoutOrder["items"][number];
export type OrderReviewTarget = {
  feedbackId: string;
  productName?: string;
  variantName?: string;
  imageUrl?: string | null;
};
export function OrderItems({
  items,
  onEditReview,
  onRate,
}: {
  items: OrderItem[];
  onEditReview: (target: OrderReviewTarget) => void;
  onRate?: (itemId: string) => void;
}) {
  return (
    <ul className="divide-y divide-border-default" aria-label="Ordered products">
      {items.map((item) => (
        <li
          key={item.flashDealVariantId}
          className="grid grid-cols-[48px_minmax(0,1fr)] gap-x-3 gap-y-2 py-4 sm:grid-cols-[48px_minmax(0,1fr)_auto]"
        >
          <MediaImage
            src={item.imageUrl}
            alt={item.productName}
            fallbackIcon={Package}
            showFallbackLabel={false}
            className="size-12 rounded-md"
          />
          <div className="min-w-0">
            <p className="text-body font-semibold text-text-primary [overflow-wrap:anywhere]">
              {item.productName}
            </p>
            {item.variantName && (
              <p className="mt-1 text-caption text-text-secondary [overflow-wrap:anywhere]">
                {item.variantName}
              </p>
            )}
            <p className="mt-1 text-caption text-text-secondary">
              Qty <span className="font-semibold text-text-primary">{item.quantity}</span> ×{" "}
              {money(item.unitPrice)}
            </p>
            <p className="mt-2 text-body font-semibold tabular-nums text-text-primary sm:hidden">
              {money(item.subtotal)}
            </p>
            {item.canReview && item.orderItemId && onRate ? (
              <CustomerActionButton
                variant="secondary"
                onClick={() => onRate(item.orderItemId)}
                className="mt-2 min-h-11 !border-accent-100 !bg-accent-50 !text-accent-700 hover:!bg-accent-100"
              >
                <Star size={15} aria-hidden="true" />
                Rate this item
              </CustomerActionButton>
            ) : item.rating ? (
              <CustomerActionButton
                variant="ghost"
                disabled={!item.feedbackId}
                onClick={() => {
                  if (item.feedbackId)
                    onEditReview({
                      feedbackId: item.feedbackId,
                      productName: item.productName,
                      variantName: item.variantName,
                      imageUrl: item.imageUrl,
                    });
                }}
                className="mt-2 min-h-11 flex-wrap justify-start !px-2 text-caption !text-primary-700 hover:!bg-primary-50"
              >
                <CheckCircle2 size={14} aria-hidden="true" />
                Reviewed
                <Star size={13} className="fill-accent-300 text-accent-700" aria-hidden="true" />
                {item.rating}/5{item.feedbackId ? " · Edit" : ""}
              </CustomerActionButton>
            ) : null}
          </div>
          <p className="hidden text-body font-semibold tabular-nums text-text-primary sm:block sm:text-right">
            {money(item.subtotal)}
          </p>
        </li>
      ))}
    </ul>
  );
}
