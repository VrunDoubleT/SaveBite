import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import {
  Clock,
  MapPin,
  Store,
  Star,
  ShoppingBag,
  CheckCircle2,
  ArrowLeft,
} from "lucide-react";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import type { FlashDeal, FlashDealVariant } from "@/features/flash-deals/types/flashDeal.types";

const fallbackImg =
  "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=800&auto=format&fit=crop&q=80";

const formatVnd = (value: number) => {
  return Number.isFinite(value) ? `${Math.round(value).toLocaleString("vi-VN")}đ` : "—";
};

const formatTime = (iso?: string) => {
  if (!iso) return "21:00";
  const date = new Date(iso);
  if (isNaN(date.getTime())) return "21:00";
  const h = String(date.getHours()).padStart(2, "0");
  const m = String(date.getMinutes()).padStart(2, "0");
  return `${h}:${m}`;
};

export function CustomerDealDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [deal, setDeal] = useState<FlashDeal | null>(null);
  const [selectedVariant, setSelectedVariant] = useState<FlashDealVariant | null>(null);
  const [activeImgIndex, setActiveImgIndex] = useState<number>(0);
  const [quantity, setQuantity] = useState<number>(1);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [addedSuccess, setAddedSuccess] = useState<boolean>(false);

  useEffect(() => {
    if (!id) return;
    let isMounted = true;
    setIsLoading(true);

    flashDealApi
      .getDealById(id)
      .then((data) => {
        if (isMounted) {
          setDeal(data);
          setError(null);
          if (data.variants && data.variants.length > 0) {
            setSelectedVariant(data.variants[0]);
          }
        }
      })
      .catch(() => {
        if (isMounted) {
          setError("Flash Deal not found or has expired.");
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [id]);

  // Countdown timer: HH:mm:ss
  const [timeLeft, setTimeLeft] = useState<string>("");

  useEffect(() => {
    if (!deal?.orderEndTime) return;

    const calcTime = () => {
      const diff = new Date(deal.orderEndTime).getTime() - Date.now();
      if (diff <= 0) return "Ended";
      const totalSec = Math.floor(diff / 1000);
      const h = String(Math.floor(totalSec / 3600)).padStart(2, "0");
      const m = String(Math.floor((totalSec % 3600) / 60)).padStart(2, "0");
      const s = String(totalSec % 60).padStart(2, "0");
      return `${h}:${m}:${s}`;
    };

    setTimeLeft(calcTime());
    const interval = setInterval(() => {
      setTimeLeft(calcTime());
    }, 1000);

    return () => clearInterval(interval);
  }, [deal?.orderEndTime]);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-neutral-50/50 py-12">
        <div className="mx-auto max-w-5xl px-4 animate-pulse">
          <div className="h-6 w-32 rounded bg-neutral-200 mb-6" />
          <div className="grid grid-cols-1 gap-8 md:grid-cols-2">
            <div className="aspect-square rounded-2xl bg-neutral-200" />
            <div className="space-y-4">
              <div className="h-8 w-3/4 rounded bg-neutral-200" />
              <div className="h-6 w-1/2 rounded bg-neutral-200" />
              <div className="h-24 rounded bg-neutral-200" />
              <div className="h-12 rounded bg-neutral-200" />
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error || !deal) {
    return (
      <div className="min-h-screen bg-neutral-50/50 py-16">
        <div className="mx-auto max-w-md px-4 text-center">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-red-100 text-red-600">
            <Store size={32} />
          </div>
          <h2 className="mt-4 text-xl font-bold text-neutral-900">Deal Not Found</h2>
          <p className="mt-2 text-sm text-neutral-600">
            {error ?? "This flash deal does not exist or has expired."}
          </p>
          <Link
            to="/"
            className="mt-6 inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white shadow-xs transition hover:bg-emerald-700"
          >
            <ArrowLeft size={16} />
            Back to Home
          </Link>
        </div>
      </div>
    );
  }

  const activeVariant = selectedVariant || deal.variants[0];
  const isSoldOut = !activeVariant || activeVariant.availableQuantity <= 0;
  const currentPrice = activeVariant ? activeVariant.dealPrice : deal.minDealPrice;
  const originalPrice = activeVariant ? activeVariant.originalPrice : deal.maxOriginalPrice;
  const discountPercent = activeVariant ? activeVariant.discountPercent : deal.maxDiscountPercent;
  const totalQty = activeVariant?.totalQuantity || 1;
  const soldQty = activeVariant?.soldQuantity || 0;
  const remainingQty = activeVariant?.availableQuantity || 0;
  const soldPercent = Math.min(100, Math.round((soldQty / totalQty) * 100));

  const galleryImages =
    deal.productImageUrls && deal.productImageUrls.length > 0
      ? deal.productImageUrls
      : [deal.productImageUrl || fallbackImg];

  const mainImage = galleryImages[activeImgIndex] || deal.productImageUrl || fallbackImg;

  const handleAddToCart = () => {
    setAddedSuccess(true);
    setTimeout(() => setAddedSuccess(false), 3000);
  };

  return (
    <div className="min-h-screen bg-neutral-50/50 pb-20 pt-6">
      <main className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        {/* Breadcrumb navigation */}
        <div className="mb-5 flex items-center gap-2 text-xs text-neutral-500">
          <Link to="/" className="hover:text-emerald-700">
            Home
          </Link>
          <span>/</span>
          <Link to={`/shops/${deal.shopId}`} className="hover:text-emerald-700">
            {deal.shopName}
          </Link>
          <span>/</span>
          <span className="truncate max-w-xs font-medium text-neutral-900">
            {deal.productName}
          </span>
        </div>

        {/* Main 2-column layout */}
        <div className="grid items-start gap-8 lg:grid-cols-2">
          {/* LEFT COLUMN: Image Gallery + Shop Card + Product Description */}
          <div>
            {/* Image Gallery */}
            <div className="rounded-xl border border-neutral-200 bg-white p-4 shadow-xs">
              <div className="aspect-square w-full overflow-hidden rounded-lg bg-neutral-100">
                <img
                  src={mainImage}
                  alt={deal.productName}
                  className="h-full w-full object-cover transition duration-300"
                  onError={(e) => {
                    e.currentTarget.src = fallbackImg;
                  }}
                />
              </div>

              {/* Thumbnail List */}
              {galleryImages.length > 1 && (
                <div className="mt-3 flex gap-2 overflow-x-auto pb-1">
                  {galleryImages.map((img, idx) => (
                    <button
                      key={idx}
                      type="button"
                      onClick={() => setActiveImgIndex(idx)}
                      className={`h-16 w-16 shrink-0 overflow-hidden rounded-md border-2 p-0 transition ${
                        idx === activeImgIndex
                          ? "border-emerald-600 ring-2 ring-emerald-500/20"
                          : "border-transparent opacity-75 hover:opacity-100"
                      }`}
                    >
                      <img
                        src={img}
                        alt=""
                        className="h-full w-full object-cover"
                        onError={(e) => {
                          e.currentTarget.src = fallbackImg;
                        }}
                      />
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Shop Information Card */}
            <div className="mt-4 rounded-xl border border-neutral-200 bg-white p-5 shadow-xs">
              <div className="flex items-center gap-3">
                <Link to={`/shops/${deal.shopId}`} className="shrink-0">
                  <div className="flex h-12 w-12 items-center justify-center overflow-hidden rounded-md border border-neutral-200 bg-emerald-50 text-emerald-700">
                    {deal.shopLogoUrl ? (
                      <img
                        src={deal.shopLogoUrl}
                        alt={deal.shopName}
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <Store size={24} />
                    )}
                  </div>
                </Link>

                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <Link
                      to={`/shops/${deal.shopId}`}
                      className="text-base font-semibold text-neutral-900 hover:text-emerald-700 hover:underline"
                    >
                      {deal.shopName}
                    </Link>
                    <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-semibold text-emerald-700">
                      Open Now
                    </span>
                  </div>

                  <div className="mt-0.5 flex items-center gap-1.5 text-xs text-neutral-500">
                    <span className="flex items-center gap-0.5 font-medium text-amber-500">
                      <Star size={12} className="fill-amber-400 text-amber-400" />
                      4.8
                    </span>
                    {deal.distanceInKm !== undefined && deal.distanceInKm !== null && (
                      <>
                        <span>·</span>
                        <span>{deal.distanceInKm} km away</span>
                      </>
                    )}
                    <span>·</span>
                    <span className="truncate flex items-center gap-0.5">
                      <MapPin size={11} className="shrink-0 text-emerald-600" />
                      {deal.shopAddress}
                    </span>
                  </div>
                </div>

                <Link
                  to={`/shops/${deal.shopId}`}
                  className="ml-auto shrink-0 whitespace-nowrap text-xs font-semibold text-emerald-700 hover:underline"
                >
                  View Store
                </Link>
              </div>
            </div>

            {/* Product Description */}
            <div className="mt-4 rounded-xl border border-neutral-200 bg-white p-5 shadow-xs">
              <h2 className="mb-3 text-base font-bold text-neutral-900">Product Description</h2>

              {deal.categoryName && (
                <span className="mb-3 inline-block rounded-sm bg-emerald-50 px-2.5 py-0.5 text-xs font-medium text-emerald-700">
                  {deal.categoryName}
                </span>
              )}

              <p className="m-0 text-sm leading-relaxed text-neutral-600">
                {deal.description || "No detailed description provided for this product."}
              </p>
            </div>
          </div>

          {/* RIGHT COLUMN: Sticky Buy Box */}
          <aside className="sticky top-20 rounded-xl border border-neutral-200 bg-white p-6 shadow-xs">
            {deal.categoryName && (
              <span className="mb-2.5 inline-block rounded-sm bg-emerald-50 px-2.5 py-0.5 text-xs font-medium text-emerald-700">
                {deal.categoryName}
              </span>
            )}

            <h1 className="text-xl font-bold leading-tight text-neutral-900 sm:text-2xl">
              {deal.productName}
            </h1>

            {/* Star Rating */}
            <div className="mt-2 mb-4 flex items-center gap-2 text-xs text-neutral-500">
              <div className="flex items-center gap-0.5 text-amber-500">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Star key={i} size={14} className="fill-amber-400 text-amber-400" />
                ))}
              </div>
              <span className="font-semibold text-neutral-800">4.8</span>
              <a href="#reviews" className="hover:text-emerald-700 hover:underline">
                (Customer Reviews)
              </a>
            </div>

            {/* Countdown Timer Banner */}
            <div className="mb-4 flex items-center justify-between rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-amber-900">
              <span className="flex items-center gap-2 text-xs font-semibold text-amber-800">
                <Clock size={16} className="text-amber-600" />
                Order window closes in
              </span>
              <span className="text-lg font-bold tabular-nums text-amber-700">
                {timeLeft}
              </span>
            </div>

            {/* Pricing */}
            <div className="mb-1 flex flex-wrap items-baseline gap-3">
              <span className="text-3xl font-extrabold text-emerald-700">
                {formatVnd(currentPrice)}
              </span>
              {originalPrice > currentPrice && (
                <span className="text-base text-neutral-400 line-through">
                  {formatVnd(originalPrice)}
                </span>
              )}
              {discountPercent > 0 && (
                <span className="rounded-sm bg-amber-500 px-2 py-0.5 text-xs font-bold text-white shadow-xs">
                  -{discountPercent}%
                </span>
              )}
            </div>

            <p className="mb-5 text-xs text-neutral-500">
              Price includes Flash Deal discount, applicable for in-store pickup
            </p>

            {/* Variants / Options */}
            {deal.variants.length > 0 && (
              <div className="mb-5">
                <label className="mb-2 block text-sm font-semibold text-neutral-900">
                  Options / Variants
                </label>
                <div className="flex flex-wrap gap-2">
                  {deal.variants.map((v) => {
                    const isSelected = activeVariant?.id === v.id;
                    const vSoldOut = v.availableQuantity <= 0;
                    return (
                      <button
                        key={v.id}
                        type="button"
                        disabled={vSoldOut}
                        onClick={() => {
                          setSelectedVariant(v);
                          setQuantity(1);
                        }}
                        className={`rounded-md border px-3.5 py-1.5 text-xs transition ${
                          isSelected
                            ? "border-emerald-600 bg-emerald-50 font-semibold text-emerald-800 ring-2 ring-emerald-500/20"
                            : vSoldOut
                            ? "border-neutral-200 bg-neutral-100 text-neutral-400 cursor-not-allowed"
                            : "border-neutral-200 bg-white text-neutral-700 hover:border-emerald-300"
                        }`}
                      >
                        {v.variantName || v.sku || "Default"}
                      </button>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Sales Progress Bar */}
            <div className="mb-5">
              <div className="mb-1.5 flex items-center justify-between text-xs text-neutral-600">
                <span>
                  Sold {soldQty}/{totalQty}
                </span>
                <span className="font-semibold text-amber-700">
                  {remainingQty <= 10 ? `Only ${remainingQty} left` : `${remainingQty} left`}
                </span>
              </div>
              <div className="h-2 w-full overflow-hidden rounded-full bg-neutral-100">
                <div
                  className="h-full rounded-full bg-amber-500 transition-all duration-300"
                  style={{ width: `${soldPercent}%` }}
                />
              </div>
            </div>

            {/* Quantity Selector */}
            <div className="mb-5 flex items-center justify-between">
              <span className="text-sm font-semibold text-neutral-800">Quantity</span>
              <div className="flex items-center overflow-hidden rounded-md border border-neutral-300">
                <button
                  type="button"
                  disabled={quantity <= 1 || isSoldOut}
                  onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                  className="flex h-8 w-8 items-center justify-center border-0 bg-white text-base font-semibold text-neutral-600 transition hover:bg-neutral-100 disabled:opacity-40"
                >
                  −
                </button>
                <span className="w-10 text-center text-sm font-semibold">{quantity}</span>
                <button
                  type="button"
                  disabled={isSoldOut || (activeVariant && quantity >= activeVariant.availableQuantity)}
                  onClick={() => setQuantity((q) => q + 1)}
                  className="flex h-8 w-8 items-center justify-center border-0 bg-white text-base font-semibold text-neutral-600 transition hover:bg-neutral-100 disabled:opacity-40"
                >
                  +
                </button>
              </div>
            </div>

            {/* Added to cart success notice */}
            {addedSuccess && (
              <div className="mb-4 flex items-center gap-2 rounded-lg bg-emerald-50 p-3 text-xs font-semibold text-emerald-800 border border-emerald-200">
                <CheckCircle2 size={16} className="text-emerald-600 shrink-0" />
                Added {quantity} {quantity > 1 ? "items" : "item"} to cart successfully!
              </div>
            )}

            {/* Add to Cart CTA */}
            <button
              type="button"
              disabled={isSoldOut}
              onClick={handleAddToCart}
              className={`flex w-full items-center justify-center gap-2 rounded-lg py-3.5 text-center text-sm font-bold shadow-xs transition ${
                isSoldOut
                  ? "cursor-not-allowed bg-neutral-200 text-neutral-500"
                  : "bg-emerald-600 text-white hover:bg-emerald-700"
              }`}
            >
              <ShoppingBag size={18} />
              <span>
                {isSoldOut
                  ? "Sold Out"
                  : `Add to Cart · ${formatVnd(currentPrice * quantity)}`}
              </span>
            </button>

            {/* Store closing reminder */}
            <p className="mt-3 text-center text-xs text-neutral-500">
              Store closes at {formatTime(deal.shopClosingTime)} — please pick up before closing
            </p>
          </aside>
        </div>

        {/* Customer Reviews Section */}
        <section id="reviews" className="mt-8 rounded-xl border border-neutral-200 bg-white p-6 shadow-xs">
          <h2 className="text-base font-bold text-neutral-900 mb-2">Customer Reviews</h2>
          <div className="py-8 text-center text-sm text-neutral-500 border border-dashed border-neutral-200 rounded-lg">
            No customer reviews yet for this product.
          </div>
        </section>
      </main>
    </div>
  );
}
