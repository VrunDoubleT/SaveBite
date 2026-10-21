import { AlertTriangle, X, Loader2 } from "lucide-react";

export interface CancelDealTarget {
  id: string;
  productName: string;
  imageUrl?: string | null;
  productImageUrl?: string | null;
  primaryImageUrl?: string | null;
  categoryName?: string | null;
  minDealPrice?: number;
}

interface CancelFlashDealModalProps {
  deal: CancelDealTarget | null;
  loading?: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

export function CancelFlashDealModal({
  deal,
  loading = false,
  onClose,
  onConfirm,
}: CancelFlashDealModalProps) {
  if (!deal) return null;

  const imageSrc =
    deal.imageUrl ||
    deal.productImageUrl ||
    deal.primaryImageUrl ||
    "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=120&auto=format&fit=crop&q=60";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 px-4 backdrop-blur-xs animate-in fade-in duration-150">
      <div className="w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-xl">
        {/* Header */}
        <div className="flex items-start justify-between border-b border-neutral-100 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-red-50 text-red-600">
              <AlertTriangle size={20} />
            </div>

            <div>
              <h2 className="text-lg font-bold text-neutral-900">Cancel Flash Deal</h2>
              <p className="mt-0.5 text-xs text-neutral-500">
                This action will stop the deal immediately.
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-lg p-1.5 text-neutral-400 transition hover:bg-neutral-100 hover:text-neutral-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <X size={18} />
          </button>
        </div>

        {/* Content */}
        <div className="px-6 py-5 space-y-4">
          <div className="flex items-center gap-3 rounded-xl border border-neutral-100 bg-neutral-50 p-3">
            <img
              src={imageSrc}
              alt={deal.productName}
              className="size-12 rounded-lg object-cover"
            />
            <div>
              <p className="font-semibold text-xs text-neutral-900">{deal.productName}</p>
              <p className="text-[11px] text-neutral-400">{deal.categoryName || "Product"}</p>
              {deal.minDealPrice != null && (
                <p className="text-[11px] font-medium text-red-600">
                  {deal.minDealPrice.toLocaleString("vi-VN")}đ
                </p>
              )}
            </div>
          </div>

          <p className="text-xs leading-5 text-neutral-600">
            Are you sure you want to cancel this Flash Deal for{" "}
            <span className="font-bold text-neutral-900">{deal.productName}</span>?
            Customers will no longer be able to discover or order this deal.
          </p>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3 border-t border-neutral-100 bg-neutral-50 px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-xl border border-neutral-200 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 transition hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Keep Deal
          </button>

          <button
            type="button"
            onClick={onConfirm}
            disabled={loading}
            className="inline-flex items-center justify-center gap-1.5 rounded-xl bg-red-600 px-4 py-2 text-xs font-semibold text-white shadow-xs transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {loading && <Loader2 className="size-3.5 animate-spin" />}
            <span>{loading ? "Cancelling..." : "Yes, Cancel Deal"}</span>
          </button>
        </div>
      </div>
    </div>
  );
}
