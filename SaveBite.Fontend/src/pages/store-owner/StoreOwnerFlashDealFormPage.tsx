import { useEffect, useState, useMemo, useRef } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  ArrowLeft,
  Trash2,
  Plus,
  Loader2,
  Calendar,
  Clock,
  AlertCircle,
  ChevronDown,
  Eye,
  EyeOff,
  Check,
} from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import {
  FlashDealStatusEnum,
  FlashDealVariantStatusEnum,
  type ShopEligibleProduct,
  type ShopEligibleProductVariant,
  type ShopFlashDealDetail,
} from "@/features/flash-deals/types/flashDeal.types";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

interface FormVariantState {
  variantId: string;
  sku?: string;
  variantName: string;
  originalPrice: number;
  dealPrice: number | "";
  totalQuantity: number | "";
  soldQuantity?: number;
  reservedQuantity?: number;
  status?: number; // 0: Active, 1: Hide
}

export function StoreOwnerFlashDealFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  // Loading states
  const [initialLoading, setInitialLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  // Eligible products for creation
  const [eligibleProducts, setEligibleProducts] = useState<ShopEligibleProduct[]>([]);
  const [selectedProductId, setSelectedProductId] = useState<string>("");
  const [isProductDropdownOpen, setIsProductDropdownOpen] = useState(false);
  const productDropdownRef = useRef<HTMLDivElement>(null);

  // Close dropdown on click outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (productDropdownRef.current && !productDropdownRef.current.contains(event.target as Node)) {
        setIsProductDropdownOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Existing deal data if editing
  const [existingDeal, setExistingDeal] = useState<ShopFlashDealDetail | null>(null);

  // Split date and time states for easy input and selection
  const [startDate, setStartDate] = useState<string>("");
  const [startTime, setStartTime] = useState<string>("");
  const [endDate, setEndDate] = useState<string>("");
  const [endTime, setEndTime] = useState<string>("");

  // Configured variants
  const [variants, setVariants] = useState<FormVariantState[]>([]);

  // Whether current deal is already OnSale
  const isDealOnSale = isEditMode && existingDeal?.status === FlashDealStatusEnum.OnSale;

  // Format helpers
  function formatDate(d: Date): string {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, "0");
    const day = String(d.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  }

  function formatTime(d: Date): string {
    const hours = String(d.getHours()).padStart(2, "0");
    const minutes = String(d.getMinutes()).padStart(2, "0");
    return `${hours}:${minutes}`;
  }

  function parseDateTime(dStr: string, tStr: string): Date | null {
    if (!dStr || !tStr) return null;
    const d = new Date(`${dStr}T${tStr}`);
    return isNaN(d.getTime()) ? null : d;
  }

  // Initial load
  useEffect(() => {
    async function init() {
      try {
        setInitialLoading(true);
        // Load eligible shop products in both modes to have full product & variant metadata
        const products = await flashDealApi.getEligibleProducts();
        setEligibleProducts(products);

        if (isEditMode && id) {
          // Edit mode: fetch deal detail
          const deal = await flashDealApi.getShopFlashDealDetail(id);
          setExistingDeal(deal);
          setSelectedProductId(deal.productId);

          const start = new Date(deal.saleStartTime);
          const end = new Date(deal.orderEndTime);
          setStartDate(formatDate(start));
          setStartTime(formatTime(start));
          setEndDate(formatDate(start)); // Always same day
          setEndTime(formatTime(end));

          setVariants(
            deal.variants.map((v) => ({
              variantId: v.variantId,
              sku: v.sku,
              variantName: v.variantName || v.sku || "Default",
              originalPrice: v.originalPrice,
              dealPrice: v.dealPrice,
              totalQuantity: v.totalQuantity,
              soldQuantity: v.soldQuantity,
              reservedQuantity: v.reservedQuantity,
              status: v.status ?? FlashDealVariantStatusEnum.Active,
            }))
          );
        } else {
          // Create mode default: start rounded to next 15 mins, end in 60 mins (within 30-120m)
          const now = new Date();
          const remainder = 15 - (now.getMinutes() % 15);
          const start = new Date(now.getTime() + remainder * 60 * 1000);
          const end = new Date(start.getTime() + 60 * 60 * 1000);

          setStartDate(formatDate(start));
          setStartTime(formatTime(start));
          setEndDate(formatDate(start)); // Same day
          setEndTime(formatTime(end));
        }
      } catch (err) {
        toast.error(getApiErrorMessage(err));
      } finally {
        setInitialLoading(false);
      }
    }
    void init();
  }, [id, isEditMode]);

  // Selected product object
  const currentProduct = useMemo(() => {
    const prodId = isEditMode && existingDeal ? existingDeal.productId : selectedProductId;
    const found = eligibleProducts.find((p) => p.id === prodId);
    if (found) return found;

    if (isEditMode && existingDeal) {
      return {
        id: existingDeal.productId,
        name: existingDeal.productName,
        description: existingDeal.description,
        categoryName: existingDeal.categoryName,
        imageUrl: existingDeal.primaryImageUrl,
        variants: existingDeal.variants.map((v) => ({
          id: v.variantId,
          sku: v.sku,
          variantName: v.variantName || v.sku || "Default",
          originalPrice: v.originalPrice,
          imageUrl: v.imageUrl,
        })),
      };
    }
    return null;
  }, [isEditMode, existingDeal, eligibleProducts, selectedProductId]);

  // Schedule Summary & Validity
  const scheduleSummary = useMemo(() => {
    const start = parseDateTime(startDate, startTime);
    const end = parseDateTime(endDate, endTime);
    if (!start || !end) return null;

    const diffMs = end.getTime() - start.getTime();
    const durationMins = Math.round(diffMs / (1000 * 60));
    const isSameDay = startDate === endDate;
    const isDurationValid = durationMins >= 30 && durationMins <= 120;
    const isValid = diffMs > 0 && isSameDay && isDurationValid;

    const hours = Math.floor(durationMins / 60);
    const minutes = durationMins % 60;

    const now = new Date();
    const isCurrentlyActive = start <= now && end >= now;
    const isUpcoming = start > now;

    // Closing time is order cutoff + 30 mins
    const closingTime = new Date(end.getTime() + 30 * 60 * 1000);

    return {
      start,
      end,
      isValid,
      durationMins,
      isSameDay,
      isDurationValid,
      hours,
      minutes,
      isCurrentlyActive,
      isUpcoming,
      closingTime,
    };
  }, [startDate, startTime, endDate, endTime]);

  // Handle selecting product in create mode
  function handleSelectProduct(productId: string) {
    setSelectedProductId(productId);
    const prod = eligibleProducts.find((p) => p.id === productId);
    if (!prod) {
      setVariants([]);
      return;
    }

    // Populate all variants of this product
    setVariants(
      prod.variants.map((v) => ({
        variantId: v.id,
        sku: v.sku,
        variantName: v.variantName || v.sku || "Default",
        originalPrice: v.originalPrice,
        dealPrice: Math.round(v.originalPrice * 0.7), // Suggest 30% discount default
        totalQuantity: 50,
      }))
    );
  }

  // Update variant field
  function handleUpdateVariant(
    index: number,
    field: "dealPrice" | "totalQuantity" | "status",
    value: string | number
  ) {
    setVariants((prev) => {
      const next = [...prev];
      const val = field === "status" ? Number(value) : value === "" ? "" : Number(value);
      next[index] = { ...next[index], [field]: val };
      return next;
    });
  }

  // Remove variant
  function handleRemoveVariant(index: number) {
    if (variants.length <= 1) {
      toast.error("Flash Deal must include at least one variant.");
      return;
    }
    const target = variants[index];
    if (target.soldQuantity && target.soldQuantity > 0) {
      toast.error(`Variant ${target.variantName} has already recorded orders, please switch its status to Hide instead of removing.`);
      return;
    }
    setVariants((prev) => prev.filter((_, i) => i !== index));
  }

  // Unselected variants of current product (to re-add)
  const remainingProductVariants = useMemo(() => {
    if (!currentProduct) return [];
    const chosenIds = new Set(variants.map((v) => v.variantId));
    return currentProduct.variants.filter((v) => !chosenIds.has(v.id));
  }, [currentProduct, variants]);

  function handleAddVariant(v: ShopEligibleProductVariant) {
    setVariants((prev) => [
      ...prev,
      {
        variantId: v.id,
        sku: v.sku,
        variantName: v.variantName || v.sku || "Default",
        originalPrice: v.originalPrice,
        dealPrice: Math.round(v.originalPrice * 0.7),
        totalQuantity: 50,
        status: FlashDealVariantStatusEnum.Active,
      },
    ]);
  }

  // Submit form
  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    if (!selectedProductId) {
      toast.error("Please select a product.");
      return;
    }

    const startDateObj = parseDateTime(startDate, startTime);
    const endDateObj = parseDateTime(endDate, endTime);

    if (!startDateObj || !endDateObj) {
      toast.error("Please select both start and cutoff dates and times.");
      return;
    }

    // 1. Must be on the same day
    if (startDate !== endDate) {
      toast.error("Flash Deals can only take place within a single calendar day.");
      return;
    }

    if (startDateObj >= endDateObj) {
      toast.error("Sale start time must be before order cutoff time.");
      return;
    }

    // 2. Duration must be between 30 and 120 minutes
    const durationMins = Math.round((endDateObj.getTime() - startDateObj.getTime()) / (1000 * 60));
    if (durationMins < 30 || durationMins > 120) {
      toast.error(`The duration of a flash deal must be between 30 and 120 minutes (currently: ${durationMins} mins).`);
      return;
    }

    const now = new Date();
    const isDealOnSale = isEditMode && existingDeal?.status === FlashDealStatusEnum.OnSale;

    if (isDealOnSale) {
      // If currently OnSale: order cutoff cannot exceed 120 mins from original start time
      const origStart = new Date(existingDeal.saleStartTime);
      const durationFromStart = Math.round((endDateObj.getTime() - origStart.getTime()) / (1000 * 60));
      if (durationFromStart > 120) {
        toast.error("Order cutoff time cannot exceed 120 minutes from the initial sale start time.");
        return;
      }
      if (endDateObj <= now) {
        toast.error("Order cutoff time must be in the future.");
        return;
      }
    } else {
      // Create mode or Pending deal: check advance scheduling window (15m to 90m) or immediate start (<= 5m)
      const diffStartMins = Math.round((startDateObj.getTime() - now.getTime()) / (1000 * 60));
      const isImmediate = diffStartMins <= 5;

      if (!isImmediate) {
        if (diffStartMins < 15) {
          toast.error("Start time must be at least 15 minutes from now, or choose immediate opening.");
          return;
        }
        if (diffStartMins > 90) {
          toast.error(`Sales can only be scheduled up to 90 minutes in advance (currently: ${diffStartMins} mins).`);
          return;
        }
      }
    }

    if (variants.length === 0) {
      toast.error("Please configure at least one variant.");
      return;
    }

    for (const v of variants) {
      if (v.dealPrice === "" || Number(v.dealPrice) <= 0) {
        toast.error(`Flash Deal price for ${v.variantName} must be greater than 0.`);
        return;
      }
      if (Number(v.dealPrice) >= v.originalPrice) {
        toast.error(`Flash Deal price for ${v.variantName} must be lower than original price (${v.originalPrice.toLocaleString()}đ).`);
        return;
      }
      if (v.totalQuantity === "" || Number(v.totalQuantity) <= 0) {
        toast.error(`Deal quantity for ${v.variantName} must be greater than 0.`);
        return;
      }
      if (v.soldQuantity !== undefined && Number(v.totalQuantity) < (v.soldQuantity + (v.reservedQuantity || 0))) {
        toast.error(`Total quantity for ${v.variantName} cannot be less than already sold quantity (${v.soldQuantity}).`);
        return;
      }
    }

    // Default shop closing time = orderEndTime + 30 mins
    const closingDateObj = new Date(endDateObj.getTime() + 30 * 60 * 1000);

    try {
      setSubmitting(true);
      if (isEditMode && id) {
        await flashDealApi.updateFlashDeal(id, {
          saleStartTime: startDateObj.toISOString(),
          orderEndTime: endDateObj.toISOString(),
          shopClosingTime: closingDateObj.toISOString(),
          variants: variants.map((v) => ({
            variantId: v.variantId,
            dealPrice: Number(v.dealPrice),
            totalQuantity: Number(v.totalQuantity),
            status: v.status ?? FlashDealVariantStatusEnum.Active,
          })),
        });
        toast.success("Flash Deal updated successfully!");
      } else {
        await flashDealApi.createFlashDeal({
          productId: selectedProductId,
          saleStartTime: startDateObj.toISOString(),
          orderEndTime: endDateObj.toISOString(),
          shopClosingTime: closingDateObj.toISOString(),
          variants: variants.map((v) => ({
            variantId: v.variantId,
            dealPrice: Number(v.dealPrice),
            totalQuantity: Number(v.totalQuantity),
          })),
        });
        toast.success("Flash Deal created successfully and scheduled!");
      }
      navigate(APP_PATHS.STORE_OWNER_FLASH_DEALS);
    } catch (err) {
      toast.error(getApiErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  if (initialLoading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <Loader2 className="size-8 animate-spin text-emerald-600" />
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Back button */}
      <button
        type="button"
        onClick={() => navigate(APP_PATHS.STORE_OWNER_FLASH_DEALS)}
        className="inline-flex items-center gap-1.5 text-xs font-semibold text-emerald-600 hover:text-emerald-700"
      >
        <ArrowLeft className="size-4" />
        <span>Back to Flash Deals</span>
      </button>

      {/* Title */}
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-neutral-900">
          {isEditMode ? "Edit Flash Deal" : "Create Flash Deal"}
        </h1>
        <p className="mt-1 text-sm text-neutral-500">
          Select a product, configure deal timing, and set discounted prices for variants.
        </p>
      </div>

      {/* Block 1: Product Selection */}
      <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs space-y-4">
        <h2 className="text-sm font-bold text-neutral-900">Product Information</h2>

        <div>
          <label className="block text-xs font-medium text-neutral-700">
            Selected Product
          </label>

          {isEditMode ? (
            <div className="mt-1">
              <input
                type="text"
                disabled
                value={currentProduct?.name || ""}
                className="w-full rounded-lg border border-neutral-200 bg-neutral-100 px-3 py-2 text-xs text-neutral-600 cursor-not-allowed font-medium"
              />
              <p className="mt-1 text-[11px] text-neutral-400">
                Product cannot be changed when editing an existing deal.
              </p>
            </div>
          ) : (
            <div className="relative mt-1" ref={productDropdownRef}>
              <button
                type="button"
                onClick={() => setIsProductDropdownOpen((prev) => !prev)}
                className="flex w-full items-center justify-between rounded-lg border border-neutral-300 bg-white px-3 py-2 text-xs text-neutral-900 transition hover:border-neutral-400 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 shadow-2xs"
              >
                {currentProduct ? (
                  <div className="flex items-center gap-2.5 truncate">
                    <img
                      src={currentProduct.imageUrl || "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=80&auto=format&fit=crop&q=60"}
                      alt={currentProduct.name}
                      className="size-7 rounded-md object-cover border border-neutral-100 shrink-0"
                    />
                    <div className="text-left truncate">
                      <span className="font-semibold text-neutral-900">{currentProduct.name}</span>
                      <span className="ml-2 text-[11px] text-neutral-400">
                        {currentProduct.categoryName ? `(${currentProduct.categoryName})` : ""} • {currentProduct.variants.length} variant{currentProduct.variants.length !== 1 ? "s" : ""}
                      </span>
                    </div>
                  </div>
                ) : (
                  <span className="text-neutral-400">Select a product to put on flash deal...</span>
                )}
                <ChevronDown className={`size-4 text-neutral-400 transition-transform ${isProductDropdownOpen ? "rotate-180" : ""}`} />
              </button>

              {isProductDropdownOpen && (
                <div className="absolute z-20 mt-1 max-h-64 w-full overflow-y-auto rounded-lg border border-neutral-200 bg-white p-1.5 shadow-lg">
                  {eligibleProducts.length === 0 ? (
                    <div className="p-3 text-center text-xs text-neutral-400">No eligible products available</div>
                  ) : (
                    eligibleProducts.map((p) => {
                      const isSelected = p.id === selectedProductId;
                      return (
                        <div
                          key={p.id}
                          onClick={() => {
                            handleSelectProduct(p.id);
                            setIsProductDropdownOpen(false);
                          }}
                          className={`flex items-center justify-between gap-3 rounded-md px-2.5 py-2 cursor-pointer transition ${
                            isSelected ? "bg-emerald-50 text-emerald-900 font-semibold" : "hover:bg-neutral-50 text-neutral-800"
                          }`}
                        >
                          <div className="flex items-center gap-2.5 truncate">
                            <img
                              src={p.imageUrl || "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=80&auto=format&fit=crop&q=60"}
                              alt={p.name}
                              className="size-9 rounded-md object-cover border border-neutral-100 shrink-0"
                            />
                            <div className="truncate text-left">
                              <p className="text-xs font-semibold text-neutral-900">{p.name}</p>
                              <p className="text-[11px] text-neutral-400">
                                {p.categoryName || "Food"} • {p.variants.length} variant{p.variants.length !== 1 ? "s" : ""}
                              </p>
                            </div>
                          </div>
                          {isSelected && <Check className="size-4 text-emerald-600 shrink-0" />}
                        </div>
                      );
                    })
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Product preview card */}
        {currentProduct && (
          <div className="flex items-center gap-3 rounded-lg border border-neutral-100 bg-neutral-50/70 p-3">
            <img
              src={currentProduct.imageUrl || "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=120&auto=format&fit=crop&q=60"}
              alt={currentProduct.name}
              className="size-14 rounded-lg object-cover"
            />
            <div>
              <p className="font-semibold text-xs text-neutral-900">{currentProduct.name}</p>
              <p className="text-[11px] text-neutral-400">
                {currentProduct.categoryName || "Food"}
              </p>
              {currentProduct.description && (
                <p className="mt-0.5 text-[11px] text-neutral-500 line-clamp-1">
                  {currentProduct.description}
                </p>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Block 2: Schedule & Status (Easy Date/Time Input & Selection) */}
      <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs space-y-5">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 border-b border-neutral-100 pb-3.5">
          <div>
            <h2 className="text-sm font-bold text-neutral-900">Schedule & Status</h2>
            <p className="mt-0.5 text-xs text-neutral-500">
              Easily type or select date and time. Orders automatically cutoff 30 minutes before store closing.
            </p>
          </div>

          {/* Dynamic Live Status Badge */}
          {scheduleSummary?.isValid ? (
            scheduleSummary.isCurrentlyActive ? (
              <span className="inline-flex items-center gap-1.5 self-start sm:self-auto rounded-full bg-emerald-50 px-3 py-1 text-xs font-bold text-emerald-700 ring-1 ring-inset ring-emerald-600/20">
                <span className="size-2 rounded-full bg-emerald-500 animate-pulse" />
                Active Immediately
              </span>
            ) : scheduleSummary.isUpcoming ? (
              <span className="inline-flex items-center gap-1.5 self-start sm:self-auto rounded-full bg-blue-50 px-3 py-1 text-xs font-bold text-blue-700 ring-1 ring-inset ring-blue-600/20">
                Upcoming Deal
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 self-start sm:self-auto rounded-full bg-neutral-100 px-3 py-1 text-xs font-medium text-neutral-600">
                Ended
              </span>
            )
          ) : (
            <span className="inline-flex items-center gap-1.5 self-start sm:self-auto rounded-full bg-neutral-100 px-3 py-1 text-xs font-medium text-neutral-500">
              Draft
            </span>
          )}
        </div>

        <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
          {/* Box 1: Sale Starts */}
          <div className="rounded-xl border border-neutral-200 bg-neutral-50/40 p-4 space-y-3.5">
            <div className="flex items-center justify-between">
              <label className="flex items-center gap-1.5 text-xs font-bold text-neutral-800">
                <Calendar className="size-4 text-emerald-600" />
                <span>Sale Starts</span>
              </label>
              <span className="text-[11px] text-neutral-400">Deal opening time</span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* Start Date */}
              <div>
                <label className="block text-[11px] font-medium text-neutral-600 mb-1">
                  Start Date
                </label>
                <input
                  type="date"
                  required
                  disabled={isDealOnSale}
                  value={startDate}
                  onChange={(e) => {
                    setStartDate(e.target.value);
                    setEndDate(e.target.value);
                  }}
                  className={`w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-xs text-neutral-900 shadow-2xs focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 ${
                    isDealOnSale ? "cursor-not-allowed bg-neutral-100 text-neutral-500" : ""
                  }`}
                />
                {/* Date Quick Chips */}
                {!isDealOnSale ? (
                  <div className="flex items-center gap-1.5 mt-2">
                    <button
                      type="button"
                      onClick={() => {
                        const today = formatDate(new Date());
                        setStartDate(today);
                        setEndDate(today);
                      }}
                      className={`rounded-md border px-2 py-0.5 text-[10px] font-medium transition ${
                        startDate === formatDate(new Date())
                          ? "border-emerald-500 bg-emerald-50 text-emerald-800 font-bold"
                          : "border-neutral-200 bg-white text-neutral-600 hover:bg-neutral-100"
                      }`}
                    >
                      Today
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        const tomorrow = new Date();
                        tomorrow.setDate(tomorrow.getDate() + 1);
                        const str = formatDate(tomorrow);
                        setStartDate(str);
                        setEndDate(str);
                      }}
                      className="rounded-md border border-neutral-200 bg-white px-2 py-0.5 text-[10px] font-medium text-neutral-600 hover:bg-neutral-100"
                    >
                      Tomorrow
                    </button>
                  </div>
                ) : (
                  <p className="mt-1.5 text-[10px] font-medium text-amber-600">
                    🔒 Deal is currently live. Start time cannot be modified.
                  </p>
                )}
              </div>

              {/* Start Time */}
              <div>
                <label className="block text-[11px] font-medium text-neutral-600 mb-1">
                  Start Time
                </label>
                <input
                  type="time"
                  required
                  disabled={isDealOnSale}
                  value={startTime}
                  onChange={(e) => setStartTime(e.target.value)}
                  className={`w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-xs text-neutral-900 shadow-2xs focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 ${
                    isDealOnSale ? "cursor-not-allowed bg-neutral-100 text-neutral-500" : ""
                  }`}
                />
                {/* Time Quick Chips */}
                {!isDealOnSale && (
                  <div className="flex flex-wrap items-center gap-1 mt-2">
                    <button
                      type="button"
                      onClick={() => {
                        const now = new Date();
                        const rem = 15 - (now.getMinutes() % 15);
                        const t = new Date(now.getTime() + rem * 60 * 1000);
                        setStartDate(formatDate(t));
                        setStartTime(formatTime(t));
                        setEndDate(formatDate(t));
                      }}
                      className="rounded-md border border-neutral-200 bg-white px-1.5 py-0.5 text-[10px] font-medium text-neutral-600 hover:bg-neutral-100"
                    >
                      Now
                    </button>
                    <button
                      type="button"
                      onClick={() => setStartTime("11:30")}
                      className="rounded-md border border-neutral-200 bg-white px-1.5 py-0.5 text-[10px] font-medium text-neutral-600 hover:bg-neutral-100"
                    >
                      11:30
                    </button>
                    <button
                      type="button"
                      onClick={() => setStartTime("17:00")}
                      className="rounded-md border border-neutral-200 bg-white px-1.5 py-0.5 text-[10px] font-medium text-neutral-600 hover:bg-neutral-100"
                    >
                      17:00
                    </button>
                    <button
                      type="button"
                      onClick={() => setStartTime("18:00")}
                      className="rounded-md border border-neutral-200 bg-white px-1.5 py-0.5 text-[10px] font-medium text-neutral-600 hover:bg-neutral-100"
                    >
                      18:00
                    </button>
                    <button
                      type="button"
                      onClick={() => setStartTime("19:00")}
                      className="rounded-md border border-neutral-200 bg-white px-1.5 py-0.5 text-[10px] font-medium text-neutral-600 hover:bg-neutral-100"
                    >
                      19:00
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Box 2: Order Cutoff */}
          <div className="rounded-xl border border-neutral-200 bg-neutral-50/40 p-4 space-y-3.5">
            <div className="flex items-center justify-between">
              <label className="flex items-center gap-1.5 text-xs font-bold text-neutral-800">
                <Clock className="size-4 text-amber-600" />
                <span>Order Cutoff</span>
              </label>
              <span className="text-[11px] text-neutral-400">Deadline for customer orders</span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* Cutoff Date */}
              <div>
                <label className="block text-[11px] font-medium text-neutral-600 mb-1">
                  Cutoff Date (Same Day)
                </label>
                <input
                  type="date"
                  required
                  readOnly
                  disabled
                  value={startDate}
                  className="w-full rounded-lg border border-neutral-200 bg-neutral-100 px-3 py-2 text-xs text-neutral-600 cursor-not-allowed shadow-2xs"
                />
                <p className="mt-1.5 text-[10px] text-neutral-500">
                  Flash Deal must take place within the same calendar day.
                </p>
              </div>

              {/* Cutoff Time */}
              <div>
                <label className="block text-[11px] font-medium text-neutral-600 mb-1">
                  Cutoff Time (30m - 120m duration)
                </label>
                <input
                  type="time"
                  required
                  value={endTime}
                  onChange={(e) => setEndTime(e.target.value)}
                  className="w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-xs text-neutral-900 shadow-2xs focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
                />
                {/* Duration Quick Chips: 30m, 45m, 60m, 90m, 120m */}
                <div className="flex flex-wrap items-center gap-1 mt-2">
                  {[30, 45, 60, 90, 120].map((mins) => (
                    <button
                      key={mins}
                      type="button"
                      onClick={() => {
                        const s = parseDateTime(startDate, startTime);
                        if (s) {
                          const target = new Date(s.getTime() + mins * 60 * 1000);
                          setEndDate(formatDate(s));
                          setEndTime(formatTime(target));
                        }
                      }}
                      className="rounded-md border border-neutral-200 bg-white px-2 py-0.5 text-[10px] font-medium text-neutral-600 hover:bg-neutral-100 hover:border-emerald-400 hover:text-emerald-700 transition"
                    >
                      +{mins}m
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Live Schedule Summary Preview */}
        {scheduleSummary && (
          <div
            className={`flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 rounded-xl border p-3.5 text-xs ${
              scheduleSummary.isValid
                ? "border-emerald-200 bg-emerald-50/40 text-neutral-800"
                : "border-red-200 bg-red-50 text-red-700"
            }`}
          >
            {scheduleSummary.isValid ? (
              <>
                <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                  <span className="flex items-center gap-1.5 font-bold text-neutral-900">
                    <Clock className="size-3.5 text-emerald-600" />
                    Duration:{" "}
                    {scheduleSummary.hours > 0 ? `${scheduleSummary.hours} hr ` : ""}
                    {scheduleSummary.minutes > 0 ? `${scheduleSummary.minutes} mins` : ""}
                  </span>
                  <span className="text-neutral-400">•</span>
                  <span className="text-neutral-600">
                    From <strong className="text-neutral-800">{startTime}</strong> ({startDate})
                  </span>
                  <span className="text-neutral-400">➔</span>
                  <span className="text-neutral-600">
                    Until <strong className="text-neutral-800">{endTime}</strong> ({endDate})
                  </span>
                </div>

                <div className="text-[11px] text-neutral-500 font-medium">
                  🔒 Store Closes: <strong>{formatTime(scheduleSummary.closingTime)}</strong> (includes 30m pickup buffer)
                </div>
              </>
            ) : (
              <div className="flex items-center gap-2 font-medium">
                <AlertCircle className="size-4 shrink-0 text-red-500" />
                <span>
                  {!scheduleSummary.isSameDay
                    ? "Flash Deals must take place within the same calendar day."
                    : !scheduleSummary.isDurationValid
                    ? `Duration must be between 30 and 120 minutes (currently ${scheduleSummary.durationMins} mins).`
                    : "Cutoff time must be strictly after the sale start time."}
                </span>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Block 3: Discounted Variants */}
      <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs space-y-4">
        <div>
          <h2 className="text-sm font-bold text-neutral-900">Discounted Variants</h2>
          <p className="mt-0.5 text-xs text-neutral-500">
            Set Flash Deal price and inventory quantity for each product variant.
          </p>
        </div>

        {variants.length === 0 ? (
          !selectedProductId ? (
            <div className="rounded-lg border border-dashed border-neutral-200 py-8 text-center text-xs text-neutral-400">
              Select a product from the dropdown above to load and configure its variants.
            </div>
          ) : currentProduct && currentProduct.variants.length === 0 ? (
            <div className="flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50/80 p-4 text-xs text-amber-900">
              <AlertCircle className="size-5 shrink-0 text-amber-600" />
              <div>
                <p className="font-bold">No active variants found for {currentProduct.name}</p>
                <p className="mt-0.5 text-amber-700">
                  This product has no active variants or pricing configured in the store menu. Please choose another product or add variants to this product in Menu Management.
                </p>
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-between rounded-lg border border-neutral-200 bg-neutral-50 p-4 text-xs">
              <p className="text-neutral-600">All variants were removed from this deal.</p>
              <button
                type="button"
                onClick={() => handleSelectProduct(selectedProductId)}
                className="font-bold text-emerald-600 hover:underline"
              >
                Reset all variants
              </button>
            </div>
          )
        ) : (
          <div className="space-y-3">
            {variants.map((v, index) => {
              const discountPercent =
                typeof v.dealPrice === "number" && v.originalPrice > 0
                  ? Math.round(((v.originalPrice - v.dealPrice) / v.originalPrice) * 100)
                  : 0;

              return (
                <div
                  key={v.variantId}
                  className="flex flex-col gap-3 rounded-lg border border-neutral-200 bg-white p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div className="min-w-32">
                    <p className="font-semibold text-xs text-neutral-900">{v.variantName}</p>
                    {v.sku && <p className="text-[10px] text-neutral-400">{v.sku}</p>}
                    <p className="mt-1 text-xs text-neutral-500">
                      Original: <span className="font-medium text-neutral-700">{v.originalPrice.toLocaleString("vi-VN")}đ</span>
                    </p>
                  </div>

                  <div className="grid grid-cols-2 gap-3 sm:flex sm:items-center sm:gap-4">
                    {/* Deal Price Input */}
                    <div className="w-full sm:w-36">
                      <label className="block text-[11px] font-medium text-neutral-500">
                        Deal Price
                      </label>
                      <div className="relative mt-1">
                        <input
                          type="number"
                          min="1000"
                          step="1000"
                          required
                          value={v.dealPrice}
                          onChange={(e) => handleUpdateVariant(index, "dealPrice", e.target.value)}
                          placeholder="25000"
                          className="w-full rounded-md border border-neutral-200 py-1.5 pl-2.5 pr-6 text-xs text-neutral-900 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
                        />
                        <span className="absolute right-2 top-1.5 text-[11px] text-neutral-400">đ</span>
                      </div>
                      {discountPercent > 0 && (
                        <p className="mt-0.5 text-[10px] font-medium text-emerald-600">
                          {discountPercent}% off
                        </p>
                      )}
                    </div>

                    {/* Quantity Input */}
                    <div className="w-full sm:w-36">
                      <label className="block text-[11px] font-medium text-neutral-500">
                        Quantity
                      </label>
                      <input
                        type="number"
                        min="1"
                        required
                        value={v.totalQuantity}
                        onChange={(e) => handleUpdateVariant(index, "totalQuantity", e.target.value)}
                        placeholder="100"
                        className="mt-1 w-full rounded-md border border-neutral-200 py-1.5 px-2.5 text-xs text-neutral-900 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
                      />
                      {v.soldQuantity !== undefined && (
                        <p className="mt-0.5 text-[10px] text-neutral-400">
                          Sold {v.soldQuantity}, reserved {v.reservedQuantity || 0}
                        </p>
                      )}
                    </div>

                    {/* Status Toggle in Edit Mode */}
                    {isEditMode && (
                      <div className="w-full sm:w-28">
                        <label className="block text-[11px] font-medium text-neutral-500">
                          Status
                        </label>
                        <button
                          type="button"
                          onClick={() => {
                            const newStatus =
                              v.status === FlashDealVariantStatusEnum.Hide
                                ? FlashDealVariantStatusEnum.Active
                                : FlashDealVariantStatusEnum.Hide;
                            handleUpdateVariant(index, "status", newStatus);
                          }}
                          className={`mt-1 inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-xs font-semibold transition ${
                            v.status === FlashDealVariantStatusEnum.Hide
                              ? "bg-amber-50 text-amber-700 border border-amber-200 hover:bg-amber-100"
                              : "bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-100"
                          }`}
                        >
                          {v.status === FlashDealVariantStatusEnum.Hide ? (
                            <>
                              <EyeOff className="size-3.5" />
                              <span>Hidden</span>
                            </>
                          ) : (
                            <>
                              <Eye className="size-3.5" />
                              <span>Active</span>
                            </>
                          )}
                        </button>
                      </div>
                    )}

                    {/* Delete variant */}
                    <button
                      type="button"
                      title="Remove variant"
                      onClick={() => handleRemoveVariant(index)}
                      className="self-end sm:self-center mt-2 sm:mt-4 p-1.5 text-neutral-400 hover:text-red-500"
                    >
                      <Trash2 className="size-4" />
                    </button>
                  </div>
                </div>
              );
            })}

            {/* Add remaining variants button */}
            {remainingProductVariants.length > 0 && (
              <div className="flex items-center gap-2 pt-2">
                <span className="text-xs text-neutral-500">Add variant:</span>
                {remainingProductVariants.map((v) => (
                  <button
                    key={v.id}
                    type="button"
                    onClick={() => handleAddVariant(v)}
                    className="inline-flex items-center gap-1 rounded-md border border-neutral-200 bg-neutral-50 px-2.5 py-1 text-xs text-neutral-700 hover:bg-neutral-100"
                  >
                    <Plus className="size-3 text-neutral-400" />
                    <span>{v.variantName || v.sku}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Footer Actions */}
      <div className="flex items-center justify-end gap-3 pt-2">
        <button
          type="button"
          onClick={() => navigate(APP_PATHS.STORE_OWNER_FLASH_DEALS)}
          className="rounded-lg border border-neutral-200 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 transition hover:bg-neutral-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={submitting}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-emerald-600 px-5 py-2 text-xs font-semibold text-white shadow-xs transition hover:bg-emerald-700 disabled:opacity-60"
        >
          {submitting && <Loader2 className="size-3.5 animate-spin" />}
          <span>Save Flash Deal</span>
        </button>
      </div>
    </form>
  );
}
