import { useState } from "react";
import { Link } from "react-router-dom";
import { Minus, Plus, Trash2, ImageIcon } from "lucide-react";
import type { CartItem } from "../types/cart.types";

import { formatCartMoney } from "../utils/formatCartMoney";

interface Props {
  item: CartItem;
  compact?: boolean;
  busy?: boolean;
  onQuantity?: (quantity: number) => void;
  onRemove?: () => void;
}

export function CartItemRow({ item, busy = false, onQuantity, onRemove, compact = false }: Props) {
  const [draft, setDraft] = useState<string | null>(null);
  const [quantityError, setQuantityError] = useState<string | null>(null);
  function commitQuantity() {
    if (draft === null) return;
    const quantity = Number(draft);
    setDraft(null);
    if (!Number.isSafeInteger(quantity) || quantity < 1 || quantity > item.availableQuantity) {
      setQuantityError(`Enter a quantity from 1 to ${item.availableQuantity}.`);
      return;
    }
    setQuantityError(null);
    if (quantity !== item.quantity) onQuantity?.(quantity);
  }

  return (
    <li
      className={`grid gap-x-3 border-t border-neutral-100 ${compact ? "grid-cols-[3rem_minmax(0,1fr)] gap-y-2 py-3 sm:grid-cols-[4rem_minmax(0,1fr)_auto] sm:gap-x-4" : "grid-cols-[4rem_minmax(0,1fr)] gap-y-3 py-5 sm:grid-cols-[5rem_minmax(0,1fr)_auto] sm:gap-x-5"}`}
    >
      <Link
        to={`/flash-deals/${item.flashDealId}`}
        className={`row-span-2 flex items-center justify-center overflow-hidden bg-neutral-100 ${compact ? "size-12 rounded-lg sm:size-16" : "size-16 rounded-xl sm:size-20"}`}
        aria-label={`View ${item.productName}`}
      >
        {item.imageUrl ? (
          <img
            src={item.imageUrl}
            alt={item.productName}
            className="size-full object-cover"
            loading="lazy"
          />
        ) : (
          <ImageIcon className="text-neutral-400" />
        )}
      </Link>
      <div className="col-start-2 row-start-1 min-w-0">
        <Link
          to={`/flash-deals/${item.flashDealId}`}
          className={`font-semibold text-neutral-900 hover:text-emerald-700 ${compact ? "text-sm" : ""}`}
        >
          {item.productName}
        </Link>
        <p className={`mt-1 text-neutral-500 ${compact ? "text-xs" : "text-sm"}`}>
          {item.variantName}
        </p>
        <div className={`${compact ? "mt-1" : "mt-2"} flex flex-wrap items-center gap-2 text-sm`}>
          <span className="font-bold text-emerald-700">{formatCartMoney(item.unitPrice)}</span>
          {item.originalPrice > item.unitPrice && (
            <del className="text-xs text-neutral-400">{formatCartMoney(item.originalPrice)}</del>
          )}
        </div>
        {!item.isAvailable && (
          <p role="status" className="mt-2 text-sm text-amber-800">
            {item.unavailableReason}
          </p>
        )}
      </div>
      <p
        className={`col-start-2 row-start-2 whitespace-nowrap text-sm font-bold tabular-nums text-neutral-900 sm:col-start-3 sm:row-start-1 sm:text-right ${compact ? "" : "sm:text-base"}`}
      >
        {formatCartMoney(item.subtotal)}
      </p>
      {onQuantity ? (
        <div className="col-span-2 flex min-w-0 items-center justify-between gap-3 sm:col-start-2">
          <div className="flex min-w-0 flex-wrap items-center gap-x-3 gap-y-1.5">
            <div className="grid h-10 shrink-0 grid-cols-[2.5rem_3rem_2.5rem] overflow-hidden rounded-lg border border-neutral-200 bg-white">
              <button
                type="button"
                aria-label={`Decrease quantity of ${item.productName}`}
                disabled={busy || !item.canUpdateQuantity || item.quantity <= 1}
                onClick={() => {
                  setDraft(null);
                  setQuantityError(null);
                  onQuantity(Math.min(item.quantity - 1, item.availableQuantity));
                }}
                className="flex h-full w-10 items-center justify-center hover:bg-neutral-50 disabled:opacity-30"
              >
                <Minus size={16} className="shrink-0" />
              </button>
              <input
                type="number"
                min={1}
                max={item.availableQuantity}
                step={1}
                aria-label={`Quantity of ${item.productName}`}
                value={draft ?? item.quantity}
                disabled={busy || !item.canUpdateQuantity}
                onChange={(event) => setDraft(event.target.value)}
                onBlur={commitQuantity}
                onKeyDown={(event) => {
                  if (event.key === "Enter") event.currentTarget.blur();
                }}
                className="h-full w-12 min-w-0 rounded-none border-x border-neutral-200 bg-white px-1 text-center text-sm tabular-nums [appearance:textfield] disabled:opacity-50 [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
              />
              <button
                type="button"
                aria-label={`Increase quantity of ${item.productName}`}
                disabled={
                  busy || !item.canUpdateQuantity || item.quantity >= item.availableQuantity
                }
                onClick={() => {
                  setDraft(null);
                  setQuantityError(null);
                  onQuantity(item.quantity + 1);
                }}
                className="flex h-full w-10 items-center justify-center hover:bg-neutral-50 disabled:opacity-30"
              >
                <Plus size={16} className="shrink-0" />
              </button>
            </div>
            <span className="text-xs text-neutral-500">{item.availableQuantity} available</span>
          </div>
          {onRemove && (
            <button
              type="button"
              disabled={busy}
              onClick={onRemove}
              aria-label={`Remove ${item.productName}`}
              className="inline-flex h-10 shrink-0 items-center justify-center gap-2 rounded-lg px-3 text-sm text-neutral-500 hover:bg-red-50 hover:text-red-600 disabled:opacity-40"
            >
              <Trash2 size={16} className="shrink-0" />
              <span className="hidden sm:inline">Remove</span>
            </button>
          )}
        </div>
      ) : (
        <p className="col-span-2 text-sm text-neutral-600 sm:col-start-2">
          Quantity: {item.quantity}
        </p>
      )}
      {quantityError && (
        <p role="alert" className="col-span-2 text-sm text-red-600 sm:col-start-2">
          {quantityError}
        </p>
      )}
    </li>
  );
}
