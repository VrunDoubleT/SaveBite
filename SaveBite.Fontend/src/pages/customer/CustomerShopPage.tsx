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
import { ShopDealCard } from "@/features/flash-deals/components/ShopDealCard";
import { Pagination, usePagination } from "@/shared/components/pagination";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

export function CustomerShopPage() {
  const { id } = useParams<{ id: string }>();
  const [deals, setDeals] = useState<FlashDeal[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Tabs state: 'deals' | 'reviews'
  const [activeTab, setActiveTab] = useState<"deals" | "reviews">("deals");

  // Expandable store details toggle
  const [isStoreInfoExpanded, setIsStoreInfoExpanded] = useState(false);

  // Toast feedback for add to cart
  const [cartToast, setCartToast] = useState<string | null>(null);

  // Rating is null as requested by user (no API yet)
  const shopRating: number | null = null;
  const reviewCount = 0;

  useEffect(() => {
    if (!id) return;
    let isMounted = true;
    setIsLoading(true);

    flashDealApi
      .getDealsByShop(id)
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
        if (isMounted) {
          setIsLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [id]);

  // Shop details from first deal or default fallbacks
  const firstDeal = deals[0];
  const shopName = firstDeal?.shopName || "Bánh Mì Cô Ba";
  const shopAddress =
    firstDeal?.shopAddress || "45 Đường Nguyễn Trãi, Phường Tân An, Ninh Kiều, Cần Thơ";
  const shopLogo = firstDeal?.shopLogoUrl;

  // Extract unique category names of this shop's deals
  const shopCategories = useMemo(() => {
    const set = new Set<string>();
    for (const d of deals) {
      if (d.categoryName) {
        set.add(d.categoryName);
      }
    }
    return set.size > 0 ? Array.from(set) : ["Ăn vặt", "Đồ uống"];
  }, [deals]);

  // Pagination for shop deals (8 or 9 items per page if shop has many deals)
  const {
    paginatedItems: paginatedDeals,
    currentPage,
    setCurrentPage,
    totalPages,
    pageSize,
    totalItems,
  } = usePagination({
    items: deals,
    initialPageSize: 8,
  });

  const handleAddToCart = (deal: FlashDeal) => {
    setCartToast(`Đã thêm "${deal.productName}" vào giỏ hàng!`);
    setTimeout(() => {
      setCartToast(null);
    }, 2800);
  };

  // High quality food banner matching Vietnamese cuisine spread
  const defaultCoverUrl =
    "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=1600&auto=format&fit=crop&q=80";

  return (
    <div className="min-h-screen bg-neutral-50/50 pb-20 pt-5">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Back Link */}
        <Link
          to="/"
          className="mb-4 inline-flex items-center gap-1.5 text-xs font-semibold text-neutral-500 transition hover:text-emerald-700"
        >
          <ArrowLeft size={15} />
          Quay lại trang chủ
        </Link>

        {/* Added to cart toast notification */}
        {cartToast && (
          <div className="fixed bottom-6 right-6 z-50 flex items-center gap-2 rounded-xl bg-neutral-900/95 px-4 py-3 text-xs font-semibold text-white shadow-xl backdrop-blur-md animate-fade-in">
            <CheckCircle2 size={16} className="text-emerald-400 shrink-0" />
            <span>{cartToast}</span>
          </div>
        )}

        {/* 1. SHOP HEADER BANNER CARD */}
        <section className="overflow-hidden rounded-2xl border border-neutral-200/90 bg-white shadow-xs">
          {/* Panoramic Cover Image */}
          <div className="relative h-44 w-full overflow-hidden bg-neutral-900 sm:h-52 md:h-60">
            <img
              src={defaultCoverUrl}
              alt={shopName}
              className="h-full w-full object-cover object-center"
            />
            <div className="absolute inset-0 bg-linear-to-t from-black/50 via-transparent to-black/20" />

            {/* Top Right Status Badge: • Đang mở cửa */}
            <div className="absolute right-4 top-4 flex items-center gap-1.5 rounded-full bg-white/95 px-3 py-1 text-xs font-bold text-emerald-800 shadow-sm backdrop-blur-xs">
              <span className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
              <span>Đang mở cửa</span>
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
                      <span>06:00 - 21:00</span>
                    </span>

                    <span className="flex items-center gap-1 text-neutral-700">
                      <MessageCircle size={15} className="text-emerald-600 shrink-0" />
                      <span>Tỷ lệ phản hồi 96%</span>
                    </span>
                  </div>

                  {/* Expandable trigger: Xem thêm thông tin cửa hàng */}
                  <button
                    type="button"
                    onClick={() => setIsStoreInfoExpanded(!isStoreInfoExpanded)}
                    className="mt-2.5 inline-flex items-center gap-1 text-xs font-bold text-emerald-700 transition hover:text-emerald-800"
                  >
                    <span>Xem thêm thông tin cửa hàng</span>
                    {isStoreInfoExpanded ? (
                      <ChevronUp size={14} />
                    ) : (
                      <ChevronDown size={14} />
                    )}
                  </button>
                </div>
              </div>

              {/* Right Column: Rating Block (Null when API is not available) */}
              <div className="flex items-center gap-3 sm:flex-col sm:items-end sm:pt-2">
                {shopRating !== null ? (
                  <div className="flex items-center gap-3">
                    <span className="text-3xl font-extrabold text-neutral-900">
                      {shopRating}
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
                        {reviewCount} đánh giá
                      </span>
                    </div>
                  </div>
                ) : (
                  <div className="flex flex-col items-start rounded-xl border border-dashed border-neutral-200 bg-neutral-50/70 px-3.5 py-2 sm:items-end">
                    <div className="flex items-center gap-0.5 text-neutral-300">
                      {[...Array(5)].map((_, i) => (
                        <Star
                          key={i}
                          size={14}
                          className="fill-neutral-200 text-neutral-200"
                        />
                      ))}
                    </div>
                    <span className="mt-1 text-xs font-medium text-neutral-400">
                      Chưa có đánh giá
                    </span>
                  </div>
                )}
              </div>
            </div>

            {/* Expandable Store Details Drawer */}
            {isStoreInfoExpanded && (
              <div className="mt-4 rounded-xl border border-neutral-200/80 bg-neutral-50 p-4 text-xs text-neutral-600 sm:text-sm animate-fade-in">
                <h3 className="font-bold text-neutral-900">Giới thiệu cửa hàng</h3>
                <p className="mt-1 leading-relaxed text-neutral-600">
                  {firstDeal?.description ||
                    `${shopName} chuyên phục vụ các món ăn tươi ngon trong ngày với công thức gia truyền. Tiệm tham gia SaveBite nhằm mang các suất ăn chất lượng đến khách hàng với mức giá ưu đãi đặc biệt cuối ngày, giảm thiểu lãng phí thực phẩm.`}
                </p>
                <div className="mt-3 grid grid-cols-1 gap-2 pt-3 border-t border-neutral-200/60 sm:grid-cols-2 text-xs">
                  <div>
                    <span className="font-semibold text-neutral-700">Địa chỉ đầy đủ:</span>{" "}
                    {shopAddress}
                  </div>
                  <div>
                    <span className="font-semibold text-neutral-700">Thời gian nhận món:</span>{" "}
                    Sau 18:00 đến trước giờ đóng cửa (21:00)
                  </div>
                </div>
              </div>
            )}
          </div>
        </section>

        {/* 2. TABS: Flash Deal (N) / Đánh giá (0) */}
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
            <span>Flash Deal</span>
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
            <span>Đánh giá</span>
            <span className="text-xs">({reviewCount})</span>
          </button>
        </div>

        {/* 3. TAB CONTENT */}
        <div className="mt-6">
          {activeTab === "deals" ? (
            <div>
              {error && (
                <div className="mb-6 rounded-xl border border-red-200 bg-red-50 p-4 text-xs font-medium text-red-700">
                  {error}
                </div>
              )}

              {/* Loading Skeleton */}
              {isLoading && (
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
              {!isLoading && deals.length === 0 && (
                <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-neutral-300 bg-white px-6 py-16 text-center shadow-xs">
                  <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                    <Store size={32} />
                  </div>
                  <h3 className="mt-4 text-base font-bold text-neutral-800">
                    Cửa hàng chưa có Flash Deal nào
                  </h3>
                  <p className="mt-1 text-xs text-neutral-500 max-w-sm">
                    Hiện tại quán chưa mở bán suất ưu đãi. Vui lòng quay lại vào khung giờ flash deal buổi chiều hoặc tối!
                  </p>
                  <Link
                    to="/"
                    className="mt-5 rounded-xl bg-emerald-600 px-5 py-2.5 text-xs font-semibold text-white shadow-xs transition hover:bg-emerald-700"
                  >
                    Xem các cửa hàng khác
                  </Link>
                </div>
              )}

              {/* 4 COLUMNS DEAL GRID MATCHING THE MOCKUP */}
              {!isLoading && deals.length > 0 && (
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

                  {/* Reusable Pagination if deals > 8 */}
                  {totalPages > 1 && (
                    <div className="mt-8">
                      <Pagination
                        currentPage={currentPage}
                        totalPages={totalPages}
                        onPageChange={setCurrentPage}
                        totalItems={totalItems}
                        pageSize={pageSize}
                        itemLabel="ưu đãi"
                        showTotalItems={true}
                      />
                    </div>
                  )}
                </div>
              )}
            </div>
          ) : (
            /* Reviews Tab (Null state when API is not available) */
            <div className="rounded-2xl border border-dashed border-neutral-200 bg-white p-12 text-center shadow-xs">
              <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-amber-50 text-amber-500">
                <Star size={32} className="fill-amber-400 text-amber-400" />
              </div>
              <h3 className="mt-4 text-base font-bold text-neutral-900">
                Chưa có đánh giá nào cho cửa hàng này
              </h3>
              <p className="mx-auto mt-1.5 max-w-md text-xs text-neutral-500 leading-relaxed">
                Tính năng đánh giá đang được hoàn thiện. Sau khi bạn nhận món thành công từ cửa hàng, bạn sẽ có thể để lại phản hồi và chấm điểm tại đây.
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
