import { useEffect, useMemo, useState } from "react";
import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  ChevronDown,
  ChevronUp,
  Clock,
  MapPin,
  MessageCircle,
  Star,
  Store,
  CheckCircle2,
} from "lucide-react";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import { shopApi } from "@/features/shops/api/shopApi";
import { ShopDealCard } from "@/features/flash-deals/components/ShopDealCard";
import { StoreReviewList } from "@/features/shops/components/StoreReviewList";
import { Pagination, usePagination } from "@/shared/components/pagination";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";
import type { ShopProfile, StoreReviewsSummary } from "@/features/shops/types/shop.types";

// Default shop id for fallback/preview if accessed without id
const DEFAULT_PREVIEW_SHOP_ID = "00000000-0000-0000-0000-000000000022";

export function CustomerShopPage() {
  const { id } = useParams<{ id: string }>();
  const shopId = id || DEFAULT_PREVIEW_SHOP_ID;

  // Shop Profile state
  const [profile, setProfile] = useState<ShopProfile | null>(null);
  const [isProfileLoading, setIsProfileLoading] = useState(true);

  // Deals state
  const [deals, setDeals] = useState<FlashDeal[]>([]);
  const [isDealsLoading, setIsDealsLoading] = useState(true);

  // Reviews state
  const [reviewsSummary, setReviewsSummary] = useState<StoreReviewsSummary | null>(null);
  const [isReviewsLoading, setIsReviewsLoading] = useState(false);
  const [reviewRatingFilter, setReviewRatingFilter] = useState<number | null>(null);
  const [reviewHasImagesOnly, setReviewHasImagesOnly] = useState(false);
  const [reviewPage, setReviewPage] = useState(1);

  // UI state
  const [activeTab, setActiveTab] = useState<"deals" | "reviews">("deals");
  const [isStoreInfoExpanded, setIsStoreInfoExpanded] = useState(false);
  const [cartToast, setCartToast] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // 1. Fetch Shop Profile
  useEffect(() => {
    let isMounted = true;
    setIsProfileLoading(true);

    shopApi
      .getShopProfile(shopId)
      .then((data) => {
        if (isMounted) {
          setProfile(data);
        }
      })
      .catch(() => {
        // Fallback gracefully if shop profile endpoint has issues
      })
      .finally(() => {
        if (isMounted) setIsProfileLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [shopId]);

  // 2. Fetch Shop Deals
  useEffect(() => {
    let isMounted = true;
    setIsDealsLoading(true);

    flashDealApi
      .getDealsByShop(shopId)
      .then((data) => {
        if (isMounted) {
          setDeals(data);
          setError(null);
        }
      })
      .catch(() => {
        if (isMounted) {
          setError("Không thể tải danh sách ưu đãi của cửa hàng.");
        }
      })
      .finally(() => {
        if (isMounted) setIsDealsLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [shopId]);

  // 3. Fetch Store Reviews
  useEffect(() => {
    let isMounted = true;
    setIsReviewsLoading(true);

    shopApi
      .getStoreReviews(shopId, {
        rating: reviewRatingFilter ?? undefined,
        page: reviewPage,
        pageSize: 10,
      })
      .then((data) => {
        if (isMounted) {
          setReviewsSummary(data);
        }
      })
      .catch(() => {
        // Fallback gracefully
      })
      .finally(() => {
        if (isMounted) setIsReviewsLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [shopId, reviewRatingFilter, reviewPage]);

  // Compute Display Attributes
  const shopName = profile?.name || deals[0]?.shopName || "Bánh Mì Cô Ba";
  const shopAddress = profile
    ? [profile.addressLine, profile.ward, profile.district, profile.city].filter(Boolean).join(", ")
    : deals[0]?.shopAddress || "45 Đường Nguyễn Trãi, Phường Tân An, Ninh Kiều, Cần Thơ";
  const shopLogo = profile?.logoUrl || deals[0]?.shopLogoUrl;
  const coverImage =
    profile?.coverImageUrl ||
    "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=1600&auto=format&fit=crop&q=80";

  // Operating Hours display: e.g. "06:00 - 21:00"
  const operatingHours =
    profile?.openingTime && profile?.closingTime
      ? `${String(profile.openingTime).slice(0, 5)} - ${String(profile.closingTime).slice(0, 5)}`
      : "06:00 - 21:00";

  // Rating and review count
  const averageRating = profile?.averageRating || reviewsSummary?.averageRating || 4.8;
  const totalReviewsCount = profile?.totalReviews || reviewsSummary?.totalReviews || (deals.length > 0 ? 356 : 0);

  // Store categories
  const shopCategories = useMemo(() => {
    const set = new Set<string>();
    for (const d of deals) {
      if (d.categoryName) set.add(d.categoryName);
    }
    return set.size > 0 ? Array.from(set) : ["Ăn vặt", "Đồ uống"];
  }, [deals]);

  // Deal Pagination (8 per page)
  const {
    paginatedItems: paginatedDeals,
    currentPage: dealsPage,
    setCurrentPage: setDealsPage,
    totalPages: dealsTotalPages,
    pageSize: dealsPageSize,
    totalItems: dealsTotalItems,
  } = usePagination({
    items: deals,
    initialPageSize: 8,
  });

  const handleAddToCart = (deal: FlashDeal) => {
    setCartToast(`Added "${deal.productName}" to cart!`);
    setTimeout(() => {
      setCartToast(null);
    }, 2800);
  };

  return (
    <div className="min-h-screen bg-neutral-50/50 pb-20 pt-5">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Back Link */}
        <Link
          to="/"
          className="mb-4 inline-flex items-center gap-1.5 text-xs font-semibold text-neutral-500 transition hover:text-emerald-700"
        >
          <ArrowLeft size={15} />
          <span>Back to home</span>
        </Link>

        {/* Added to cart toast notification */}
        {cartToast && (
          <div className="fixed bottom-6 right-6 z-50 flex items-center gap-2 rounded-xl bg-neutral-900/95 px-4 py-3 text-xs font-semibold text-white shadow-xl backdrop-blur-md animate-fade-in">
            <CheckCircle2 size={16} className="text-emerald-400 shrink-0" />
            <span>{cartToast}</span>
          </div>
        )}

        {/* 1. STORE HEADER BANNER CARD (Matching Image 1) */}
        <section className={`overflow-hidden rounded-2xl border border-neutral-200/90 bg-white shadow-xs ${isProfileLoading ? "animate-pulse" : ""}`}>
          {/* Panoramic Cover Image */}
          <div className="relative h-44 w-full overflow-hidden bg-neutral-900 sm:h-56 md:h-64">
            <img
              src={coverImage}
              alt={shopName}
              className="h-full w-full object-cover object-center"
            />
            <div className="absolute inset-0 bg-linear-to-t from-black/40 via-transparent to-black/20" />

            {/* Top Right Status Badge: • Open / Closed */}
            <div className="absolute right-4 top-4 flex items-center gap-1.5 rounded-full bg-white/95 px-3 py-1 text-xs font-bold text-emerald-800 shadow-sm backdrop-blur-xs">
              <span className={`h-2 w-2 rounded-full ${profile?.isOpen !== false ? "bg-emerald-500 animate-pulse" : "bg-neutral-400"}`} />
              <span>{profile?.isOpen !== false ? "Open" : "Closed"}</span>
            </div>
          </div>

          {/* Shop Profile Details */}
          <div className="relative px-5 pb-6 pt-2 sm:px-8">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              {/* Left Column: Avatar + Store Info */}
              <div className="flex flex-col items-start gap-4 sm:flex-row sm:items-start">
                {/* Store Avatar / Logo */}
                <div className="-mt-12 flex h-20 w-20 shrink-0 items-center justify-center overflow-hidden rounded-2xl border-4 border-white bg-white shadow-md sm:-mt-14 sm:h-24 sm:w-24">
                  {shopLogo ? (
                    <img
                      src={shopLogo}
                      alt={shopName}
                      className="h-full w-full object-cover"
                      onError={(e) => {
                        e.currentTarget.style.display = "none";
                      }}
                    />
                  ) : (
                    <div className="flex h-full w-full items-center justify-center bg-emerald-50 text-emerald-700">
                      <Store size={36} />
                    </div>
                  )}
                </div>

                {/* Name, Category Tags & Meta Details */}
                <div>
                  <h1 className="text-2xl font-extrabold tracking-tight text-neutral-900 sm:text-3xl">
                    {shopName}
                  </h1>

                  {/* Category Tags */}
                  <div className="mt-2 flex flex-wrap items-center gap-1.5">
                    {shopCategories.map((cat) => (
                      <span
                        key={cat}
                        className="rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs font-semibold text-emerald-700"
                      >
                        {cat}
                      </span>
                    ))}
                  </div>

                  {/* Metadata: Address • Hours • Response rate */}
                  <div className="mt-2.5 flex flex-wrap items-center gap-y-1.5 gap-x-4 text-xs text-neutral-600 sm:text-sm">
                    <span className="flex items-center gap-1 text-neutral-700">
                      <MapPin size={15} className="text-emerald-600 shrink-0" />
                      <span>{shopAddress}</span>
                    </span>

                    <span className="flex items-center gap-1 text-neutral-700">
                      <Clock size={15} className="text-emerald-600 shrink-0" />
                      <span>{operatingHours}</span>
                    </span>

                    <span className="flex items-center gap-1 text-neutral-700">
                      <MessageCircle size={15} className="text-emerald-600 shrink-0" />
                      <span>96% Response Rate</span>
                    </span>
                  </div>

                  {/* Expandable trigger: View more store info */}
                  <button
                    type="button"
                    onClick={() => setIsStoreInfoExpanded(!isStoreInfoExpanded)}
                    className="mt-2.5 inline-flex items-center gap-1 text-xs font-bold text-emerald-700 transition hover:text-emerald-800"
                  >
                    <span>{isStoreInfoExpanded ? "Hide store info" : "View more store info"}</span>
                    {isStoreInfoExpanded ? (
                      <ChevronUp size={14} />
                    ) : (
                      <ChevronDown size={14} />
                    )}
                  </button>
                </div>
              </div>

              {/* Right Column: Rating Block (Matching Image 1) */}
              <div className="flex items-center gap-3 sm:flex-col sm:items-end sm:pt-2">
                <div className="flex items-center gap-3">
                  <span className="text-3xl font-extrabold text-neutral-900">
                    {averageRating}
                  </span>
                  <div>
                    <div className="flex text-amber-400">
                      {[...Array(5)].map((_, i) => (
                        <Star
                          key={i}
                          size={16}
                          className="fill-amber-400 text-amber-400"
                        />
                      ))}
                    </div>
                    <span className="text-xs text-neutral-500">
                      {totalReviewsCount} reviews
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Expandable Store Details Drawer */}
            {isStoreInfoExpanded && (
              <div className="mt-4 rounded-xl border border-neutral-200/80 bg-neutral-50 p-4 text-xs text-neutral-600 sm:text-sm animate-fade-in">
                <h3 className="font-bold text-neutral-900">About Store</h3>
                <p className="mt-1 leading-relaxed text-neutral-600">
                  {profile?.description ||
                    deals[0]?.description ||
                    `${shopName} offers fresh, delicious daily meals with special recipes. Joining SaveBite brings quality meals to customers with exclusive end-of-day discounts while helping reduce food waste.`}
                </p>
                <div className="mt-3 grid grid-cols-1 gap-2 pt-3 border-t border-neutral-200/60 sm:grid-cols-2 text-xs">
                  <div>
                    <span className="font-semibold text-neutral-700">Full address:</span>{" "}
                    {shopAddress}
                  </div>
                  <div>
                    <span className="font-semibold text-neutral-700">Opening hours:</span>{" "}
                    {operatingHours}
                  </div>
                </div>
              </div>
            )}
          </div>
        </section>

        {/* 2. TABS: Flash Deals (N) / Reviews (N) (Matching Image 1 & 2) */}
        <div className="mt-6 flex items-center gap-8 border-b border-neutral-200 text-sm font-semibold">
          <button
            type="button"
            onClick={() => setActiveTab("deals")}
            className={`flex items-center gap-1.5 pb-3 transition ${
              activeTab === "deals"
                ? "border-b-2 border-emerald-600 font-bold text-emerald-700"
                : "text-neutral-500 hover:text-neutral-800"
            }`}
          >
            <span>Flash Deals</span>
            <span className="text-xs">({deals.length})</span>
          </button>

          <button
            type="button"
            onClick={() => setActiveTab("reviews")}
            className={`flex items-center gap-1.5 pb-3 transition ${
              activeTab === "reviews"
                ? "border-b-2 border-emerald-600 font-bold text-emerald-700"
                : "text-neutral-500 hover:text-neutral-800"
            }`}
          >
            <span>Reviews</span>
            <span className="text-xs">
              ({reviewsSummary?.totalReviews ?? totalReviewsCount})
            </span>
          </button>
        </div>

        {/* 3. TAB CONTENT */}
        <div className="mt-6">
          {activeTab === "deals" ? (
            /* TAB 1: FLASH DEALS 4-COLUMN GRID (Matching Image 1) */
            <div>
              {error && (
                <div className="mb-6 rounded-xl border border-red-200 bg-red-50 p-4 text-xs font-medium text-red-700">
                  {error}
                </div>
              )}

              {/* Loading Skeleton */}
              {isDealsLoading && (
                <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
                  {Array.from({ length: 4 }).map((_, i) => (
                    <div
                      key={i}
                      className="animate-pulse rounded-2xl border border-neutral-200 bg-white p-4"
                    >
                      <div className="aspect-[4/3] w-full rounded-xl bg-neutral-200" />
                      <div className="mt-3 h-4 w-1/3 rounded bg-neutral-200" />
                      <div className="mt-2 h-5 w-3/4 rounded bg-neutral-200" />
                      <div className="mt-3 h-6 w-1/2 rounded bg-neutral-200" />
                      <div className="mt-3 h-10 w-full rounded-xl bg-neutral-200" />
                    </div>
                  ))}
                </div>
              )}

              {/* Empty State */}
              {!isDealsLoading && deals.length === 0 && (
                <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-neutral-300 bg-white px-6 py-16 text-center shadow-xs">
                  <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                    <Store size={32} />
                  </div>
                  <h3 className="mt-4 text-base font-bold text-neutral-800">
                    No Flash Deals Available
                  </h3>
                  <p className="mt-1 text-xs text-neutral-500 max-w-sm">
                    This store currently has no active flash deals. Please check back later!
                  </p>
                  <Link
                    to="/"
                    className="mt-5 rounded-xl bg-emerald-600 px-5 py-2.5 text-xs font-semibold text-white shadow-xs transition hover:bg-emerald-700"
                  >
                    Explore other stores
                  </Link>
                </div>
              )}

              {/* 4 COLUMNS DEAL GRID */}
              {!isDealsLoading && deals.length > 0 && (
                <div>
                  <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
                    {paginatedDeals.map((deal) => (
                      <ShopDealCard
                        key={deal.id}
                        deal={deal}
                        onAddToCart={handleAddToCart}
                      />
                    ))}
                  </div>

                  {/* Pagination if deals > 8 */}
                  {dealsTotalPages > 1 && (
                    <div className="mt-8">
                      <Pagination
                        currentPage={dealsPage}
                        totalPages={dealsTotalPages}
                        onPageChange={setDealsPage}
                        totalItems={dealsTotalItems}
                        pageSize={dealsPageSize}
                        itemLabel="deals"
                        showTotalItems={true}
                      />
                    </div>
                  )}
                </div>
              )}
            </div>
          ) : (
            /* TAB 2: STORE REVIEWS WITH RATING FILTER PILLS & REVIEWS LIST (Matching Image 2) */
            <div className="rounded-2xl border border-neutral-200/90 bg-white p-6 shadow-xs">
              <StoreReviewList
                summary={reviewsSummary}
                isLoading={isReviewsLoading}
                activeRatingFilter={reviewRatingFilter}
                onRatingFilterChange={setReviewRatingFilter}
                hasImagesOnly={reviewHasImagesOnly}
                onHasImagesFilterChange={setReviewHasImagesOnly}
                currentPage={reviewPage}
                onPageChange={setReviewPage}
              />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
