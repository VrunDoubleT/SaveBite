import { useEffect, useMemo, useState } from "react";
import { useParams, Link, useNavigate } from "react-router-dom";
import {
  ArrowLeft,
  ChevronDown,
  ChevronUp,
  ChevronRight,
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

const defaultStoreCover =
  "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=1600&auto=format&fit=crop&q=80";

export function CustomerShopPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const shopId = id;

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
    if (!shopId) {
      setIsProfileLoading(false);
      return;
    }

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
    if (!shopId) {
      setIsDealsLoading(false);
      return;
    }

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
          setError("Failed to load store flash deals.");
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
    if (!shopId) {
      setIsReviewsLoading(false);
      return;
    }

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

  // Compute Display Attributes strictly from real API data
  const shopName = profile?.name || "";
  const shopAddress = profile
    ? [profile.addressLine, profile.ward, profile.district, profile.city].filter(Boolean).join(", ")
    : "";
  const shopLogo = profile?.logoUrl || "";
  const coverImage = profile?.coverImageUrl || "";

  // Operating Hours display: e.g. "06:00 - 21:00"
  const operatingHours =
    profile?.openingTime && profile?.closingTime
      ? `${String(profile.openingTime).slice(0, 5)} - ${String(profile.closingTime).slice(0, 5)}`
      : "";

  // Rating and review count
  const averageRating = profile?.averageRating ?? 0;
  const totalReviewsCount = profile?.totalReviews ?? 0;

  // Store categories
  const shopCategories = useMemo(() => {
    const set = new Set<string>();
    for (const d of deals) {
      if (d.categoryName) set.add(d.categoryName);
    }
    return Array.from(set);
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

  if (!shopId || (!isProfileLoading && !profile)) {
    return (
      <div className="flex min-h-[70vh] flex-col items-center justify-center p-6 text-center">
        <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-neutral-100 text-neutral-400 mb-4">
          <Store size={32} />
        </div>
        <h2 className="text-xl font-bold text-neutral-900">Store Not Found</h2>
        <p className="mt-1 max-w-sm text-sm text-neutral-500">
          The store you are looking for does not exist, has been removed, or is temporarily inactive.
        </p>
        <Link
          to="/"
          className="mt-6 inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-5 py-2.5 text-xs font-bold text-white shadow-xs transition hover:bg-emerald-700"
        >
          <ArrowLeft size={14} /> Back to Home
        </Link>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-neutral-50/50 pb-20 pt-5">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Thanh Điều Hướng: Nút Quay Lại & Breadcrumb gọn gàng */}
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => {
                if (window.history.length > 1) {
                  navigate(-1);
                } else {
                  navigate("/");
                }
              }}
              className="group inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-3.5 py-1.5 text-xs sm:text-sm font-semibold text-neutral-700 shadow-2xs transition hover:border-emerald-500 hover:bg-emerald-50/60 hover:text-emerald-700 cursor-pointer"
            >
              <ArrowLeft
                size={14}
                className="text-neutral-500 transition-transform group-hover:-translate-x-0.5 group-hover:text-emerald-600"
              />
              <span>Quay lại</span>
            </button>

            <nav
              aria-label="Breadcrumb"
              className="hidden sm:flex items-center gap-1.5 text-xs text-neutral-500"
            >
              <Link to="/" className="hover:text-emerald-700 transition">
                Trang chủ
              </Link>
              <ChevronRight size={13} className="text-neutral-300 shrink-0" />
              <span className="truncate max-w-[240px] font-semibold text-neutral-800">
                {shopName || "Chi tiết cửa hàng"}
              </span>
            </nav>
          </div>
        </div>

        {/* Added to cart toast notification */}
        {cartToast && (
          <div className="fixed bottom-6 right-6 z-50 flex items-center gap-2 rounded-xl bg-neutral-900/95 px-4 py-3 text-xs font-semibold text-white shadow-xl backdrop-blur-md animate-fade-in">
            <CheckCircle2 size={16} className="text-emerald-400 shrink-0" />
            <span>{cartToast}</span>
          </div>
        )}

        {/* 1. STORE HEADER BANNER CARD */}
        <section className={`overflow-hidden rounded-2xl border border-neutral-200/90 bg-white shadow-xs ${isProfileLoading ? "animate-pulse" : ""}`}>
          {/* Panoramic Cover Image */}
          <div className="relative h-44 w-full overflow-hidden bg-neutral-800 sm:h-56 md:h-64">
            <img
              src={coverImage || defaultStoreCover}
              alt={shopName}
              className="h-full w-full object-cover object-center"
              onError={(e) => {
                e.currentTarget.src = defaultStoreCover;
              }}
            />
            <div className="absolute inset-0 bg-linear-to-t from-black/50 via-transparent to-black/20" />

            {/* Top Right Status Badge: • Đang mở cửa / Đã đóng cửa */}
            <div className="absolute right-4 top-4 flex items-center gap-2 rounded-full bg-white/95 px-3.5 py-1.5 text-xs font-bold text-emerald-800 shadow-md backdrop-blur-md">
              <span className={`h-2.5 w-2.5 rounded-full ${profile?.isOpen !== false ? "bg-emerald-500 animate-pulse" : "bg-neutral-400"}`} />
              <span>{profile?.isOpen !== false ? "Đang mở cửa" : "Đã đóng cửa"}</span>
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
                      <span>96% Tỉ lệ phản hồi</span>
                    </span>
                  </div>

                  {/* Expandable trigger: Xem thêm thông tin quán */}
                  <button
                    type="button"
                    onClick={() => setIsStoreInfoExpanded(!isStoreInfoExpanded)}
                    className="mt-3 inline-flex items-center gap-1.5 rounded-xl border border-neutral-200 bg-white px-3.5 py-1.5 text-xs sm:text-sm font-bold text-neutral-700 shadow-2xs transition hover:border-emerald-300 hover:bg-emerald-50 hover:text-emerald-800 cursor-pointer"
                  >
                    <span>{isStoreInfoExpanded ? "Thu gọn thông tin" : "Xem thêm thông tin quán"}</span>
                    {isStoreInfoExpanded ? (
                      <ChevronUp size={15} />
                    ) : (
                      <ChevronDown size={15} />
                    )}
                  </button>
                </div>
              </div>

              {/* Right Column: Rating Block */}
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
                      {totalReviewsCount} đánh giá
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Expandable Store Details Drawer */}
            {isStoreInfoExpanded && (
              <div className="mt-4 rounded-xl border border-neutral-200/80 bg-neutral-50 p-4 text-xs text-neutral-600 sm:text-sm animate-fade-in">
                <h3 className="font-bold text-neutral-900">Giới thiệu quán</h3>
                <p className="mt-1 leading-relaxed text-neutral-600">
                  {profile?.description ||
                    deals[0]?.description ||
                    `${shopName} chuyên phục vụ các món ăn tươi ngon chuẩn vị mỗi ngày. Đồng hành cùng SaveBite mang đến những bữa ăn chất lượng với giá ưu đãi đặc biệt cuối ngày, chung tay bảo vệ môi trường và giảm lãng phí thực phẩm.`}
                </p>
                <div className="mt-3 grid grid-cols-1 gap-2 pt-3 border-t border-neutral-200/60 sm:grid-cols-2 text-xs">
                  <div>
                    <span className="font-semibold text-neutral-700">Địa chỉ quán:</span>{" "}
                    {shopAddress}
                  </div>
                  <div>
                    <span className="font-semibold text-neutral-700">Giờ hoạt động:</span>{" "}
                    {operatingHours}
                  </div>
                </div>
              </div>
            )}
          </div>
        </section>

        {/* 2. TABS: Flash Deals (N) / Reviews (N) */}
        <div className="mt-8 flex items-center gap-6 border-b border-neutral-200 text-sm font-semibold">
          <button
            type="button"
            onClick={() => setActiveTab("deals")}
            className={`flex items-center gap-2 pb-3.5 text-sm sm:text-base font-bold transition cursor-pointer ${
              activeTab === "deals"
                ? "border-b-2 border-emerald-600 text-emerald-700"
                : "text-neutral-500 hover:text-neutral-800"
            }`}
          >
            <span>Ưu đãi Flash Deal</span>
            <span className="rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs font-extrabold text-emerald-700">
              {deals.length}
            </span>
          </button>

          <button
            type="button"
            onClick={() => setActiveTab("reviews")}
            className={`flex items-center gap-2 pb-3.5 text-sm sm:text-base font-bold transition cursor-pointer ${
              activeTab === "reviews"
                ? "border-b-2 border-emerald-600 text-emerald-700"
                : "text-neutral-500 hover:text-neutral-800"
            }`}
          >
            <span>Đánh giá</span>
            <span className="rounded-full bg-neutral-100 px-2.5 py-0.5 text-xs font-bold text-neutral-600">
              {reviewsSummary?.totalReviews ?? totalReviewsCount}
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
                    Chưa có ưu đãi Flash Deal nào
                  </h3>
                  <p className="mt-1 text-xs text-neutral-500 max-w-sm">
                    Quán hiện chưa mở bán deal mới hôm nay. Vui lòng quay lại sau nhé!
                  </p>
                  <Link
                    to="/"
                    className="mt-5 rounded-xl bg-emerald-600 px-5 py-2.5 text-xs font-semibold text-white shadow-xs transition hover:bg-emerald-700"
                  >
                    Khám phá các quán khác
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
