import { useEffect, useState, useMemo, useRef } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  ArrowLeft,
  Loader2,
  Clock,
  AlertCircle,
  ChevronDown,
  Eye,
  EyeOff,
  Check,
  Zap,
} from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import {
  FlashDealStatusEnum,
  FlashDealVariantStatusEnum,
  type ShopEligibleProduct,
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
  isSelected: boolean;
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

  // Selling mode: "immediate" (sell immediately) or "scheduled" (schedule for later)
  const [saleMode, setSaleMode] = useState<"immediate" | "scheduled">("immediate");

  // Timing states (clean HH:mm, duration in minutes)
  const [startTime, setStartTime] = useState<string>("");
  const [endTime, setEndTime] = useState<string>("");
  const [durationMins, setDurationMins] = useState<number>(60);

  // Configured variants with checkbox selection
  const [variants, setVariants] = useState<FormVariantState[]>([]);

  // Whether current deal is already OnSale
  const isDealOnSale = isEditMode && existingDeal?.status === FlashDealStatusEnum.OnSale;

  // Time format helper
  function formatTime(d: Date): string {
    const hours = String(d.getHours()).padStart(2, "0");
    const minutes = String(d.getMinutes()).padStart(2, "0");
    return `${hours}:${minutes}`;
  }

  // Get Start timestamp based on mode and time string
  function getStartTimestamp(mode: "immediate" | "scheduled", timeStr: string): Date {
    const now = new Date();
    if (mode === "immediate") {
      return now;
    }
    if (!timeStr) {
      return new Date(now.getTime() + 30 * 60 * 1000);
    }
    const [h, m] = timeStr.split(":").map(Number);
    const d = new Date(now.getFullYear(), now.getMonth(), now.getDate(), h, m, 0, 0);
    // If chosen time is earlier than current time by > 15 minutes, wrap to tomorrow
    if (d.getTime() < now.getTime() - 15 * 60 * 1000) {
      d.setDate(d.getDate() + 1);
    }
    return d;
  }

  // Get End timestamp handling midnight rollover
  function getEndTimestamp(startDate: Date, timeStr: string, fallbackMins = 60): Date {
    if (!timeStr) {
      return new Date(startDate.getTime() + fallbackMins * 60 * 1000);
    }
    const [h, m] = timeStr.split(":").map(Number);
    const d = new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate(), h, m, 0, 0);
    if (d.getTime() <= startDate.getTime()) {
      // End time is after midnight (rolls into next day)
      d.setDate(d.getDate() + 1);
    }
    return d;
  }

  // Initial load
  useEffect(() => {
    async function init() {
      try {
        setInitialLoading(true);
        const products = await flashDealApi.getEligibleProducts();
        setEligibleProducts(products);

        if (isEditMode && id) {
          // Edit mode: fetch deal detail
          const deal = await flashDealApi.getShopFlashDealDetail(id);
          setExistingDeal(deal);
          setSelectedProductId(deal.productId);
          setSaleMode(deal.status === FlashDealStatusEnum.OnSale ? "immediate" : "scheduled");

          const start = new Date(deal.saleStartTime);
          const end = new Date(deal.orderEndTime);
          setStartTime(formatTime(start));
          setEndTime(formatTime(end));
          const diff = Math.round((end.getTime() - start.getTime()) / (60 * 1000));
          setDurationMins(diff > 0 ? diff : 60);

          const prod = products.find((p) => p.id === deal.productId);
          const existingMap = new Map(deal.variants.map((v) => [v.variantId, v]));

          if (prod && prod.variants.length > 0) {
            setVariants(
              prod.variants.map((pv) => {
                const ex = existingMap.get(pv.id);
                if (ex) {
                  return {
                    variantId: ex.variantId,
                    sku: ex.sku,
                    variantName: ex.variantName || ex.sku || "Default",
                    originalPrice: ex.originalPrice,
                    dealPrice: ex.dealPrice,
                    totalQuantity: ex.totalQuantity,
                    soldQuantity: ex.soldQuantity,
                    reservedQuantity: ex.reservedQuantity,
                    status: ex.status ?? FlashDealVariantStatusEnum.Active,
                    isSelected: true,
                  };
                } else {
                  return {
                    variantId: pv.id,
                    sku: pv.sku,
                    variantName: pv.variantName || pv.sku || "Default",
                    originalPrice: pv.originalPrice,
                    dealPrice: Math.round(pv.originalPrice * 0.7),
                    totalQuantity: 50,
                    status: FlashDealVariantStatusEnum.Active,
                    isSelected: false,
                  };
                }
              })
            );
          } else {
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
                isSelected: true,
              }))
            );
          }
        } else {
          // Create mode default: immediate selling mode
          const now = new Date();
          const start = new Date(now.getTime() + 30 * 60 * 1000);
          const end = new Date(now.getTime() + 60 * 60 * 1000);
          setStartTime(formatTime(start));
          setEndTime(formatTime(end));
          setDurationMins(60);
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
    const now = new Date();
    const isImmediate = !isEditMode ? saleMode === "immediate" : (existingDeal?.status === FlashDealStatusEnum.OnSale);

    const start = isImmediate ? now : getStartTimestamp("scheduled", startTime);
    const end = getEndTimestamp(start, endTime, durationMins || 60);

    const diffMs = end.getTime() - start.getTime();
    const computedDurationMins = Math.round(diffMs / (1000 * 60));
    const isDurationValid = computedDurationMins >= 30 && computedDurationMins <= 120;

    let isAdvanceValid = true;
    let advanceErrorMessage = "";

    if (!isImmediate && !isDealOnSale) {
      const diffStartMins = Math.round((start.getTime() - now.getTime()) / (1000 * 60));
      if (diffStartMins < 14) {
        isAdvanceValid = false;
        advanceErrorMessage = "Start time must be at least 15 minutes from now, or choose immediate selling.";
      } else if (diffStartMins > 91) {
        isAdvanceValid = false;
        advanceErrorMessage = `Flash deals can only be scheduled up to 90 minutes in advance (currently: ${diffStartMins} mins).`;
      }
    }

    let durationErrorMessage = "";
    if (!isDurationValid) {
      durationErrorMessage = `Deal duration must be between 30 and 120 minutes (currently ${computedDurationMins} mins).`;
    }

    const isValid = diffMs > 0 && isDurationValid && isAdvanceValid;
    const errorMessage = advanceErrorMessage || durationErrorMessage || (diffMs <= 0 ? "End time must be after start time." : "");

    const hours = Math.floor(computedDurationMins / 60);
    const minutes = computedDurationMins % 60;

    const isCurrentlyActive = isImmediate || (start <= now && end >= now);
    const isUpcoming = !isImmediate && start > now;

    // Closing time is order cutoff + 30 mins
    const closingTime = new Date(end.getTime() + 30 * 60 * 1000);
    const isNextDay = end.getDate() !== start.getDate();

    return {
      start,
      end,
      isValid,
      durationMins: computedDurationMins,
      isDurationValid,
      isAdvanceValid,
      errorMessage,
      hours,
      minutes,
      isCurrentlyActive,
      isUpcoming,
      closingTime,
      isImmediate,
      isNextDay,
    };
  }, [startTime, endTime, durationMins, saleMode, isEditMode, isDealOnSale, existingDeal]);

  // Quick duration selection
  function handleSelectDuration(mins: number) {
    setDurationMins(mins);
    const now = new Date();
    const isImmediate = !isEditMode ? saleMode === "immediate" : (existingDeal?.status === FlashDealStatusEnum.OnSale);
    const start = isImmediate ? now : getStartTimestamp("scheduled", startTime);
    const end = new Date(start.getTime() + mins * 60 * 1000);
    setEndTime(formatTime(end));
  }

  // End time input change
  function handleEndTimeChange(newEndTime: string) {
    setEndTime(newEndTime);
    if (!newEndTime) return;
    const now = new Date();
    const isImmediate = !isEditMode ? saleMode === "immediate" : (existingDeal?.status === FlashDealStatusEnum.OnSale);
    const start = isImmediate ? now : getStartTimestamp("scheduled", startTime);
    const end = getEndTimestamp(start, newEndTime);
    const diff = Math.round((end.getTime() - start.getTime()) / (60 * 1000));
    setDurationMins(diff);
  }

  // Start time input change
  function handleStartTimeChange(newStartTime: string) {
    setStartTime(newStartTime);
    if (!newStartTime) return;
    const start = getStartTimestamp("scheduled", newStartTime);
    const end = new Date(start.getTime() + (durationMins || 60) * 60 * 1000);
    setEndTime(formatTime(end));
  }

  // Start time quick offset (+15m, +30m, etc.)
  function handleSetStartOffset(offsetMins: number) {
    const target = new Date(Date.now() + offsetMins * 60 * 1000);
    const newStartStr = formatTime(target);
    setStartTime(newStartStr);
    const end = new Date(target.getTime() + (durationMins || 60) * 60 * 1000);
    setEndTime(formatTime(end));
  }

  // Mode switch handler
  function handleSwitchMode(mode: "immediate" | "scheduled") {
    setSaleMode(mode);
    const now = new Date();
    if (mode === "immediate") {
      setDurationMins(60);
      const end = new Date(now.getTime() + 60 * 60 * 1000);
      setEndTime(formatTime(end));
    } else {
      const start = new Date(now.getTime() + 30 * 60 * 1000);
      const end = new Date(start.getTime() + 60 * 60 * 1000);
      setStartTime(formatTime(start));
      setEndTime(formatTime(end));
      setDurationMins(60);
    }
  }

  // Handle selecting product in create mode
  function handleSelectProduct(productId: string) {
    setSelectedProductId(productId);
    const prod = eligibleProducts.find((p) => p.id === productId);
    if (!prod) {
      setVariants([]);
      return;
    }

    // Populate all variants of this product with checkboxes checked
    setVariants(
      prod.variants.map((v) => ({
        variantId: v.id,
        sku: v.sku,
        variantName: v.variantName || v.sku || "Default",
        originalPrice: v.originalPrice,
        dealPrice: Math.round(v.originalPrice * 0.7), // Suggest 30% discount default
        totalQuantity: 50,
        isSelected: true,
        status: FlashDealVariantStatusEnum.Active,
      }))
    );
  }

  // Toggle variant checkbox
  function toggleSelectVariant(variantId: string) {
    setVariants((prev) =>
      prev.map((v) => {
        if (v.variantId !== variantId) return v;
        if (v.isSelected && v.soldQuantity && v.soldQuantity > 0) {
          toast.error(`Variant ${v.variantName} already has orders. Please switch its status to Hide instead.`);
          return v;
        }
        return { ...v, isSelected: !v.isSelected };
      })
    );
  }

  // Toggle select all variants
  function toggleSelectAll(selectAll: boolean) {
    setVariants((prev) =>
      prev.map((v) => {
        if (!selectAll && v.soldQuantity && v.soldQuantity > 0) {
          return v;
        }
        return { ...v, isSelected: selectAll };
      })
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

  // Count selected variants
  const selectedVariantsCount = variants.filter((v) => v.isSelected).length;

  // Submit form
  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    if (!selectedProductId) {
      toast.error("Please select a product.");
      return;
    }

    const isImmediate = !isEditMode ? saleMode === "immediate" : (existingDeal?.status === FlashDealStatusEnum.OnSale);

    if (!scheduleSummary || !scheduleSummary.isValid) {
      toast.error(scheduleSummary?.errorMessage || "Please enter valid deal schedule timing.");
      return;
    }

    const { start: startDateObj, end: endDateObj, closingTime: closingDateObj } = scheduleSummary;

    const selectedVariants = variants.filter((v) => v.isSelected);
    if (selectedVariants.length === 0) {
      toast.error("Please tick at least one variant for the flash deal.");
      return;
    }

    for (const v of selectedVariants) {
      const priceNum = Number(v.dealPrice);
      if (v.dealPrice === "" || isNaN(priceNum) || priceNum <= 0) {
        toast.error(`Deal price for ${v.variantName} must be greater than 0.`);
        return;
      }
      if (priceNum < 1000) {
        toast.error(`Deal price for ${v.variantName} must be at least 1,000 VND.`);
        return;
      }
      if (priceNum % 1000 !== 0) {
        const floor = Math.floor(priceNum / 1000) * 1000;
        const ceil = Math.ceil(priceNum / 1000) * 1000;
        toast.error(
          `Deal price for ${v.variantName} must be a multiple of 1,000 VND. The two nearest valid values are ${floor.toLocaleString("vi-VN")}đ and ${ceil.toLocaleString("vi-VN")}đ.`
        );
        return;
      }
      if (priceNum >= v.originalPrice) {
        toast.error(`Deal price for ${v.variantName} must be lower than original price (${v.originalPrice.toLocaleString("vi-VN")}đ).`);
        return;
      }
      const qtyNum = Number(v.totalQuantity);
      if (v.totalQuantity === "" || isNaN(qtyNum) || qtyNum <= 0 || !Number.isInteger(qtyNum)) {
        toast.error(`Deal quantity for ${v.variantName} must be a positive integer.`);
        return;
      }
      if (v.soldQuantity !== undefined && qtyNum < (v.soldQuantity + (v.reservedQuantity || 0))) {
        toast.error(`Deal quantity for ${v.variantName} cannot be less than already sold quantity (${v.soldQuantity}).`);
        return;
      }
    }

    try {
      setSubmitting(true);
      if (isEditMode && id) {
        await flashDealApi.updateFlashDeal(id, {
          saleStartTime: startDateObj.toISOString(),
          orderEndTime: endDateObj.toISOString(),
          shopClosingTime: closingDateObj.toISOString(),
          variants: selectedVariants.map((v) => ({
            variantId: v.variantId,
            dealPrice: Number(v.dealPrice),
            totalQuantity: Number(v.totalQuantity),
            status: v.status ?? FlashDealVariantStatusEnum.Active,
          })),
        });
        toast.success("Flash deal updated successfully!");
      } else {
        await flashDealApi.createFlashDeal({
          productId: selectedProductId,
          isImmediate: isImmediate,
          saleStartTime: isImmediate ? undefined : startDateObj.toISOString(),
          orderEndTime: endDateObj.toISOString(),
          shopClosingTime: closingDateObj.toISOString(),
          variants: selectedVariants.map((v) => ({
            variantId: v.variantId,
            dealPrice: Number(v.dealPrice),
            totalQuantity: Number(v.totalQuantity),
          })),
        });
        toast.success(isImmediate ? "Flash deal created and activated immediately!" : "Flash deal scheduled successfully!");
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
    <form onSubmit={handleSubmit} noValidate className="space-y-6">
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
          Select a product, configure deal timing, and tick variants to offer special flash prices.
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

      {/* Block 2: Sale Mode & Schedule (Unified 1-Container Layout, No Date Pickers) */}
      <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs space-y-5">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 border-b border-neutral-100 pb-3.5">
          <div>
            <h2 className="text-sm font-bold text-neutral-900">Sale Mode & Schedule</h2>
            <p className="mt-0.5 text-xs text-neutral-500">
              Choose to sell immediately or schedule sale opening ahead (15m - 90m). Deal runs for 30m to 120m.
            </p>
          </div>

          {/* Dynamic Live Status Badge */}
          {scheduleSummary?.isValid ? (
            scheduleSummary.isCurrentlyActive ? (
              <span className="inline-flex items-center gap-1.5 self-start sm:self-auto rounded-full bg-emerald-50 px-3 py-1 text-xs font-bold text-emerald-700 ring-1 ring-inset ring-emerald-600/20">
                <span className="size-2 rounded-full bg-emerald-500 animate-pulse" />
                ⚡ Active Immediately
              </span>
            ) : scheduleSummary.isUpcoming ? (
              <span className="inline-flex items-center gap-1.5 self-start sm:self-auto rounded-full bg-blue-50 px-3 py-1 text-xs font-bold text-blue-700 ring-1 ring-inset ring-blue-600/20">
                ⏰ Scheduled (Pending)
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 self-start sm:self-auto rounded-full bg-neutral-100 px-3 py-1 text-xs font-medium text-neutral-600">
                Ended
              </span>
            )
          ) : (
            <span className="inline-flex items-center gap-1.5 self-start sm:self-auto rounded-full bg-amber-50 px-3 py-1 text-xs font-medium text-amber-700 ring-1 ring-inset ring-amber-600/20">
              Incomplete / Invalid Timing
            </span>
          )}
        </div>

        {/* Mode selection buttons */}
        {!isDealOnSale && (
          <div>
            <label className="block text-xs font-semibold text-neutral-700 mb-2">
              Select Selling Mode <span className="text-red-500">*</span>
            </label>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {/* Option 1: Sell Immediately */}
              <button
                type="button"
                disabled={isEditMode}
                onClick={() => handleSwitchMode("immediate")}
                className={`group relative flex items-start gap-3.5 rounded-xl border p-4 text-left transition-all ${
                  saleMode === "immediate"
                    ? "border-emerald-500 bg-emerald-50/50 shadow-xs ring-1 ring-emerald-500"
                    : "border-neutral-200 bg-white hover:border-neutral-300 hover:bg-neutral-50/50"
                } ${isEditMode ? "opacity-75 cursor-not-allowed" : ""}`}
              >
                <div
                  className={`mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg transition-colors ${
                    saleMode === "immediate"
                      ? "bg-emerald-600 text-white shadow-2xs"
                      : "bg-neutral-100 text-neutral-500 group-hover:bg-neutral-200"
                  }`}
                >
                  <Zap className="size-5" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between">
                    <p className="text-xs font-bold text-neutral-900 truncate">
                      Sell Immediately
                    </p>
                    <span
                      className={`rounded-full px-2 py-0.5 text-[10px] font-semibold shrink-0 ${
                        saleMode === "immediate"
                          ? "bg-emerald-100 text-emerald-800"
                          : "bg-neutral-100 text-neutral-600"
                      }`}
                    >
                      Active Now
                    </span>
                  </div>
                  <p className="mt-1 text-[11px] leading-relaxed text-neutral-500">
                    Deal is activated immediately upon saving. Customers can order right away, you only need to select duration.
                  </p>
                </div>
              </button>

              {/* Option 2: Schedule for Later */}
              <button
                type="button"
                disabled={isEditMode}
                onClick={() => handleSwitchMode("scheduled")}
                className={`group relative flex items-start gap-3.5 rounded-xl border p-4 text-left transition-all ${
                  saleMode === "scheduled"
                    ? "border-emerald-500 bg-emerald-50/50 shadow-xs ring-1 ring-emerald-500"
                    : "border-neutral-200 bg-white hover:border-neutral-300 hover:bg-neutral-50/50"
                } ${isEditMode ? "opacity-75 cursor-not-allowed" : ""}`}
              >
                <div
                  className={`mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg transition-colors ${
                    saleMode === "scheduled"
                      ? "bg-emerald-600 text-white shadow-2xs"
                      : "bg-neutral-100 text-neutral-500 group-hover:bg-neutral-200"
                  }`}
                >
                  <Clock className="size-5" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between">
                    <p className="text-xs font-bold text-neutral-900 truncate">
                      Schedule for Later
                    </p>
                    <span
                      className={`rounded-full px-2 py-0.5 text-[10px] font-semibold shrink-0 ${
                        saleMode === "scheduled"
                          ? "bg-emerald-100 text-emerald-800"
                          : "bg-neutral-100 text-neutral-600"
                      }`}
                    >
                      Ahead (15m - 90m)
                    </span>
                  </div>
                  <p className="mt-1 text-[11px] leading-relaxed text-neutral-500">
                    Schedule sale opening 15 to 90 minutes in advance. The deal will automatically go live at your chosen time.
                  </p>
                </div>
              </button>
            </div>
          </div>
        )}

        {/* Unified Timing Controls (No Date Pickers) */}
        <div className="rounded-xl border border-neutral-200/90 bg-neutral-50/50 p-4 space-y-4">
          <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
            {/* Start Time Section */}
            <div className="space-y-2">
              <label className="flex items-center justify-between text-xs font-bold text-neutral-800">
                <span className="flex items-center gap-1.5">
                  {saleMode === "immediate" ? (
                    <Zap className="size-4 text-emerald-600" />
                  ) : (
                    <Clock className="size-4 text-emerald-600" />
                  )}
                  <span>Sale Start Time</span>
                </span>
                <span className="text-[10px] text-neutral-400 font-normal">
                  {saleMode === "immediate" ? "Automatic live start" : "15m - 90m in advance"}
                </span>
              </label>

              {saleMode === "immediate" ? (
                <div className="rounded-lg border border-emerald-200 bg-emerald-50/70 p-3 space-y-1.5">
                  <div className="flex items-center gap-2">
                    <span className="size-2 rounded-full bg-emerald-500 animate-pulse" />
                    <span className="text-xs font-bold text-emerald-900">
                      Starts immediately upon saving
                    </span>
                  </div>
                  <p className="text-[11px] leading-relaxed text-emerald-800">
                    Customers will see and can order this deal as soon as it is saved.
                  </p>
                </div>
              ) : (
                <div className="space-y-2">
                  <input
                    type="time"
                    required
                    disabled={isDealOnSale}
                    value={startTime}
                    onChange={(e) => handleStartTimeChange(e.target.value)}
                    className="w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-xs text-neutral-900 shadow-2xs focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 font-semibold"
                  />
                  {!isDealOnSale && (
                    <div className="flex flex-wrap items-center gap-1.5 pt-0.5">
                      <span className="text-[10px] text-neutral-400 font-medium">Quick start:</span>
                      {[15, 30, 45, 60, 90].map((mins) => (
                        <button
                          key={mins}
                          type="button"
                          onClick={() => handleSetStartOffset(mins)}
                          className="rounded-md border border-neutral-200 bg-white px-2 py-0.5 text-[10px] font-medium text-neutral-600 hover:bg-neutral-100 hover:border-emerald-400 hover:text-emerald-700 transition"
                        >
                          +{mins}m
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Cutoff Time & Duration Section */}
            <div className="space-y-2">
              <label className="flex items-center justify-between text-xs font-bold text-neutral-800">
                <span className="flex items-center gap-1.5">
                  <Clock className="size-4 text-emerald-600" />
                  <span>Order Cutoff Time</span>
                </span>
                <span className="text-[10px] text-neutral-400 font-normal">
                  Duration: 30m - 120m
                </span>
              </label>

              <div className="space-y-2">
                <div className="relative">
                  <input
                    type="time"
                    required
                    value={endTime}
                    onChange={(e) => handleEndTimeChange(e.target.value)}
                    className="w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-xs text-neutral-900 shadow-2xs focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 font-semibold"
                  />
                  {scheduleSummary?.isNextDay && (
                    <span className="absolute right-2.5 top-2 rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-semibold text-amber-800">
                      Next Day
                    </span>
                  )}
                </div>

                {/* Duration Chips */}
                <div className="flex flex-wrap items-center gap-1.5 pt-0.5">
                  <span className="text-[10px] text-neutral-400 font-medium">Duration:</span>
                  {[30, 45, 60, 90, 120].map((mins) => {
                    const isSelected = durationMins === mins;
                    return (
                      <button
                        key={mins}
                        type="button"
                        onClick={() => handleSelectDuration(mins)}
                        className={`rounded-md border px-2 py-0.5 text-[10px] font-semibold transition ${
                          isSelected
                            ? "border-emerald-500 bg-emerald-600 text-white shadow-2xs"
                            : "border-neutral-200 bg-white text-neutral-600 hover:bg-neutral-100 hover:border-emerald-400 hover:text-emerald-700"
                        }`}
                      >
                        {mins}m
                      </button>
                    );
                  })}
                </div>
              </div>
            </div>
          </div>

          {/* Live Schedule Summary inside this container */}
          <div
            className={`rounded-lg border p-3.5 text-xs transition-all ${
              scheduleSummary?.isValid
                ? "border-emerald-200 bg-emerald-50/70 text-emerald-950"
                : "border-red-200 bg-red-50 text-red-700"
            }`}
          >
            {scheduleSummary?.isValid ? (
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2.5">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-bold flex items-center gap-1">
                    <Clock className="size-3.5 text-emerald-600" />
                    Duration: {scheduleSummary.durationMins} mins
                    {scheduleSummary.hours > 0 && ` (${scheduleSummary.hours}h${scheduleSummary.minutes > 0 ? ` ${scheduleSummary.minutes}m` : ""})`}
                  </span>
                  <span className="text-neutral-300">•</span>
                  <span>
                    {saleMode === "immediate" ? "Live: Immediately" : `Starts: ${startTime}`}
                  </span>
                  <span className="text-neutral-400">➔</span>
                  <span>
                    Cutoff: <strong>{endTime}</strong> {scheduleSummary.isNextDay && <strong className="text-amber-700">(Next Day)</strong>}
                  </span>
                </div>
                <div className="text-[11px] font-medium text-neutral-600">
                  🔒 Store Closes: <strong>{formatTime(scheduleSummary.closingTime)}</strong> (includes 30m pickup buffer)
                </div>
              </div>
            ) : (
              <div className="flex items-center gap-2 font-medium">
                <AlertCircle className="size-4 shrink-0 text-red-500" />
                <span>{scheduleSummary?.errorMessage || "Please enter valid deal schedule timing."}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Block 3: Discounted Variants (Checkbox-based selection) */}
      <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 border-b border-neutral-100 pb-3">
          <div>
            <h2 className="text-sm font-bold text-neutral-900">Discounted Variants</h2>
            <p className="mt-0.5 text-xs text-neutral-500">
              Tick the variants you want to offer in this Flash Deal. Set deal price and inventory quantity for each selected variant.
            </p>
          </div>

          {variants.length > 0 && (
            <div className="flex items-center gap-3 self-start sm:self-auto">
              <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-semibold text-emerald-700 border border-emerald-200">
                {selectedVariantsCount} / {variants.length} selected
              </span>
              <button
                type="button"
                onClick={() => toggleSelectAll(selectedVariantsCount < variants.length)}
                className="text-xs font-semibold text-emerald-600 hover:text-emerald-700 hover:underline"
              >
                {selectedVariantsCount < variants.length ? "Select All" : "Deselect All"}
              </button>
            </div>
          )}
        </div>

        {variants.length === 0 ? (
          !selectedProductId ? (
            <div className="rounded-lg border border-dashed border-neutral-200 py-8 text-center text-xs text-neutral-400">
              Select a product from the dropdown above to load and configure its variants.
            </div>
          ) : (
            <div className="flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50/80 p-4 text-xs text-amber-900">
              <AlertCircle className="size-5 shrink-0 text-amber-600" />
              <div>
                <p className="font-bold">No variants found for {currentProduct?.name}</p>
                <p className="mt-0.5 text-amber-700">
                  This product has no active variants. Please choose another product or add variants in Menu Management.
                </p>
              </div>
            </div>
          )
        ) : (
          <div className="space-y-3">
            {variants.map((v, index) => {
              const priceNum = typeof v.dealPrice === "number" ? v.dealPrice : Number(v.dealPrice);
              const isPriceEntered = v.dealPrice !== "" && !isNaN(priceNum);
              const isNotMultipleOf1000 = isPriceEntered && priceNum > 0 && priceNum % 1000 !== 0;
              const isPriceGteOriginal = isPriceEntered && priceNum >= v.originalPrice;
              const isPriceTooLow = isPriceEntered && priceNum < 1000;
              const hasPriceError = isNotMultipleOf1000 || isPriceGteOriginal || isPriceTooLow;

              const nearestFloor = isPriceEntered ? Math.max(1000, Math.floor(priceNum / 1000) * 1000) : 1000;
              const nearestCeil = isPriceEntered ? Math.ceil(priceNum / 1000) * 1000 : 1000;

              const discountPercent =
                isPriceEntered && v.originalPrice > 0 && priceNum < v.originalPrice && priceNum > 0
                  ? Math.round(((v.originalPrice - priceNum) / v.originalPrice) * 100)
                  : 0;

              const hasOrders = Boolean(v.soldQuantity && v.soldQuantity > 0);

              return (
                <div
                  key={v.variantId}
                  className={`flex flex-col gap-3.5 rounded-xl border p-4 transition-all sm:flex-row sm:items-center sm:justify-between shadow-2xs ${
                    v.isSelected
                      ? "border-emerald-300 bg-white ring-1 ring-emerald-500/10"
                      : "border-neutral-200 bg-neutral-50/60 opacity-80 hover:opacity-100"
                  }`}
                >
                  {/* Left: Checkbox + Variant Information (Vertically Centered) */}
                  <div
                    onClick={() => !hasOrders && toggleSelectVariant(v.variantId)}
                    className="flex items-center gap-3.5 sm:w-72 shrink-0 cursor-pointer select-none"
                  >
                    <input
                      type="checkbox"
                      id={`var-select-${v.variantId}`}
                      checked={v.isSelected}
                      disabled={hasOrders}
                      onChange={() => toggleSelectVariant(v.variantId)}
                      onClick={(e) => e.stopPropagation()}
                      className="size-4.5 rounded-md border-neutral-300 text-emerald-600 focus:ring-emerald-500 cursor-pointer disabled:cursor-not-allowed accent-emerald-600 shrink-0"
                    />

                    <div className="min-w-0">
                      <label
                        htmlFor={`var-select-${v.variantId}`}
                        className="font-bold text-xs text-neutral-900 cursor-pointer block truncate hover:text-emerald-700 transition-colors"
                      >
                        {v.variantName}
                      </label>
                      {v.sku && (
                        <p className="text-[10px] text-neutral-400 font-mono mt-0.5">{v.sku}</p>
                      )}
                      <p className="mt-0.5 text-xs text-neutral-500">
                        Original: <span className="font-semibold text-neutral-700">{v.originalPrice.toLocaleString("vi-VN")}đ</span>
                      </p>
                      {hasOrders && (
                        <span className="mt-1 inline-block rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-semibold text-amber-800">
                          Has orders (cannot uncheck)
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Right: Inputs & Actions */}
                  {v.isSelected ? (
                    <div className="flex flex-1 flex-wrap items-center justify-end gap-3 sm:gap-4">
                      {/* Deal Price Input */}
                      <div className="w-full sm:w-44">
                        <label className="block text-[11px] font-medium text-neutral-600 mb-1">
                          Flash Deal Price
                        </label>
                        <div className="relative">
                          <input
                            type="number"
                            min="1000"
                            step="1000"
                            required
                            value={v.dealPrice}
                            onChange={(e) => handleUpdateVariant(index, "dealPrice", e.target.value)}
                            placeholder="25000"
                            className={`w-full rounded-lg border py-1.5 pl-3 pr-7 text-xs text-neutral-900 shadow-2xs font-semibold focus:outline-none focus:ring-1 ${
                              hasPriceError
                                ? "border-amber-400 bg-amber-50/20 focus:border-amber-500 focus:ring-amber-500"
                                : "border-neutral-300 bg-white focus:border-emerald-500 focus:ring-emerald-500"
                            }`}
                          />
                          <span className="absolute right-2.5 top-1.5 text-xs text-neutral-400 font-medium">đ</span>
                        </div>

                        {/* Real-time Inline Validation Message & Nearest Suggestions */}
                        {isNotMultipleOf1000 ? (
                          <div className="mt-1 space-y-1">
                            <p className="text-[10px] font-medium text-amber-600">
                              Must be multiple of 1,000đ
                            </p>
                            <div className="flex items-center gap-1 text-[10px]">
                              <span className="text-neutral-400">Nearest:</span>
                              <button
                                type="button"
                                onClick={() => handleUpdateVariant(index, "dealPrice", nearestFloor)}
                                className="rounded bg-neutral-100 hover:bg-emerald-50 hover:text-emerald-700 px-1.5 py-0.5 font-bold text-neutral-700 border border-neutral-200 transition text-[10px]"
                              >
                                {nearestFloor.toLocaleString("vi-VN")}đ
                              </button>
                              {nearestCeil !== nearestFloor && (
                                <button
                                  type="button"
                                  onClick={() => handleUpdateVariant(index, "dealPrice", nearestCeil)}
                                  className="rounded bg-neutral-100 hover:bg-emerald-50 hover:text-emerald-700 px-1.5 py-0.5 font-bold text-neutral-700 border border-neutral-200 transition text-[10px]"
                                >
                                  {nearestCeil.toLocaleString("vi-VN")}đ
                                </button>
                              )}
                            </div>
                          </div>
                        ) : isPriceGteOriginal ? (
                          <p className="mt-1 text-[10px] font-medium text-red-500">
                            Must be lower than {v.originalPrice.toLocaleString("vi-VN")}đ
                          </p>
                        ) : isPriceTooLow ? (
                          <p className="mt-1 text-[10px] font-medium text-red-500">
                            Must be at least 1,000đ
                          </p>
                        ) : discountPercent > 0 ? (
                          <p className="mt-1 text-[10px] font-semibold text-emerald-600">
                            {discountPercent}% off original price
                          </p>
                        ) : (
                          <p className="mt-1 text-[10px] text-neutral-400">Enter deal price</p>
                        )}
                      </div>

                      {/* Quantity Input */}
                      <div className="w-full sm:w-36">
                        <label className="block text-[11px] font-medium text-neutral-600 mb-1">
                          Deal Quantity
                        </label>
                        <input
                          type="number"
                          min="1"
                          step="1"
                          required
                          value={v.totalQuantity}
                          onChange={(e) => handleUpdateVariant(index, "totalQuantity", e.target.value)}
                          placeholder="50"
                          className="w-full rounded-lg border border-neutral-300 py-1.5 px-3 text-xs text-neutral-900 shadow-2xs focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 font-semibold"
                        />
                        {v.soldQuantity !== undefined ? (
                          <p className="mt-1 text-[10px] text-neutral-400">
                            Sold {v.soldQuantity}, reserved {v.reservedQuantity || 0}
                          </p>
                        ) : (
                          <p className="mt-1 text-[10px] text-neutral-400">Allocated deal stock</p>
                        )}
                      </div>

                      {/* Status Toggle in Edit Mode */}
                      {isEditMode && (
                        <div className="w-full sm:w-28">
                          <label className="block text-[11px] font-medium text-neutral-600 mb-1">
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
                            className={`w-full inline-flex items-center justify-center gap-1.5 rounded-lg py-1.5 px-2.5 text-xs font-semibold transition ${
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
                    </div>
                  ) : (
                    <div className="flex-1 flex items-center justify-end py-2">
                      <span className="text-xs text-neutral-400 italic">
                        Not included in deal. Tick checkbox to sell this variant.
                      </span>
                    </div>
                  )}
                </div>
              );
            })}
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
