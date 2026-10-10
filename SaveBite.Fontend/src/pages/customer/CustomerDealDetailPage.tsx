import { useEffect, useMemo, useState } from "react";
import { useParams, Link, useNavigate } from "react-router-dom";
import {
  Clock,
  Store,
  Star,
  ShoppingBag,
  CheckCircle2,
  ArrowLeft,
  ChevronRight,
} from "lucide-react";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import type {
  FlashDeal,
  FlashDealVariant,
} from "@/features/flash-deals/types/flashDeal.types";

const fallbackImg =
  "https://images.unsplash.com/photo-1509722747041-616f39b57569?w=800&auto=format&fit=crop&q=80";

const formatVnd = (value: number) => {
  return Number.isFinite(value)
    ? `${Math.round(value).toLocaleString("vi-VN")}đ`
    : "—";
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
  const navigate = useNavigate();
  const [deal, setDeal] = useState<FlashDeal | null>(null);
  const [selectedVariant, setSelectedVariant] =
    useState<FlashDealVariant | null>(null);
  const [selectedAttributes, setSelectedAttributes] = useState<
    Record<string, string>
  >({});
  const [activeImgUrl, setActiveImgUrl] = useState<string | null>(null);
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
            // Tìm variant mặc định ưu tiên: Thường & Không cay (hoặc variant có giá ưu đãi thấp nhất)
            const defaultVar =
              data.variants.find((v) => {
                const attrs = v.attributes || {};
                const sizeVal = (attrs["Size"] || "").toLowerCase();
                const spiceVal = (attrs["Độ cay"] || "").toLowerCase();
                return (
                  (sizeVal.includes("thường") || sizeVal.includes("nhỏ")) &&
                  spiceVal.includes("không")
                );
              }) ||
              [...data.variants].sort((a, b) => a.dealPrice - b.dealPrice)[0];

            setSelectedVariant(defaultVar);
            if (defaultVar.imageUrl) {
              setActiveImgUrl(defaultVar.imageUrl);
            } else if (data.productImageUrl) {
              setActiveImgUrl(data.productImageUrl);
            }

            // Thiết lập giá trị thuộc tính ban đầu
            const initialAttrs: Record<string, string> = {};
            if (defaultVar.attributes) {
              Object.assign(initialAttrs, defaultVar.attributes);
            }
            data.attributeGroups?.forEach((g) => {
              if (!initialAttrs[g.name] && g.values.length > 0) {
                const preferred =
                  g.values.find((val) => {
                    const vLow = val.toLowerCase();
                    return vLow.includes("thường") || vLow.includes("không");
                  }) || g.values[0];
                initialAttrs[g.name] = preferred;
              }
            });
            setSelectedAttributes(initialAttrs);
          }
        }
      })
      .catch(() => {
        if (isMounted) {
          setError("Flash Deal không tồn tại hoặc đã hết hạn.");
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
  const [isEnded, setIsEnded] = useState<boolean>(false);

  useEffect(() => {
    if (!deal?.orderEndTime) return;

    const calcTime = () => {
      const diff = new Date(deal.orderEndTime).getTime() - Date.now();
      if (diff <= 0) {
        setIsEnded(true);
        return "Đã kết thúc";
      }
      setIsEnded(false);
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

  // Danh sách ảnh Gallery thumbnails
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
    return list.length > 0 ? list : [fallbackImg];
  }, [deal]);

  // Ảnh chính đang hiển thị
  const mainImage = activeImgUrl || galleryImages[0] || fallbackImg;

  // Lấy các nhóm thuộc tính (Size, Độ cay...) có sắp xếp chuẩn
  const attributeGroups = useMemo(() => {
    if (!deal) return [];
    let groups: { name: string; values: string[] }[] = [];
    if (deal.attributeGroups && deal.attributeGroups.length > 0) {
      groups = deal.attributeGroups.map((g) => ({
        name: g.name,
        values: [...g.values],
      }));
    } else {
      // Tự động phân tích từ variants nếu backend chưa trả attributeGroups
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

    // Sắp xếp các nhóm: "Size" lên trước, "Độ cay" sau
    groups.sort((a, b) => {
      const aName = a.name.toLowerCase();
      const bName = b.name.toLowerCase();
      if (aName.includes("size")) return -1;
      if (bName.includes("size")) return 1;
      return a.name.localeCompare(b.name, "vi");
    });

    // Sắp xếp thứ tự giá trị chuẩn:
    // Size: 'Thường' đứng trước 'Lớn'
    // Độ cay: 'Không cay' đứng trước 'Cay vừa'
    groups.forEach((g) => {
      g.values.sort((a, b) => {
        const aLow = a.toLowerCase();
        const bLow = b.toLowerCase();
        if (aLow.includes("thường") || aLow.includes("không")) return -1;
        if (bLow.includes("thường") || bLow.includes("không")) return 1;
        return a.localeCompare(b, "vi");
      });
    });

    return groups;
  }, [deal]);

  // Xử lý khi click chọn 1 thuộc tính (ví dụ: Size hoặc Độ cay)
  const handleSelectAttribute = (groupName: string, value: string) => {
    const updatedAttrs = { ...selectedAttributes, [groupName]: value };
    setSelectedAttributes(updatedAttrs);

    if (!deal || !deal.variants) return;

    // Tìm variant khớp đầy đủ nhất với các thuộc tính vừa chọn
    const matched =
      deal.variants.find((v) => {
        if (!v.attributes) return false;
        return Object.entries(updatedAttrs).every(
          ([k, val]) => v.attributes?.[k] === val,
        );
      }) ||
      deal.variants.find((v) => {
        return v.attributes?.[groupName] === value;
      }) ||
      deal.variants[0];

    if (matched) {
      setSelectedVariant(matched);
      setQuantity(1);
      // Đổi ảnh món theo variant ngay lập tức!
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

  const handleAddToCart = () => {
    setAddedSuccess(true);
    setTimeout(() => setAddedSuccess(false), 3000);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-neutral-50/50 py-12">
        <div className="mx-auto max-w-5xl px-4 animate-pulse">
          <div className="mb-6 h-6 w-32 rounded bg-neutral-200" />
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
          <h2 className="mt-4 text-xl font-bold text-neutral-900">
            Không tìm thấy ưu đãi
          </h2>
          <p className="mt-2 text-sm text-neutral-600">
            {error ?? "Món ăn Flash Deal này không tồn tại hoặc đã kết thúc."}
          </p>
          <Link
            to="/"
            className="mt-6 inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white shadow-xs transition hover:bg-emerald-700"
          >
            <ArrowLeft size={16} />
            Quay lại trang chủ
          </Link>
        </div>
      </div>
    );
  }

  const activeVariant = selectedVariant || deal.variants[0];
  const isSoldOut = !activeVariant || activeVariant.availableQuantity <= 0;
  const currentPrice = activeVariant ? activeVariant.dealPrice : deal.minDealPrice;
  const originalPrice = activeVariant
    ? activeVariant.originalPrice
    : deal.maxOriginalPrice;
  const discountPercent = activeVariant
    ? activeVariant.discountPercent
    : deal.maxDiscountPercent;
  const totalQty = activeVariant?.totalQuantity || 100;
  const soldQty = activeVariant?.soldQuantity || 0;
  const remainingQty = activeVariant?.availableQuantity || 0;
  const soldPercent = Math.min(100, Math.round((soldQty / totalQty) * 100));

  return (
    <div className="min-h-screen bg-neutral-50/40 pb-20 pt-6">
      <main className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
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
              <Link
                to={`/shops/${deal.shopId}`}
                className="hover:text-emerald-700 transition font-medium"
              >
                {deal.shopName}
              </Link>
              <ChevronRight size={13} className="text-neutral-300 shrink-0" />
              <span className="truncate max-w-[240px] font-semibold text-neutral-800">
                {deal.productName}
              </span>
            </nav>
          </div>
        </div>

        {/* Main 2-column layout (Ảnh & Thông tin) */}
        <div className="grid items-start gap-8 lg:grid-cols-2">
          {/* CỘT TRÁI: Gallery Ảnh + Thông tin Quán + Mô tả món */}
          <div>
            {/* Khung Ảnh Lớn */}
            <div className="rounded-2xl border border-neutral-200/90 bg-white p-4 shadow-2xs">
              <div className="aspect-square w-full overflow-hidden rounded-xl bg-neutral-100">
                <img
                  src={mainImage}
                  alt={deal.productName}
                  className="h-full w-full object-cover transition-all duration-300 hover:scale-105"
                  onError={(e) => {
                    e.currentTarget.src = fallbackImg;
                  }}
                />
              </div>

              {/* Danh sách Ảnh Thumbnails Gallery (giống hình 3 mẫu) */}
              {galleryImages.length > 1 && (
                <div className="mt-4 flex gap-2.5 overflow-x-auto pb-1">
                  {galleryImages.map((img, idx) => (
                    <button
                      key={idx}
                      type="button"
                      onClick={() => {
                        setActiveImgUrl(img);
                        const matchedVar = deal.variants.find(
                          (v) => v.imageUrl === img,
                        );
                        if (matchedVar) {
                          setSelectedVariant(matchedVar);
                          if (matchedVar.attributes) {
                            setSelectedAttributes({ ...matchedVar.attributes });
                          }
                        }
                      }}
                      className={`h-16 w-16 shrink-0 overflow-hidden rounded-xl border-2 p-0 transition-all cursor-pointer ${
                        img === mainImage
                          ? "border-emerald-600 ring-2 ring-emerald-500/25"
                          : "border-transparent opacity-70 hover:opacity-100 hover:border-neutral-300"
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

            {/* Thông tin Quán (Shop Card) */}
            <div className="mt-5 rounded-2xl border border-neutral-200/90 bg-white p-5 shadow-2xs">
              <div className="flex items-center gap-3.5">
                <Link to={`/shops/${deal.shopId}`} className="shrink-0">
                  <div className="flex h-12 w-12 items-center justify-center overflow-hidden rounded-xl border border-neutral-200 bg-emerald-50 text-emerald-700">
                    {deal.shopLogoUrl ? (
                      <img
                        src={deal.shopLogoUrl}
                        alt={deal.shopName}
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <Store size={22} />
                    )}
                  </div>
                </Link>

                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <Link
                      to={`/shops/${deal.shopId}`}
                      className="text-base font-bold text-neutral-900 hover:text-emerald-700 hover:underline"
                    >
                      {deal.shopName}
                    </Link>
                    <span className="rounded-full bg-emerald-50 px-2.5 py-0.5 text-[11px] font-bold text-emerald-700">
                      Đang mở cửa
                    </span>
                  </div>

                  <div className="mt-1 flex items-center gap-1.5 text-xs text-neutral-500">
                    <span className="flex items-center gap-0.5 font-bold text-amber-500">
                      <Star size={12} className="fill-amber-400 text-amber-400" />
                      4.8
                    </span>
                    {deal.distanceInKm !== undefined && deal.distanceInKm !== null && (
                      <>
                        <span>·</span>
                        <span>cách {deal.distanceInKm} km</span>
                      </>
                    )}
                    <span>·</span>
                    <span>128 đánh giá</span>
                  </div>
                </div>

                <Link
                  to={`/shops/${deal.shopId}`}
                  className="ml-auto shrink-0 whitespace-nowrap text-xs font-bold text-emerald-700 hover:underline"
                >
                  Xem cửa hàng
                </Link>
              </div>
            </div>

            {/* Mô tả sản phẩm */}
            <div className="mt-5 rounded-2xl border border-neutral-200/90 bg-white p-5 shadow-2xs">
              <h2 className="mb-3 text-base font-extrabold text-neutral-900">
                Mô tả sản phẩm
              </h2>

              {deal.categoryName && (
                <span className="mb-3 inline-block rounded-lg bg-emerald-50 px-2.5 py-1 text-xs font-bold text-emerald-700">
                  {deal.categoryName}
                </span>
              )}

              <p className="m-0 text-xs sm:text-sm leading-relaxed text-neutral-600">
                {deal.description ||
                  "Món ăn thơm ngon, chuẩn bị nóng hổi mỗi ngày. Tiết kiệm chi phí và cùng chung tay giảm lãng phí thực phẩm cùng SaveBite!"}
              </p>
            </div>
          </div>

          {/* CỘT PHẢI: Khung Mua Hàng & Lựa chọn Thuộc tính (Sticky Buy Box cân đối chuẩn mẫu) */}
          <aside className="sticky top-20 rounded-2xl border border-neutral-200/90 bg-white p-5 shadow-xs">
            {deal.categoryName && (
              <span className="mb-2 inline-block rounded-md bg-emerald-50 px-2.5 py-0.5 text-xs font-semibold text-emerald-700">
                {deal.categoryName}
              </span>
            )}

            <h1 className="text-xl font-bold leading-snug text-neutral-900">
              {deal.productName}
            </h1>

            {/* Đánh giá sao */}
            <div className="mt-1.5 mb-3.5 flex items-center gap-1.5 text-xs text-neutral-500">
              <div className="flex items-center gap-0.5 text-amber-500">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Star
                    key={i}
                    size={13}
                    className="fill-amber-400 text-amber-400"
                  />
                ))}
              </div>
              <span className="font-bold text-neutral-800">4.7</span>
              <a href="#reviews" className="hover:text-emerald-700 hover:underline">
                (128 đánh giá)
              </a>
            </div>

            {/* Đồng hồ đếm ngược Flash Deal */}
            <div
              className={`mb-3.5 flex items-center justify-between rounded-xl px-3.5 py-2.5 text-xs ${
                isEnded
                  ? "border border-neutral-200 bg-neutral-100 text-neutral-700"
                  : "border border-amber-200/80 bg-amber-50/70 text-amber-900"
              }`}
            >
              <span className="flex items-center gap-1.5 font-medium text-amber-800">
                <Clock size={15} className="text-amber-600" />
                Kết thúc sau
              </span>
              <span className="font-bold tabular-nums text-amber-700">
                {timeLeft}
              </span>
            </div>

            {/* Giá tiền & Giảm giá */}
            <div className="mb-1 flex flex-wrap items-baseline gap-2.5">
              <span className="text-2xl font-bold text-emerald-700">
                {formatVnd(currentPrice)}
              </span>
              {originalPrice > currentPrice && (
                <span className="text-sm text-neutral-400 line-through">
                  {formatVnd(originalPrice)}
                </span>
              )}
              {discountPercent > 0 && (
                <span className="rounded bg-amber-500 px-1.5 py-0.5 text-[11px] font-bold text-white shadow-2xs">
                  -{Math.round(discountPercent)}%
                </span>
              )}
            </div>

            <p className="mb-4 text-xs text-neutral-400">
              Giá đã bao gồm ưu đãi Flash Deal, áp dụng khi đến lấy tại quán
            </p>

            {/* NHÓM THUỘC TÍNH (Size, Độ cay...) TƯƠNG TÁC ĐỔI ẢNH */}
            {attributeGroups.length > 0 ? (
              <div className="mb-4 space-y-3">
                {attributeGroups.map((group) => {
                  const currentValue = selectedAttributes[group.name];
                  return (
                    <div key={group.name}>
                      <label className="mb-1.5 block text-xs font-semibold text-neutral-700">
                        {group.name}
                      </label>
                      <div className="flex flex-wrap gap-2">
                        {group.values.map((val) => {
                          const isSelected = currentValue === val;
                          return (
                            <button
                              key={val}
                              type="button"
                              onClick={() =>
                                handleSelectAttribute(group.name, val)
                              }
                              className={`rounded-lg px-3.5 py-1.5 text-xs font-medium transition cursor-pointer ${
                                isSelected
                                  ? "border border-emerald-600 bg-emerald-50/80 text-emerald-800 shadow-2xs"
                                  : "border border-neutral-200 bg-white text-neutral-600 hover:border-neutral-300 hover:bg-neutral-50"
                              }`}
                            >
                              {val}
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  );
                })}
              </div>
            ) : deal.variants.length > 1 ? (
              /* Fallback nếu chỉ có danh sách variants chung */
              <div className="mb-4">
                <label className="mb-1.5 block text-xs font-semibold text-neutral-700">
                  Lựa chọn
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
                        onClick={() => handleSelectVariantDirect(v)}
                        className={`rounded-lg px-3.5 py-1.5 text-xs font-medium transition cursor-pointer ${
                          isSelected
                            ? "border border-emerald-600 bg-emerald-50/80 text-emerald-800 shadow-2xs"
                            : vSoldOut
                            ? "border border-neutral-200 bg-neutral-100 text-neutral-400 cursor-not-allowed"
                            : "border border-neutral-200 bg-white text-neutral-600 hover:border-neutral-300 hover:bg-neutral-50"
                        }`}
                      >
                        {v.variantName || v.sku || "Mặc định"}
                      </button>
                    );
                  })}
                </div>
              </div>
            ) : null}

            {/* Thanh tiến độ đã bán (Sales Progress Bar) */}
            <div className="mb-4">
              <div className="mb-1.5 flex items-center justify-between text-xs text-neutral-500">
                <span>
                  Đã bán {soldQty}/{totalQty}
                </span>
                <span className="font-semibold text-amber-600">
                  {remainingQty <= 10
                    ? `Chỉ còn ${remainingQty} suất`
                    : `Còn ${remainingQty} suất`}
                </span>
              </div>
              <div className="h-1.5 w-full overflow-hidden rounded-full bg-neutral-100">
                <div
                  className="h-full rounded-full bg-amber-500 transition-all duration-300"
                  style={{ width: `${soldPercent}%` }}
                />
              </div>
            </div>

            {/* Chọn số lượng */}
            <div className="mb-4 flex items-center justify-between">
              <span className="text-xs font-semibold text-neutral-800">
                Số lượng
              </span>
              <div className="flex items-center overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-2xs">
                <button
                  type="button"
                  disabled={quantity <= 1 || isSoldOut}
                  onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                  className="flex h-8 w-8 items-center justify-center bg-white text-sm font-bold text-neutral-500 transition hover:bg-neutral-100 disabled:opacity-30 cursor-pointer"
                >
                  −
                </button>
                <span className="w-8 text-center text-xs font-bold text-neutral-800">
                  {quantity}
                </span>
                <button
                  type="button"
                  disabled={
                    isSoldOut ||
                    (activeVariant && quantity >= activeVariant.availableQuantity)
                  }
                  onClick={() => setQuantity((q) => q + 1)}
                  className="flex h-8 w-8 items-center justify-center bg-white text-sm font-bold text-neutral-500 transition hover:bg-neutral-100 disabled:opacity-30 cursor-pointer"
                >
                  +
                </button>
              </div>
            </div>

            {/* Thông báo thêm vào giỏ thành công */}
            {addedSuccess && (
              <div className="mb-3 flex items-center gap-2 rounded-xl bg-emerald-50 p-2.5 text-xs font-semibold text-emerald-800 border border-emerald-200">
                <CheckCircle2 size={15} className="text-emerald-600 shrink-0" />
                Đã thêm {quantity} phần vào giỏ hàng thành công!
              </div>
            )}

            {/* Nút Thêm vào giỏ */}
            <button
              type="button"
              disabled={isSoldOut}
              onClick={handleAddToCart}
              className={`flex w-full items-center justify-center gap-2 rounded-xl py-3 text-center text-sm font-bold shadow-sm transition-all cursor-pointer ${
                isSoldOut
                  ? "cursor-not-allowed bg-neutral-200 text-neutral-500 shadow-none"
                  : "bg-emerald-600 text-white shadow-emerald-600/20 hover:bg-emerald-700 active:scale-[0.99]"
              }`}
            >
              <ShoppingBag size={17} />
              <span>
                {isSoldOut
                  ? "Hết suất"
                  : `Thêm vào giỏ · ${formatVnd(currentPrice * quantity)}`}
              </span>
            </button>

            {/* Lời nhắc giờ đóng cửa */}
            <p className="mt-2.5 text-center text-[11px] text-neutral-400">
              Quán đóng cửa lúc {formatTime(deal.shopClosingTime)} — vui lòng đến lấy trước giờ này
            </p>
          </aside>
        </div>

        {/* Khung Đánh giá từ khách hàng */}
        <section
          id="reviews"
          className="mt-8 rounded-2xl border border-neutral-200/90 bg-white p-6 shadow-2xs"
        >
          <h2 className="text-base font-extrabold text-neutral-900 mb-2">
            Đánh giá từ khách hàng
          </h2>
          <div className="py-8 text-center text-sm text-neutral-500 border border-dashed border-neutral-200 rounded-xl">
            Chưa có đánh giá nào cho món ăn này.
          </div>
        </section>
      </main>
    </div>
  );
}
