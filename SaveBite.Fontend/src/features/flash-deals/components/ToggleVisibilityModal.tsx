import { Eye, EyeOff, X, Loader2, ArrowRight } from "lucide-react";
import { FlashDealStatusEnum } from "../types/flashDeal.types";

export interface ToggleVisibilityTarget {
  id: string;
  productName: string;
  imageUrl?: string | null;
  productImageUrl?: string | null;
  primaryImageUrl?: string | null;
  categoryName?: string | null;
  status: FlashDealStatusEnum | number;
  minDealPrice?: number;
}

interface ToggleVisibilityModalProps {
  deal: ToggleVisibilityTarget | null;
  loading?: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

export function ToggleVisibilityModal({
  deal,
  loading = false,
  onClose,
  onConfirm,
}: ToggleVisibilityModalProps) {
  if (!deal) return null;

  const isCurrentlyHidden = deal.status === FlashDealStatusEnum.Hidden;
  const imageSrc =
    deal.imageUrl ||
    deal.productImageUrl ||
    deal.primaryImageUrl ||
    "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=120&auto=format&fit=crop&q=60";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 px-4 backdrop-blur-xs animate-in fade-in duration-150">
      <div className="w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-2xl transition-all">
        {/* Header */}
        <div className="flex items-start justify-between border-b border-neutral-100 px-6 py-5">
          <div className="flex items-center gap-3">
            <div
              className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl ${
                isCurrentlyHidden
                  ? "bg-emerald-50 text-emerald-600 ring-1 ring-emerald-500/20"
                  : "bg-amber-50 text-amber-600 ring-1 ring-amber-500/20"
              }`}
            >
              {isCurrentlyHidden ? <Eye size={22} /> : <EyeOff size={22} />}
            </div>

            <div>
              <h2 className="text-lg font-bold text-neutral-900">
                {isCurrentlyHidden ? "Unhide Flash Deal" : "Hide Flash Deal"}
              </h2>
              <p className="text-xs text-neutral-500">
                {isCurrentlyHidden
                  ? "Restore deal to customer discovery and search"
                  : "Temporarily hide deal from customer discovery"}
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
        <div className="space-y-4 px-6 py-5">
          {/* Status Transition Pill */}
          <div className="flex items-center justify-between rounded-xl border border-neutral-100 bg-neutral-50/80 p-3 text-xs">
            <div className="flex items-center gap-2">
              <span className="text-[11px] text-neutral-500">Current status:</span>
              {isCurrentlyHidden ? (
                <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-[11px] font-bold text-amber-800">
                  <EyeOff className="size-3" />
                  Hidden
                </span>
              ) : (
                <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2.5 py-0.5 text-[11px] font-bold text-emerald-800">
                  <Eye className="size-3" />
                  Active
                </span>
              )}
            </div>

            <ArrowRight className="size-4 text-neutral-400" />

            <div className="flex items-center gap-2">
              <span className="text-[11px] text-neutral-500">New status:</span>
              {isCurrentlyHidden ? (
                <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2.5 py-0.5 text-[11px] font-bold text-emerald-800">
                  <Eye className="size-3" />
                  Active
                </span>
              ) : (
                <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-[11px] font-bold text-amber-800">
                  <EyeOff className="size-3" />
                  Hidden
                </span>
              )}
            </div>
          </div>

          {/* Product Card */}
          <div className="flex items-center gap-3 rounded-xl border border-neutral-100 bg-white p-3 shadow-2xs">
            <img
              src={imageSrc}
              alt={deal.productName}
              className="size-12 rounded-lg object-cover"
            />
            <div className="min-w-0 flex-1">
              <p className="truncate font-semibold text-xs text-neutral-900">
                {deal.productName}
              </p>
              <p className="text-[11px] text-neutral-400">{deal.categoryName || "Product"}</p>
              {deal.minDealPrice != null && (
                <p className="text-[11px] font-semibold text-red-600">
                  {deal.minDealPrice.toLocaleString("vi-VN")}đ
                </p>
              )}
            </div>
          </div>

          {/* Explanation Text */}
          <div
            className={`rounded-xl border p-3.5 text-xs leading-5 ${
              isCurrentlyHidden
                ? "border-emerald-200 bg-emerald-50/60 text-emerald-900"
                : "border-amber-200 bg-amber-50/60 text-amber-900"
            }`}
          >
            {isCurrentlyHidden ? (
              <p>
                <strong>Unhiding this deal</strong> will immediately make it visible to
                customers in the SaveBite app, home feed, and search results. The deal
                will sync to Redis cache instantly.
              </p>
            ) : (
              <p>
                <strong>Hiding this deal</strong> will temporarily remove it from customer
                discovery. Customers will not be able to order this deal while hidden.
                Existing placed orders are unaffected and you can unhide it at any time.
              </p>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3 border-t border-neutral-100 bg-neutral-50 px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-xl border border-neutral-200 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 transition hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Cancel
          </button>

          <button
            type="button"
            onClick={onConfirm}
            disabled={loading}
            className={`inline-flex items-center justify-center gap-1.5 rounded-xl px-4 py-2 text-xs font-bold text-white shadow-xs transition disabled:cursor-not-allowed disabled:opacity-60 ${
              isCurrentlyHidden
                ? "bg-emerald-600 hover:bg-emerald-700"
                : "bg-amber-600 hover:bg-amber-700"
            }`}
          >
            {loading && <Loader2 className="size-3.5 animate-spin" />}
            <span>
              {loading
                ? isCurrentlyHidden
                  ? "Unhiding..."
                  : "Hiding..."
                : isCurrentlyHidden
                ? "Yes, Unhide Deal"
                : "Yes, Hide Deal"}
            </span>
          </button>
        </div>
      </div>
    </div>
  );
}
