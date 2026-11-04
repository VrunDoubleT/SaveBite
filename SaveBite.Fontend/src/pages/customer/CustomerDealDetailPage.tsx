import { useEffect, useMemo, useState } from "react";
import { useParams, Link, useNavigate } from "react-router-dom";
import {
  Clock3,
  Store,
  ShoppingBag,
  CheckCircle2,
  ArrowLeft,
  ChevronRight,
  MapPin,
  Flame,
  Minus,
  Plus,
  Package,
  Leaf,
  Star,
} from "lucide-react";
import {
  Badge,
  Button,
  ButtonLink,
  Card,
  EmptyState,
  ErrorState,
  IconButton,
  MediaImage,
  PageHeader,
  Skeleton,
} from "@/shared/components/ui";
import { isAxiosError } from "axios";
import type { StoreReviewsSummary } from "@/features/shops/types/shop.types";
import { DealStoreReviews } from "@/features/shops/components/DealStoreReviews";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import type { FlashDeal, FlashDealVariant } from "@/features/flash-deals/types/flashDeal.types";

import { useCartStore } from "@/features/cart/stores/cartStore";
import { useAuthStore } from "@/shared/stores/authStore";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";
import { APP_PATHS } from "@/app/router/paths";

const formatVnd = (value: number) => {
  return Number.isFinite(value) ? `${Math.round(value).toLocaleString("vi-VN")}đ` : "—";
};

const formatTime = (iso?: string) => {
  if (!iso || Number.isNaN(new Date(iso).getTime())) return "Not provided";
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(iso));
};

export function CustomerDealDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [attempt, setAttempt] = useState(0);
  return (
    <DealDetailContent
      key={`${id}:${attempt}`}
      id={id}
      onRetry={() => setAttempt((value) => value + 1)}
    />
  );
}

function DealDetailContent({ id, onRetry }: { id?: string; onRetry: () => void }) {
  const navigate = useNavigate();
  const [deal, setDeal] = useState<FlashDeal | null>(null);
  const [reviewSummary, setReviewSummary] = useState<StoreReviewsSummary | null>(null);
  const [selectedVariant, setSelectedVariant] = useState<FlashDealVariant | null>(null);
  const [selectedAttributes, setSelectedAttributes] = useState<Record<string, string>>({});
  const [activeImgUrl, setActiveImgUrl] = useState<string | null>(null);
  const [quantity, setQuantity] = useState<number>(1);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [addedSuccess, setAddedSuccess] = useState<boolean>(false);
  const addCartItem = useCartStore((state) => state.addItem);
  const isAdding = useCartStore((state) => state.isMutating);

  useEffect(() => {
    if (!id) return;
    let isMounted = true;

    flashDealApi
      .getDealById(id)
      .then((data) => {
        if (isMounted) {
          setDeal(data ?? null);
          setError(null);
          if (!data) return;
          if (data.variants && data.variants.length > 0) {
            // Find default variant preference: Regular & Non-spicy (or lowest deal price)
            const defaultVar =
              data.variants.find((v) => {
                const attrs = v.attributes || {};
                const sizeVal = (attrs["Size"] || attrs["size"] || "").toLowerCase();
                const spiceVal = (
                  attrs["Spice Level"] ||
                  attrs["Độ cay"] ||
                  attrs["spice"] ||
                  ""
                ).toLowerCase();
                return (
                  (sizeVal.includes("regular") ||
                    sizeVal.includes("small") ||
                    sizeVal.includes("thường") ||
                    sizeVal.includes("nhỏ")) &&
                  (spiceVal.includes("none") ||
                    spiceVal.includes("mild") ||
                    spiceVal.includes("không"))
                );
              }) || [...data.variants].sort((a, b) => a.dealPrice - b.dealPrice)[0];

            setSelectedVariant(defaultVar);
            if (defaultVar.imageUrl) {
              setActiveImgUrl(defaultVar.imageUrl);
            } else if (data.productImageUrl) {
              setActiveImgUrl(data.productImageUrl);
            }

            // Set initial attribute values
            const initialAttrs: Record<string, string> = {};
            if (defaultVar.attributes) {
              Object.assign(initialAttrs, defaultVar.attributes);
            }
            data.attributeGroups?.forEach((g) => {
              if (!initialAttrs[g.name] && g.values.length > 0) {
                const preferred =
                  g.values.find((val) => {
                    const vLow = val.toLowerCase();
                    return (
                      vLow.includes("regular") ||
                      vLow.includes("none") ||
                      vLow.includes("thường") ||
                      vLow.includes("không")
                    );
                  }) || g.values[0];
                initialAttrs[g.name] = preferred;
              }
            });
            setSelectedAttributes(initialAttrs);
          }
        }
      })
      .catch((cause: unknown) => {
        if (isMounted) {
          if (isAxiosError(cause) && cause.response?.status === 404) setDeal(null);
          else setError("We couldn't load this deal. Please try again in a moment.");
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

  const [nowMs, setNowMs] = useState(() => Date.now());
  useEffect(() => {
    const interval = setInterval(() => setNowMs(Date.now()), 1000);
    return () => clearInterval(interval);
  }, []);
  const secondsLeft = deal
    ? Math.max(0, Math.floor((new Date(deal.orderEndTime).getTime() - nowMs) / 1000))
    : 0;
  const timeLeft = Number.isFinite(secondsLeft)
    ? [Math.floor(secondsLeft / 3600), Math.floor((secondsLeft % 3600) / 60), secondsLeft % 60]
        .map((value) => String(value).padStart(2, "0"))
        .join(":")
    : "—";

  // Gallery thumbnails list
  const galleryImages = useMemo(() => {
    if (!deal) return [];
    const list: string[] = [];
    if (deal.productImageUrl) list.push(deal.productImageUrl);
    if (deal.productImageUrls) {
      deal.productImageUrls.forEach((u) => {
        if (u && !list.includes(u)) list.push(u);
      });
    }
    deal.variants?.forEach((v) => {
      if (v.imageUrl && !list.includes(v.imageUrl)) {
        list.push(v.imageUrl);
      }
    });
    return list;
  }, [deal]);

  // Main active image
  const mainImage = activeImgUrl || galleryImages[0];

  // Retrieve sorted attribute groups (Size, Spice Level, etc.)
  const attributeGroups = useMemo(() => {
    if (!deal) return [];
    let groups: { name: string; values: string[] }[] = [];
    if (deal.attributeGroups && deal.attributeGroups.length > 0) {
      groups = deal.attributeGroups.map((g) => ({
        name: g.name,
        values: [...g.values],
      }));
    } else {
      // Analyze from variants if attributeGroups is not provided by backend
      const map = new Map<string, Set<string>>();
      deal.variants.forEach((v) => {
        if (v.attributes) {
          Object.entries(v.attributes).forEach(([k, val]) => {
            if (!map.has(k)) map.set(k, new Set());
            map.get(k)!.add(val);
          });
        }
      });
      if (map.size > 0) {
        groups = Array.from(map.entries()).map(([name, valuesSet]) => ({
          name,
          values: Array.from(valuesSet),
        }));
      }
    }

    // Sort groups: "Size" first, "Spice" next
    groups.sort((a, b) => {
      const aName = a.name.toLowerCase();
      const bName = b.name.toLowerCase();
      if (aName.includes("size")) return -1;
      if (bName.includes("size")) return 1;
      return a.name.localeCompare(b.name, "en");
    });

    // Standard sorting for attribute values:
    // Size: 'Regular' / 'Standard' before 'Large'
    // Spice: 'Non-spicy' / 'Mild' before 'Hot' / 'Spicy'
    groups.forEach((g) => {
      g.values.sort((a, b) => {
        const aLow = a.toLowerCase();
        const bLow = b.toLowerCase();
        if (
          aLow.includes("regular") ||
          aLow.includes("none") ||
          aLow.includes("thường") ||
          aLow.includes("không")
        )
          return -1;
        if (
          bLow.includes("regular") ||
          bLow.includes("none") ||
          bLow.includes("thường") ||
          bLow.includes("không")
        )
          return 1;
        return a.localeCompare(b, "en");
      });
    });

    return groups;
  }, [deal]);

  // Handle click on an attribute option (e.g. Size or Spice Level)
  const handleSelectAttribute = (groupName: string, value: string) => {
    const updatedAttrs = { ...selectedAttributes, [groupName]: value };
    setSelectedAttributes(updatedAttrs);

    if (!deal || !deal.variants) return;

    // Find best matching variant for selected attributes
    const matched =
      deal.variants.find((v) => {
        if (!v.attributes) return false;
        return Object.entries(updatedAttrs).every(([k, val]) => v.attributes?.[k] === val);
      }) ||
      deal.variants.find((v) => {
        return v.attributes?.[groupName] === value;
      }) ||
      deal.variants[0];

    if (matched) {
      setSelectedVariant(matched);
      setQuantity(1);
      // Switch active image immediately if variant has specific image
      if (matched.imageUrl) {
        setActiveImgUrl(matched.imageUrl);
      }
    }
  };

  const handleSelectVariantDirect = (v: FlashDealVariant) => {
    setSelectedVariant(v);
    setQuantity(1);
    if (v.attributes) {
      setSelectedAttributes({ ...v.attributes });
    }
    if (v.imageUrl) {
      setActiveImgUrl(v.imageUrl);
    }
  };

  const handleAddToCart = async () => {
    if (!useAuthStore.getState().isAuthenticated) {
      navigate(APP_PATHS.LOGIN, {
        state: {
          message: "Sign in to add items to your cart.",
          from: { pathname: `/flash-deals/${id}` },
        },
      });
      return;
    }
    const variant = selectedVariant || deal?.variants[0];
    if (!variant) return;
    setAddedSuccess(false);
    try {
      await addCartItem(variant.id, quantity);
      setAddedSuccess(true);
      toast.success("Added to your cart.");
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    }
  };

  if (isLoading && id) {
    return (
      <div role="status" aria-label="Loading flash deal" className="space-y-6 py-6 sm:py-8">
        <span className="sr-only">Loading flash deal</span>
        <Skeleton className="h-5 w-48" />
        <div className="grid items-start gap-6 lg:grid-cols-2">
          <div className="space-y-4">
            <Skeleton className="aspect-[4/3] w-full rounded-lg" />
            <Skeleton className="h-32" />
          </div>
          <Card className="space-y-5">
            <Skeleton className="h-6 w-24" />
            <Skeleton className="h-8 w-4/5" />
            <Skeleton className="h-16" />
            <Skeleton className="h-10 w-1/2" />
            <Skeleton className="h-24" />
            <Skeleton className="h-12" />
          </Card>
        </div>
      </div>
    );
  }
  if (error || !deal) {
    return (
      <div className="space-y-6 py-8">
        <PageHeader title="Flash deal" />
        {error ? (
          <ErrorState title="Couldn't load this deal" description={error} onRetry={onRetry} />
        ) : (
          <EmptyState
            title="This deal is no longer available"
            description="It may have ended or been removed. Discover another bite nearby."
            icon={ShoppingBag}
            action={<ButtonLink to="/">Browse nearby deals</ButtonLink>}
          />
        )}
        {error && (
          <ButtonLink to="/" variant="secondary">
            <ArrowLeft className="size-4" aria-hidden="true" />
            Back to deals
          </ButtonLink>
        )}
      </div>
    );
  }

  const activeVariant = selectedVariant || deal.variants[0];

  // Business Rule: A Flash Deal variant is purchasable only while the Deal is OnSale,
  // the current time is between SaleStartTime and OrderEndTime, the variant is Active, and its available stock is greater than zero.
  const now = new Date(nowMs);
  const saleStart = new Date(deal.saleStartTime);
  const orderEnd = new Date(deal.orderEndTime);
  const isBeforeSale = now < saleStart;
  const isAfterSale = now >= orderEnd || now >= new Date(deal.shopClosingTime);
  const isTimeValid = !isBeforeSale && !isAfterSale;
  const isDealOnSale = deal.status === 1; // 1 = OnSale
  const isVariantActive = activeVariant
    ? activeVariant.status === 0 || String(activeVariant.status) === "Active"
    : false;
  const isSoldOut = !activeVariant || activeVariant.availableQuantity <= 0;

  const isPurchasable = Boolean(
    activeVariant?.isPurchasable !== false &&
    isDealOnSale &&
    isTimeValid &&
    isVariantActive &&
    !isSoldOut,
  );

  let unpurchasableReason = "Unavailable";
  if (!isDealOnSale) {
    unpurchasableReason = deal.status === 3 ? "Sold Out" : "Unavailable";
  } else if (isBeforeSale) {
    unpurchasableReason = "Sale Starts Soon";
  } else if (isAfterSale) {
    unpurchasableReason = "Sale Ended";
  } else if (isSoldOut) {
    unpurchasableReason = "Sold Out";
  } else if (!isVariantActive) {
    unpurchasableReason = "Variant Inactive";
  }

  const isOrderingWindowOpen = isDealOnSale && isTimeValid;
  const timingLabel = !isDealOnSale
    ? "Deal status"
    : isBeforeSale
      ? "Sale starts"
      : isAfterSale
        ? "Ordering has ended"
        : "Time left to order";
  const timingValue = !isDealOnSale
    ? unpurchasableReason
    : isBeforeSale
      ? formatTime(deal.saleStartTime)
      : isAfterSale
        ? "Ended"
        : timeLeft;

  const currentPrice = activeVariant ? activeVariant.dealPrice : deal.minDealPrice;
  const originalPrice = activeVariant ? activeVariant.originalPrice : deal.maxOriginalPrice;
  const discountPercent = activeVariant ? activeVariant.discountPercent : deal.maxDiscountPercent;
  const totalQty = activeVariant?.totalQuantity ?? 0;
  const soldQty = activeVariant?.soldQuantity || 0;
  const remainingQty = activeVariant?.availableQuantity || 0;
  const soldPercent = totalQty > 0 ? Math.min(100, Math.round((soldQty / totalQty) * 100)) : 0;

  return (
    <div className="space-y-6 py-6 sm:py-8">
      <nav
        aria-label="Breadcrumb"
        className="flex min-w-0 flex-wrap items-center gap-2 text-caption text-text-secondary"
      >
        <Link
          to="/"
          className="inline-flex min-h-8 items-center gap-1 font-medium hover:text-primary-700"
        >
          All deals
        </Link>
        <ChevronRight className="size-3 shrink-0 text-text-muted" aria-hidden="true" />
        <Link
          to={`/shops/${deal.shopId}`}
          title={deal.shopName}
          className="min-w-0 max-w-64 truncate hover:text-primary-700"
        >
          {deal.shopName}
        </Link>
        <ChevronRight className="size-3 shrink-0 text-text-muted" aria-hidden="true" />
        <span
          aria-current="page"
          className="min-w-0 max-w-full truncate font-medium text-text-primary"
          title={deal.productName}
        >
          {deal.productName}
        </span>
      </nav>

      <div className="grid items-start gap-6 lg:grid-cols-2">
        <Card
          aria-label="Product gallery"
          className="min-w-0 !p-3 sm:!p-4 lg:col-start-1 lg:row-start-1"
        >
          <div className="relative overflow-hidden rounded-md">
            <MediaImage
              src={mainImage}
              alt={deal.productName}
              loading="eager"
              className="aspect-[4/3] w-full"
            />
            <div className="pointer-events-none absolute left-3 top-3 flex flex-wrap gap-2">
              <Badge tone="warning" className="bg-accent-100 font-semibold">
                <Flame className="size-3" aria-hidden="true" />
                Flash deal
              </Badge>
              {discountPercent > 0 && (
                <Badge tone="primary" className="bg-bg-surface font-semibold">
                  Save {Math.round(discountPercent)}%
                </Badge>
              )}
            </div>
          </div>
          {galleryImages.length > 1 && (
            <div role="group" aria-label="Product photos" className="mt-3 flex flex-wrap gap-2">
              {galleryImages.map((img, idx) => (
                <Button
                  key={img}
                  variant="secondary"
                  aria-label={`View product photo ${idx + 1}`}
                  aria-pressed={img === mainImage}
                  onClick={() => {
                    setActiveImgUrl(img);
                    const matchedVar = deal.variants.find((v) => v.imageUrl === img);
                    if (matchedVar) {
                      setSelectedVariant(matchedVar);
                      if (matchedVar.attributes)
                        setSelectedAttributes({ ...matchedVar.attributes });
                    }
                  }}
                  className="size-16 overflow-hidden !p-0 aria-pressed:border-primary-700 aria-pressed:ring-2 aria-pressed:ring-primary-200"
                >
                  <MediaImage
                    src={img}
                    alt=""
                    showFallbackLabel={false}
                    className="h-full w-full"
                  />
                </Button>
              ))}
            </div>
          )}
          <p className="mt-3 flex items-center gap-2 text-caption text-text-secondary">
            <Leaf className="size-4 shrink-0 text-primary-700" aria-hidden="true" />
            Discover more taste. Leave less waste.
          </p>
        </Card>

        <Card
          aria-label="Choose your deal"
          className="min-w-0 space-y-4 lg:sticky lg:top-24 lg:col-start-2 lg:row-start-1 lg:row-span-3"
        >
          <div className="space-y-2">
            <div className="flex flex-wrap items-center gap-2">
              {deal.categoryName && (
                <Badge tone="info" className="max-w-full bg-secondary-50 text-secondary-700">
                  <span className="truncate" title={deal.categoryName}>
                    {deal.categoryName}
                  </span>
                </Badge>
              )}
              <Badge tone={isPurchasable ? "primary" : "neutral"}>
                {isPurchasable ? "Available now" : unpurchasableReason}
              </Badge>
            </div>
            <h1 className="text-page font-semibold text-text-primary [overflow-wrap:anywhere]">
              {deal.productName}
            </h1>
            <div className="flex min-w-0 flex-col items-start gap-1">
              <Link
                to={`/shops/${deal.shopId}`}
                className="inline-flex min-h-8 max-w-full items-center gap-2 text-body font-medium text-text-secondary hover:text-primary-700 hover:underline"
              >
                <Store className="size-4 shrink-0 text-primary-700" aria-hidden="true" />
                <span className="truncate" title={deal.shopName}>
                  {deal.shopName}
                </span>
                <ChevronRight className="size-4 shrink-0" aria-hidden="true" />
              </Link>

              <a
                href="#reviews"
                className="inline-flex min-h-8 max-w-full flex-wrap items-center gap-2 rounded-md border border-accent-100 bg-accent-50 px-2 py-1 text-caption text-text-secondary transition-colors hover:bg-accent-100 hover:text-text-primary"
              >
                <Star className="size-4 shrink-0 text-accent-700" aria-hidden="true" />
                {reviewSummary && reviewSummary.totalReviews > 0 ? (
                  <>
                    <strong className="tabular-nums text-text-primary">
                      {reviewSummary.averageRating.toFixed(1)}
                    </strong>
                    <span>Store rating · {reviewSummary.totalReviews} reviews</span>
                  </>
                ) : (
                  <span>
                    {reviewSummary ? "No reviews yet · View reviews" : "Read customer reviews"}
                  </span>
                )}
              </a>
            </div>
          </div>

          <div
            className={`flex flex-wrap items-center justify-between gap-3 rounded-md border p-3 ${isOrderingWindowOpen ? "border-accent-100 bg-accent-50 text-accent-700" : "border-border-default bg-neutral-100 text-text-secondary"}`}
          >
            <span className="flex items-center gap-2 text-body font-medium">
              <Clock3 className="size-4" aria-hidden="true" />
              {timingLabel}
            </span>
            <span
              className="text-section font-semibold tabular-nums"
              aria-label={`${timingLabel}: ${timingValue}`}
            >
              {timingValue}
            </span>
          </div>

          <div>
            <div className="flex flex-wrap items-baseline gap-3 tabular-nums">
              <span className="text-3xl font-bold text-primary-700">{formatVnd(currentPrice)}</span>
              {originalPrice > currentPrice && (
                <del className="text-body text-text-muted">{formatVnd(originalPrice)}</del>
              )}
              {discountPercent > 0 && (
                <Badge tone="warning">Save {Math.round(discountPercent)}%</Badge>
              )}
            </div>
            <p className="mt-2 text-caption text-text-secondary">
              Price per item · In-store pickup
            </p>
          </div>

          {attributeGroups.length > 0 ? (
            <div className="space-y-3 border-t border-border-default pt-4">
              {attributeGroups.map((group) => (
                <fieldset key={group.name} className="min-w-0">
                  <legend className="mb-2 text-body font-semibold text-text-primary [overflow-wrap:anywhere]">
                    {group.name}
                  </legend>
                  <div className="flex flex-wrap gap-2">
                    {group.values.map((val) => (
                      <Button
                        key={val}
                        variant="secondary"
                        aria-pressed={selectedAttributes[group.name] === val}
                        onClick={() => handleSelectAttribute(group.name, val)}
                        className="min-h-11 max-w-full whitespace-normal text-left aria-pressed:border-primary-700 aria-pressed:bg-primary-50 aria-pressed:text-primary-700 [overflow-wrap:anywhere]"
                      >
                        {val}
                      </Button>
                    ))}
                  </div>
                </fieldset>
              ))}
            </div>
          ) : deal.variants.length > 1 ? (
            <fieldset className="min-w-0 border-t border-border-default pt-4">
              <legend className="mb-2 text-body font-semibold text-text-primary">Options</legend>
              <div className="flex flex-wrap gap-2">
                {deal.variants.map((v) => (
                  <Button
                    key={v.id}
                    variant="secondary"
                    disabled={v.availableQuantity <= 0}
                    aria-pressed={activeVariant?.id === v.id}
                    onClick={() => handleSelectVariantDirect(v)}
                    className="min-h-11 max-w-full whitespace-normal aria-pressed:border-primary-700 aria-pressed:bg-primary-50 aria-pressed:text-primary-700 [overflow-wrap:anywhere]"
                  >
                    {v.variantName || v.sku || "Default"}
                  </Button>
                ))}
              </div>
            </fieldset>
          ) : null}

          <div className="space-y-2">
            <p className="flex flex-wrap justify-between gap-2 text-caption text-text-secondary">
              <span>
                {soldQty} of {totalQty} sold
              </span>
              <span
                className={`font-semibold ${remainingQty > 0 ? "text-primary-700" : "text-text-secondary"}`}
              >
                {remainingQty > 0 && remainingQty <= 10
                  ? `Only ${remainingQty} left`
                  : `${remainingQty} left`}
              </span>
            </p>
            <div className="h-1 w-full overflow-hidden rounded-full bg-neutral-100">
              <div
                className="h-full rounded-full bg-primary-400"
                style={{ width: `${soldPercent}%` }}
              />
            </div>
          </div>

          <div className="space-y-3 border-t border-border-default pt-4">
            <div className="flex items-center justify-between gap-3">
              <span className="text-body font-semibold text-text-primary">Quantity</span>
              <div
                role="group"
                aria-label="Quantity"
                className="flex items-center gap-1 rounded-md border border-border-default bg-bg-surface p-1"
              >
                <IconButton
                  aria-label="Decrease quantity"
                  className="size-11"
                  disabled={quantity <= 1 || !isPurchasable}
                  onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                >
                  <Minus className="size-4" aria-hidden="true" />
                </IconButton>
                <output
                  aria-live="polite"
                  aria-label="Selected quantity"
                  className="min-w-8 text-center font-semibold tabular-nums text-text-primary"
                >
                  {quantity}
                </output>
                <IconButton
                  aria-label="Increase quantity"
                  className="size-11"
                  disabled={
                    !isPurchasable ||
                    Boolean(activeVariant && quantity >= activeVariant.availableQuantity)
                  }
                  onClick={() => setQuantity((q) => q + 1)}
                >
                  <Plus className="size-4" aria-hidden="true" />
                </IconButton>
              </div>
            </div>
            {addedSuccess && (
              <p
                role="status"
                className="flex items-center gap-2 rounded-md border border-primary-200 bg-primary-50 p-3 text-body font-medium text-primary-700"
              >
                <CheckCircle2 className="size-4 shrink-0" aria-hidden="true" />
                Added to your cart.
                <Link to="/cart" className="ml-auto shrink-0 underline">
                  View cart
                </Link>
              </p>
            )}
            <Button
              size="lg"
              className="w-full whitespace-normal"
              loading={isAdding}
              disabled={!isPurchasable}
              onClick={() => void handleAddToCart()}
            >
              <ShoppingBag className="size-5 shrink-0" aria-hidden="true" />
              <span>
                {isAdding
                  ? "Adding…"
                  : !isPurchasable
                    ? unpurchasableReason
                    : `Add to cart · ${formatVnd(currentPrice * quantity)}`}
              </span>
            </Button>
            {!isPurchasable && (
              <p className="text-body text-text-secondary">
                {deal.variants.length
                  ? "This selection isn't available to order right now. You can explore other nearby deals."
                  : "There are no portions available for this deal."}
              </p>
            )}
            <p className="flex items-start justify-center gap-2 text-caption text-text-secondary">
              <Package className="size-4 shrink-0 text-secondary-700" aria-hidden="true" />
              Pick up before {formatTime(deal.shopClosingTime)}
            </p>
          </div>
        </Card>

        <Card className="min-w-0 space-y-3 lg:col-start-1 lg:row-start-2">
          <h2 className="flex items-center gap-2 text-section font-semibold text-text-primary">
            <span className="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary-50 text-primary-700">
              <Leaf className="size-4" aria-hidden="true" />
            </span>
            About this bite
          </h2>
          <p className="whitespace-pre-line text-body leading-relaxed text-text-secondary [overflow-wrap:anywhere]">
            {deal.description || "The store hasn't added a description for this item yet."}
          </p>
        </Card>

        <Card className="min-w-0 space-y-4 lg:col-start-1 lg:row-start-3">
          <div className="flex items-center gap-3">
            <MediaImage
              src={deal.shopLogoUrl}
              alt=""
              fallbackIcon={Store}
              showFallbackLabel={false}
              className="size-12 shrink-0 rounded-md"
            />
            <div className="min-w-0">
              <p className="text-caption text-text-secondary">Your pickup store</p>
              <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
                {deal.shopName}
              </h2>
            </div>
          </div>
          <p className="flex items-start gap-2 text-body text-text-secondary">
            <MapPin className="mt-1 size-4 shrink-0 text-secondary-700" aria-hidden="true" />
            <span className="min-w-0 [overflow-wrap:anywhere]">
              {deal.shopAddress || "Pickup address not provided."}
              {deal.distanceInKm != null && (
                <span className="mt-1 block text-caption">{deal.distanceInKm} km away</span>
              )}
            </span>
          </p>
          <dl className="space-y-3 rounded-md bg-secondary-50 p-4 text-body">
            <div className="flex flex-wrap justify-between gap-2">
              <dt className="text-secondary-700">Order before</dt>
              <dd className="font-medium text-text-primary">{formatTime(deal.orderEndTime)}</dd>
            </div>
            <div className="flex flex-wrap justify-between gap-2">
              <dt className="text-secondary-700">Store closes</dt>
              <dd className="font-medium text-text-primary">{formatTime(deal.shopClosingTime)}</dd>
            </div>
          </dl>
          <ButtonLink to={`/shops/${deal.shopId}`} variant="secondary" className="min-h-11 w-full">
            View store
            <ChevronRight className="size-4" aria-hidden="true" />
          </ButtonLink>
        </Card>
      </div>
      <DealStoreReviews
        shopId={deal.shopId}
        shopName={deal.shopName}
        onSummary={setReviewSummary}
      />
    </div>
  );
}
