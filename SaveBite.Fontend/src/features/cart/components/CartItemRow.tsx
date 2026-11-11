import { useId, useState } from "react";
import { Link } from "react-router-dom";
import { Minus, Plus, Trash2, Clock3, CircleAlert } from "lucide-react";
import { Badge, Button, IconButton, Input, MediaImage } from "@/shared/components/ui";
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
  const errorId = useId();
  function commitQuantity() {
    if (draft === null) return;
    let quantity = Number(draft);
    setDraft(null);
    if (!Number.isSafeInteger(quantity) || quantity < 1) {
      quantity = 1;
    } else if (quantity > item.availableQuantity) {
      quantity = item.availableQuantity;
    }
    setQuantityError(null);
    if (quantity !== item.quantity) onQuantity?.(quantity);
  }
  const endDate = new Date(item.orderEndTime);
  const deadline = Number.isNaN(endDate.getTime())
    ? null
    : new Intl.DateTimeFormat("en-GB", {
        day: "numeric",
        month: "short",
        hour: "2-digit",
        minute: "2-digit",
      }).format(endDate);

  return (
    <li
      className={`grid min-w-0 grid-cols-[4rem_minmax(0,1fr)] gap-x-3 gap-y-3 border-b border-border-default last:border-b-0 sm:grid-cols-[4rem_minmax(0,1fr)_auto] sm:gap-x-4 ${compact ? "py-4" : "py-5"}`}
    >
      <Link
        to={`/flash-deals/${item.flashDealId}`}
        className="row-span-2 block size-16 overflow-hidden rounded-md"
        aria-label={`View ${item.productName}`}
      >
        <MediaImage
          src={item.imageUrl ?? undefined}
          alt=""
          showFallbackLabel={false}
          className="h-full w-full"
        />
      </Link>
      <div className="col-start-2 row-start-1 min-w-0 space-y-2">
        <h3 className="text-body font-semibold text-text-primary">
          <Link
            to={`/flash-deals/${item.flashDealId}`}
            title={item.productName}
            className="line-clamp-2 hover:text-primary-700 [overflow-wrap:anywhere]"
          >
            {item.productName}
          </Link>
        </h3>
        {item.variantName && (
          <Badge className="max-w-full bg-secondary-50 text-secondary-700">
            <span className="truncate" title={item.variantName}>
              {item.variantName}
            </span>
          </Badge>
        )}
        <p className="flex flex-wrap items-baseline gap-2 text-body tabular-nums">
          <span className="font-semibold text-primary-700">{formatCartMoney(item.unitPrice)}</span>
          {item.originalPrice > item.unitPrice && (
            <del className="text-caption text-text-muted">
              {formatCartMoney(item.originalPrice)}
            </del>
          )}
          <span className="text-caption text-text-secondary">/ item</span>
        </p>
        {onQuantity && deadline && (
          <p className="flex flex-wrap items-center gap-1 text-caption text-text-secondary">
            <Clock3 className="size-3 shrink-0 text-accent-700" aria-hidden="true" />
            <span>
              Order by <time dateTime={item.orderEndTime}>{deadline}</time>
            </span>
          </p>
        )}
        {!item.isAvailable && (
          <p
            role="status"
            className="flex items-start gap-2 rounded-md bg-accent-50 p-2 text-body text-accent-700 [overflow-wrap:anywhere]"
          >
            <CircleAlert className="mt-1 size-4 shrink-0" aria-hidden="true" />
            {item.unavailableReason || "This item is currently unavailable."}
          </p>
        )}
      </div>
      <div className="col-start-2 row-start-2 min-w-0 sm:col-start-3 sm:row-start-1 sm:text-right">
        <p className="text-caption text-text-secondary">Item total</p>
        <p className="mt-1 break-words text-body font-semibold tabular-nums text-text-primary">
          {formatCartMoney(item.subtotal)}
        </p>
      </div>
      {onQuantity ? (
        <div className="col-span-2 flex min-w-0 flex-wrap items-center justify-between gap-3 sm:col-start-2">
          <div className="flex min-w-0 flex-wrap items-center gap-2">
            <div className="flex shrink-0 items-center overflow-hidden rounded-md border border-border-default bg-bg-surface">
              <IconButton
                aria-label={`Decrease quantity of ${item.productName}`}
                className="size-11 !rounded-none"
                disabled={busy || !item.canUpdateQuantity || item.quantity <= 1}
                onClick={() => {
                  setDraft(null);
                  setQuantityError(null);
                  onQuantity(Math.min(item.quantity - 1, item.availableQuantity));
                }}
              >
                <Minus className="size-4" aria-hidden="true" />
              </IconButton>
              <Input
                type="number"
                min={1}
                max={item.availableQuantity}
                step={1}
                aria-label={`Quantity of ${item.productName}`}
                aria-invalid={Boolean(quantityError)}
                aria-describedby={quantityError ? errorId : undefined}
                value={draft ?? item.quantity}
                disabled={busy || !item.canUpdateQuantity}
                onChange={(event) => {
                  const raw = event.target.value;
                  if (raw === "") {
                    setDraft("");
                    return;
                  }
                  const num = parseInt(raw, 10);
                  if (!Number.isNaN(num) && num > item.availableQuantity) {
                    setDraft(String(item.availableQuantity));
                    setQuantityError(null);
                    if (item.availableQuantity !== item.quantity) {
                      onQuantity?.(item.availableQuantity);
                    }
                    return;
                  }
                  setDraft(raw);
                }}
                onBlur={commitQuantity}
                onKeyDown={(event) => {
                  if (event.key === "Enter") event.currentTarget.blur();
                }}
                className="min-h-11 !w-12 !rounded-none !border-y-0 px-1 text-center tabular-nums [appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
              />
              <IconButton
                aria-label={`Increase quantity of ${item.productName}`}
                className="size-11 !rounded-none"
                disabled={
                  busy || !item.canUpdateQuantity || item.quantity >= item.availableQuantity
                }
                onClick={() => {
                  setDraft(null);
                  setQuantityError(null);
                  onQuantity(item.quantity + 1);
                }}
              >
                <Plus className="size-4" aria-hidden="true" />
              </IconButton>
            </div>
            <span className="text-caption text-text-secondary">
              {item.availableQuantity} available
            </span>
          </div>
          {onRemove && (
            <Button
              variant="ghost"
              className="min-h-11 px-3 hover:bg-danger-soft hover:text-danger-strong"
              disabled={busy}
              onClick={onRemove}
              aria-label={`Remove ${item.productName}`}
            >
              <Trash2 className="size-4 shrink-0" aria-hidden="true" />
              <span className="hidden sm:inline">Remove</span>
            </Button>
          )}
        </div>
      ) : (
        <p className="col-span-2 text-body text-text-secondary sm:col-start-2">
          Quantity:{" "}
          <strong className="font-medium tabular-nums text-text-primary">{item.quantity}</strong>
        </p>
      )}
      {quantityError && (
        <p
          id={errorId}
          role="alert"
          className="col-span-2 text-body text-danger-strong sm:col-start-2"
        >
          {quantityError}
        </p>
      )}
    </li>
  );
}
