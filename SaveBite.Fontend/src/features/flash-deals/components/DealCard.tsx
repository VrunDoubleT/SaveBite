import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Clock3, ImageOff, MapPin, Package, ShoppingBag, Sparkles } from "lucide-react";
import { Badge, ButtonLink, Card } from "@/shared/components/ui";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";
import { isDealViewed, markDealAsViewed } from "@/features/flash-deals/utils/viewedDeals";

interface DealCardProps {
  deal: FlashDeal;
  isSameShop?: boolean;
}
const formatVnd = (value: number) =>
  Number.isFinite(value) ? `${Math.round(value).toLocaleString("vi-VN")} VND` : "—";

function countdown(deal: FlashDeal, soldOut: boolean) {
  if (soldOut) return "Sold out";
  const now = Date.now();
  if (new Date(deal.saleStartTime).getTime() > now) return "Upcoming";
  const diff = new Date(deal.orderEndTime).getTime() - now;
  if (diff <= 0) return "Ended";
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(minutes / 60);
  return hours > 0 ? `Ends in ${hours}h ${minutes % 60}m` : `Ends in ${minutes}m`;
}

function isDealRecentlyCreated(deal: FlashDeal) {
  // If customer has already viewed this deal's detail, it is no longer NEW
  if (isDealViewed(deal.id)) return false;

  if (deal.isNew) return true;
  if (!deal.createdAt) return false;
  const createdMs = new Date(deal.createdAt).getTime();
  if (Number.isNaN(createdMs)) return false;
  return Date.now() - createdMs < 30 * 60 * 1000;
}

export function DealCard({ deal, isSameShop = false }: DealCardProps) {
  const totalQty = deal.variants.reduce((sum, v) => sum + (v.totalQuantity || 0), 0) || 1;
  const soldQty = deal.variants.reduce((sum, v) => sum + (v.soldQuantity || 0), 0);
  const remainingQty = deal.variants.reduce((sum, v) => sum + (v.availableQuantity || 0), 0);
  const soldPct = Math.min(100, Math.round((soldQty / totalQty) * 100));
  const isDealSoldOut = deal.status === 3 || remainingQty <= 0;
  const [timeLeft, setTimeLeft] = useState(() => countdown(deal, isDealSoldOut));
  const [failedImage, setFailedImage] = useState<string | null>(null);
  const isNew = isDealRecentlyCreated(deal);

  useEffect(() => {
    const timer = setInterval(() => setTimeLeft(countdown(deal, isDealSoldOut)), 30000);
    return () => clearInterval(timer);
  }, [deal, isDealSoldOut]);

  return (
    <Card
      id={`flash-deal-card-${deal.id}`}
      className={`group flex h-full flex-col overflow-hidden !p-0 transition-all duration-500 hover:shadow-md ${
        isNew ? "ring-2 ring-emerald-500 shadow-md shadow-emerald-500/15" : ""
      }`}
    >
      <div className="relative aspect-[4/3] overflow-hidden bg-neutral-100">
        <Link
          to={`/flash-deals/${deal.id}`}
          onClick={() => markDealAsViewed(deal.id)}
          className="block h-full"
          aria-label={`View ${deal.productName}`}
        >
          {deal.productImageUrl && failedImage !== deal.productImageUrl ? (
            <img
              src={deal.productImageUrl}
              alt={deal.productName}
              loading="lazy"
              className="h-full w-full object-cover transition-transform group-hover:scale-105 motion-reduce:transform-none"
              onError={() => setFailedImage(deal.productImageUrl ?? null)}
            />
          ) : (
            <span className="flex h-full flex-col items-center justify-center gap-2 text-text-muted">
              <ImageOff className="size-8" aria-hidden="true" />
              <span className="text-caption">Image unavailable</span>
            </span>
          )}
        </Link>
        <div className="pointer-events-none absolute inset-x-3 top-3 flex flex-wrap items-start justify-between gap-2">
          <div className="flex flex-wrap items-center gap-1.5">
            {deal.maxDiscountPercent > 0 && (
              <Badge tone="warning" className="bg-accent-100 font-semibold shadow-xs">
                Save {Math.round(deal.maxDiscountPercent)}%
              </Badge>
            )}
            {isNew && (
              <span className="inline-flex items-center gap-1 rounded-full bg-red-600 px-2.5 py-0.5 text-[11px] font-black uppercase tracking-wider text-white shadow-md ring-1 ring-white/90 animate-pulse">
                <Sparkles className="size-3 shrink-0" aria-hidden="true" />
                NEW
              </span>
            )}
          </div>
        </div>
      </div>
      <div className="flex flex-1 flex-col gap-3 p-4">
        <div className="flex flex-wrap items-center gap-2 text-caption text-text-secondary">
          {deal.categoryName && (
            <span
              className="min-w-0 truncate rounded-full bg-secondary-50 px-2 py-1 font-medium text-secondary-700"
              title={deal.categoryName}
            >
              {deal.categoryName}
            </span>
          )}
          <span className="ml-auto inline-flex items-center gap-1 font-medium tabular-nums">
            <Clock3 className="size-3 shrink-0" aria-hidden="true" />
            {timeLeft}
          </span>
        </div>
        <Link
          to={`/flash-deals/${deal.id}`}
          onClick={() => markDealAsViewed(deal.id)}
          title={deal.productName}
          className="line-clamp-2 text-base font-semibold leading-snug text-text-primary hover:text-primary-700"
        >
          {deal.productName}
        </Link>
        {isSameShop ? (
          <p className="flex flex-wrap gap-3 text-caption text-text-secondary">
            <span className="inline-flex items-center gap-1">
              <Package className="size-3" aria-hidden="true" />
              {deal.variants.length} options
            </span>
            <span className="inline-flex items-center gap-1">
              <ShoppingBag className="size-3" aria-hidden="true" />
              {remainingQty} left
            </span>
          </p>
        ) : (
          <div className="flex min-w-0 items-center gap-2 text-caption text-text-secondary">
            <Link
              to={`/shops/${deal.shopId}`}
              title={deal.shopName}
              className="min-w-0 truncate hover:text-primary-700 hover:underline"
            >
              {deal.shopName}
            </Link>
            {deal.distanceInKm != null && (
              <span className="ml-auto inline-flex shrink-0 items-center gap-1">
                <MapPin className="size-3" aria-hidden="true" />
                {deal.distanceInKm} km
              </span>
            )}
          </div>
        )}
        <div className="mt-auto flex flex-wrap items-baseline gap-x-2 gap-y-1 pt-1 tabular-nums">
          <span className="text-section font-bold text-primary-700">
            {formatVnd(deal.minDealPrice)}
          </span>
          {deal.maxOriginalPrice > deal.minDealPrice && (
            <del className="text-caption text-text-muted">{formatVnd(deal.maxOriginalPrice)}</del>
          )}
        </div>
        <div className="space-y-2">
          <div
            role="meter"
            aria-label="Portions sold"
            aria-valuenow={soldQty}
            aria-valuemin={0}
            aria-valuemax={Math.max(totalQty, soldQty)}
            className="h-1 w-full overflow-hidden rounded-full bg-neutral-100"
          >
            <div className="h-full rounded-full bg-primary-400" style={{ width: `${soldPct}%` }} />
          </div>
          <p className="flex justify-between gap-2 text-caption text-text-secondary">
            <span>
              {soldQty} of {totalQty} sold
            </span>
            <span className="font-medium">{remainingQty} left</span>
          </p>
        </div>
        <ButtonLink
          to={`/flash-deals/${deal.id}`}
          onClick={() => markDealAsViewed(deal.id)}
          variant={isDealSoldOut ? "secondary" : "primary"}
          aria-label={`View deal: ${deal.productName}`}
          className="mt-1 min-h-11"
        >
          {isDealSoldOut ? "Sold out · View details" : "View deal"}
        </ButtonLink>
      </div>
    </Card>
  );
}