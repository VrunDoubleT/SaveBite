import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Eye,
  Edit2,
  EyeOff,
  Plus,
  Search,
  XCircle,
  Sparkles,
  Loader2,
} from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import {
  FlashDealStatusEnum,
  type ShopFlashDealListItem,
} from "@/features/flash-deals/types/flashDeal.types";
import { CancelFlashDealModal } from "@/features/flash-deals/components/CancelFlashDealModal";
import { ToggleVisibilityModal } from "@/features/flash-deals/components/ToggleVisibilityModal";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

type FilterTab = "all" | "pending" | "running" | "ended" | "hidden";

export function StoreOwnerFlashDealsPage() {
  const navigate = useNavigate();
  const [deals, setDeals] = useState<ShopFlashDealListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<FilterTab>("all");
  const [searchKeyword, setSearchKeyword] = useState("");

  // Modal state for cancelling deal
  const [cancellingDeal, setCancellingDeal] = useState<ShopFlashDealListItem | null>(null);
  const [cancelModalLoading, setCancelModalLoading] = useState(false);

  // Modal state for toggling visibility (Hide / Unhide)
  const [togglingDeal, setTogglingDeal] = useState<ShopFlashDealListItem | null>(null);
  const [toggleModalLoading, setToggleModalLoading] = useState(false);

  // Load shop flash deals
  async function loadDeals(showLoader = false) {
    try {
      if (showLoader) setLoading(true);
      const res = await flashDealApi.getShopFlashDeals({ page: 1, pageSize: 50 });
      setDeals(res.items);
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let ignore = false;
    async function init() {
      try {
        const res = await flashDealApi.getShopFlashDeals({ page: 1, pageSize: 50 });
        if (!ignore) {
          setDeals(res.items);
        }
      } catch (error) {
        if (!ignore) toast.error(getApiErrorMessage(error));
      } finally {
        if (!ignore) setLoading(false);
      }
    }
    void init();
    return () => {
      ignore = true;
    };
  }, []);

  // Compute status helpers
  function getDealState(deal: ShopFlashDealListItem): "pending" | "running" | "ended" | "hidden" {
    if (deal.status === FlashDealStatusEnum.Hidden) return "hidden";
    if (deal.status === FlashDealStatusEnum.Cancelled || deal.status === FlashDealStatusEnum.Closed) return "ended";
    const now = new Date();
    const start = new Date(deal.saleStartTime);
    const end = new Date(deal.orderEndTime);

    if (now < start) return "pending";
    if (now >= start && now <= end) return "running";
    return "ended";
  }

  // Stats
  const stats = useMemo(() => {
    let running = 0;
    let pending = 0;
    let sold = 0;
    let total = 0;
    let remaining = 0;
    let ended = 0;
    let hidden = 0;

    deals.forEach((d) => {
      const state = getDealState(d);
      if (state === "running") running++;
      if (state === "pending") pending++;
      if (state === "ended") ended++;
      if (state === "hidden") hidden++;

      sold += d.soldQuantity;
      total += d.totalQuantity;
      remaining += d.availableQuantity;
    });

    return {
      running,
      pending,
      sold,
      total,
      remaining,
      totalDeals: deals.length,
      ended,
      hidden,
    };
  }, [deals]);

  // Tab counts
  const tabCounts = useMemo(() => {
    return {
      all: deals.length,
      pending: stats.pending,
      running: stats.running,
      ended: stats.ended,
      hidden: stats.hidden,
    };
  }, [deals, stats]);

  // Filtered deals
  const filteredDeals = useMemo(() => {
    return deals.filter((deal) => {
      const state = getDealState(deal);
      if (activeTab !== "all" && state !== activeTab) return false;

      if (searchKeyword.trim()) {
        const kw = searchKeyword.toLowerCase();
        const matchName = deal.productName.toLowerCase().includes(kw);
        const matchCat = deal.categoryName?.toLowerCase().includes(kw);
        if (!matchName && !matchCat) return false;
      }

      return true;
    });
  }, [deals, activeTab, searchKeyword]);

  // Confirm toggle visibility via modal
  async function handleConfirmToggleVisibility() {
    if (!togglingDeal) return;
    try {
      setToggleModalLoading(true);
      if (togglingDeal.status === FlashDealStatusEnum.Hidden) {
        await flashDealApi.unhideFlashDeal(togglingDeal.id);
        toast.success(`"${togglingDeal.productName}" is now visible to customers.`);
      } else {
        await flashDealApi.hideFlashDeal(togglingDeal.id);
        toast.success(`"${togglingDeal.productName}" has been temporarily hidden.`);
      }
      setTogglingDeal(null);
      await loadDeals();
    } catch (err) {
      toast.error(getApiErrorMessage(err));
    } finally {
      setToggleModalLoading(false);
    }
  }

  // Confirm cancel via modal
  async function handleConfirmCancel() {
    if (!cancellingDeal) return;
    try {
      setCancelModalLoading(true);
      await flashDealApi.cancelFlashDeal(cancellingDeal.id);
      toast.success("Flash Deal cancelled successfully.");
      setCancellingDeal(null);
      await loadDeals();
    } catch (err) {
      toast.error(getApiErrorMessage(err));
    } finally {
      setCancelModalLoading(false);
    }
  }

  function formatDateTime(dateStr: string) {
    try {
      const d = new Date(dateStr);
      const day = String(d.getDate()).padStart(2, "0");
      const month = String(d.getMonth() + 1).padStart(2, "0");
      const year = d.getFullYear();
      const hours = String(d.getHours()).padStart(2, "0");
      const minutes = String(d.getMinutes()).padStart(2, "0");
      return `${day}/${month}/${year} ${hours}:${minutes}`;
    } catch {
      return dateStr;
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Flash Deals</h1>
          <p className="mt-1 text-sm text-neutral-500">
            View, monitor, and manage your store's surplus food discount deals.
          </p>
        </div>
        <button
          type="button"
          onClick={() => navigate(APP_PATHS.STORE_OWNER_FLASH_DEAL_CREATE)}
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-emerald-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2"
        >
          <Plus className="size-4" />
          <span>Create Flash Deal</span>
        </button>
      </div>

      {/* 4 Stats Cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {/* Card 1: Running */}
        <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs">
          <p className="text-sm font-medium text-neutral-500">Running Deals</p>
          <p className="mt-2 text-3xl font-bold text-neutral-900">{stats.running}</p>
          <p className="mt-1 text-xs text-neutral-400">
            {stats.pending} upcoming program{stats.pending !== 1 ? "s" : ""}
          </p>
        </div>

        {/* Card 2: Sold */}
        <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs">
          <p className="text-sm font-medium text-neutral-500">Total Sold</p>
          <p className="mt-2 text-3xl font-bold text-neutral-900">{stats.sold}</p>
          <p className="mt-1 text-xs text-neutral-400">
            out of {stats.total} total items
          </p>
        </div>

        {/* Card 3: Remaining */}
        <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs">
          <p className="text-sm font-medium text-neutral-500">Remaining Inventory</p>
          <p className="mt-2 text-3xl font-bold text-emerald-600">{stats.remaining}</p>
          <p className="mt-1 text-xs text-neutral-400">Available for customer orders</p>
        </div>

        {/* Card 4: Total Programs */}
        <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-xs">
          <p className="text-sm font-medium text-neutral-500">Total Deals</p>
          <p className="mt-2 text-3xl font-bold text-neutral-900">{stats.totalDeals}</p>
          <p className="mt-1 text-xs text-neutral-400">
            {stats.hidden > 0 ? (
              <span className="font-semibold text-amber-700">{stats.hidden} hidden from buyers</span>
            ) : (
              <span>{stats.ended} ended or closed</span>
            )}
          </p>
        </div>
      </div>

      {/* Hidden Deals Notice Banner */}
      {stats.hidden > 0 && activeTab !== "hidden" && (
        <div className="flex flex-col gap-2 rounded-xl border border-amber-200 bg-amber-50/80 px-4 py-3 text-xs sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-2.5 text-amber-900">
            <div className="flex size-7 shrink-0 items-center justify-center rounded-lg bg-amber-100 text-amber-700">
              <EyeOff className="size-4" />
            </div>
            <div>
              <span className="font-bold">
                You have {stats.hidden} hidden Flash Deal{stats.hidden !== 1 ? "s" : ""}
              </span>
              <span className="text-amber-700"> — Hidden deals are not visible to customers on SaveBite.</span>
            </div>
          </div>
          <button
            type="button"
            onClick={() => setActiveTab("hidden")}
            className="self-start sm:self-auto rounded-lg bg-amber-200/80 px-3 py-1 font-bold text-amber-900 transition hover:bg-amber-300"
          >
            View Hidden Deals ({stats.hidden})
          </button>
        </div>
      )}

      {/* Tabs & Search */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            onClick={() => setActiveTab("all")}
            className={`rounded-full px-3.5 py-1.5 text-xs font-semibold transition ${
              activeTab === "all"
                ? "bg-emerald-600 text-white shadow-xs"
                : "bg-neutral-100 text-neutral-600 hover:bg-neutral-200"
            }`}
          >
            All ({tabCounts.all})
          </button>
          <button
            type="button"
            onClick={() => setActiveTab("pending")}
            className={`rounded-full px-3.5 py-1.5 text-xs font-semibold transition ${
              activeTab === "pending"
                ? "bg-emerald-600 text-white shadow-xs"
                : "bg-neutral-100 text-neutral-600 hover:bg-neutral-200"
            }`}
          >
            Upcoming ({tabCounts.pending})
          </button>
          <button
            type="button"
            onClick={() => setActiveTab("running")}
            className={`rounded-full px-3.5 py-1.5 text-xs font-semibold transition ${
              activeTab === "running"
                ? "bg-emerald-600 text-white shadow-xs"
                : "bg-neutral-100 text-neutral-600 hover:bg-neutral-200"
            }`}
          >
            Running ({tabCounts.running})
          </button>
          <button
            type="button"
            onClick={() => setActiveTab("ended")}
            className={`rounded-full px-3.5 py-1.5 text-xs font-semibold transition ${
              activeTab === "ended"
                ? "bg-emerald-600 text-white shadow-xs"
                : "bg-neutral-100 text-neutral-600 hover:bg-neutral-200"
            }`}
          >
            Ended ({tabCounts.ended})
          </button>
          <button
            type="button"
            onClick={() => setActiveTab("hidden")}
            className={`inline-flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-xs font-semibold transition ${
              activeTab === "hidden"
                ? "bg-amber-600 text-white shadow-xs"
                : tabCounts.hidden > 0
                ? "bg-amber-100/70 text-amber-800 hover:bg-amber-200/70"
                : "bg-neutral-100 text-neutral-600 hover:bg-neutral-200"
            }`}
          >
            <EyeOff className="size-3.5" />
            <span>Hidden ({tabCounts.hidden})</span>
          </button>
        </div>

        {/* Search input */}
        <div className="relative w-full sm:w-72">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-neutral-400" />
          <input
            type="text"
            value={searchKeyword}
            onChange={(e) => setSearchKeyword(e.target.value)}
            placeholder="Search deals or products..."
            className="w-full rounded-lg border border-neutral-200 bg-white py-2 pl-9 pr-3 text-xs text-neutral-900 placeholder:text-neutral-400 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
          />
        </div>
      </div>

      {/* Deals Table */}
      <div className="overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-xs">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-neutral-200 text-left text-xs">
            <thead className="bg-neutral-50/75 text-neutral-500">
              <tr>
                <th scope="col" className="px-4 py-3.5 font-medium">Product</th>
                <th scope="col" className="px-4 py-3.5 font-medium">Flash Deal Price</th>
                <th scope="col" className="px-4 py-3.5 font-medium">Schedule</th>
                <th scope="col" className="px-4 py-3.5 font-medium">Sales Progress</th>
                <th scope="col" className="px-4 py-3.5 font-medium">Status & Visibility</th>
                <th scope="col" className="px-4 py-3.5 text-right font-medium">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-200 bg-white">
              {loading ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-neutral-400">
                    <Loader2 className="mx-auto size-6 animate-spin text-emerald-600" />
                    <p className="mt-2 text-xs">Loading flash deals...</p>
                  </td>
                </tr>
              ) : filteredDeals.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-neutral-400">
                    <Sparkles className="mx-auto size-8 text-neutral-300" />
                    <p className="mt-2 text-sm font-medium text-neutral-600">No flash deals found</p>
                    <p className="mt-1 text-xs text-neutral-400">
                      Create a new Flash Deal to offer surplus food at discounted prices!
                    </p>
                  </td>
                </tr>
              ) : (
                filteredDeals.map((deal) => {
                  const state = getDealState(deal);
                  const isHidden = deal.status === FlashDealStatusEnum.Hidden;
                  const soldPercent = deal.totalQuantity > 0
                    ? Math.min(100, Math.round((deal.soldQuantity / deal.totalQuantity) * 100))
                    : 0;

                  return (
                    <tr
                      key={deal.id}
                      className={
                        isHidden
                          ? "border-l-4 border-l-amber-500 bg-amber-50/35 transition-colors hover:bg-amber-50/60"
                          : "border-l-4 border-l-transparent transition-colors hover:bg-neutral-50/60"
                      }
                    >
                      {/* Product */}
                      <td className="px-4 py-4">
                        <div className="flex items-center gap-3">
                          <img
                            src={deal.productImageUrl || "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=120&auto=format&fit=crop&q=60"}
                            alt={deal.productName}
                            className="size-11 rounded-lg object-cover"
                          />
                          <div>
                            <div className="flex items-center gap-1.5">
                              <p className="font-semibold text-neutral-900">{deal.productName}</p>
                              {isHidden && (
                                <span className="rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-bold text-amber-800">
                                  HIDDEN
                                </span>
                              )}
                            </div>
                            <p className="text-[11px] text-neutral-400">
                              {deal.categoryName || "Food"}
                            </p>
                          </div>
                        </div>
                      </td>

                      {/* Price */}
                      <td className="px-4 py-4 whitespace-nowrap">
                        <p className="font-bold text-red-600">
                          {deal.minDealPrice.toLocaleString("vi-VN")}đ
                        </p>
                        <p className="text-[11px] text-neutral-400">
                          up to {deal.maxDiscountPercent}% off
                        </p>
                      </td>

                      {/* Time */}
                      <td className="px-4 py-4 whitespace-nowrap text-neutral-600">
                        <p>Starts: {formatDateTime(deal.saleStartTime)}</p>
                        <p className="text-neutral-400">Ends: {formatDateTime(deal.orderEndTime)}</p>
                      </td>

                      {/* Progress */}
                      <td className="px-4 py-4 whitespace-nowrap">
                        <div className="w-36 space-y-1">
                          <div className="flex items-center justify-between text-[11px]">
                            <span className="font-semibold text-neutral-800">
                              {deal.soldQuantity} / {deal.totalQuantity}
                            </span>
                            <span className="text-neutral-400">{soldPercent}%</span>
                          </div>
                          <div className="h-1.5 w-full overflow-hidden rounded-full bg-neutral-100">
                            <div
                              className="h-full rounded-full bg-emerald-500"
                              style={{ width: `${soldPercent}%` }}
                            />
                          </div>
                          <p className="text-[10px] text-neutral-400">
                            Remaining {deal.availableQuantity} • Reserved {deal.reservedQuantity}
                          </p>
                        </div>
                      </td>

                      {/* Status & Visibility */}
                      <td className="px-4 py-4 whitespace-nowrap">
                        {isHidden ? (
                          <div className="space-y-0.5">
                            <span className="inline-flex items-center gap-1.5 rounded-full bg-amber-100 px-2.5 py-0.5 text-[11px] font-bold text-amber-800 ring-1 ring-inset ring-amber-600/30">
                              <EyeOff className="size-3 text-amber-700" />
                              Hidden
                            </span>
                            <p className="text-[10px] font-medium text-amber-700">Not visible to buyers</p>
                          </div>
                        ) : state === "running" ? (
                          <div className="space-y-0.5">
                            <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-2.5 py-0.5 text-[11px] font-bold text-emerald-700 ring-1 ring-inset ring-emerald-600/20">
                              <span className="size-1.5 rounded-full bg-emerald-500 animate-pulse" />
                              Active
                            </span>
                            <p className="text-[10px] font-medium text-emerald-600">Visible to customers</p>
                          </div>
                        ) : state === "pending" ? (
                          <div className="space-y-0.5">
                            <span className="inline-flex items-center gap-1.5 rounded-full bg-blue-50 px-2.5 py-0.5 text-[11px] font-medium text-blue-700 ring-1 ring-inset ring-blue-600/20">
                              Upcoming
                            </span>
                            <p className="text-[10px] text-neutral-400">Scheduled to start</p>
                          </div>
                        ) : (
                          <div className="space-y-0.5">
                            <span className="inline-flex items-center gap-1.5 rounded-full bg-neutral-100 px-2.5 py-0.5 text-[11px] font-medium text-neutral-600 ring-1 ring-inset ring-neutral-500/20">
                              Ended
                            </span>
                            <p className="text-[10px] text-neutral-400">Deal closed</p>
                          </div>
                        )}
                      </td>

                      {/* Actions */}
                      <td className="px-4 py-4 whitespace-nowrap text-right">
                        <div className="inline-flex items-center gap-2">
                          {/* Prominent High-Visibility Hide / Unhide Button */}
                          {state !== "ended" && (
                            isHidden ? (
                              <button
                                type="button"
                                onClick={() => setTogglingDeal(deal)}
                                className="inline-flex items-center gap-1.5 rounded-lg border border-amber-400 bg-amber-500 px-3 py-1.5 text-xs font-bold text-white shadow-2xs transition hover:bg-amber-600 active:scale-95"
                                title="Click to unhide this deal for customers"
                              >
                                <Eye className="size-3.5" />
                                <span>Unhide Deal</span>
                              </button>
                            ) : (
                              <button
                                type="button"
                                onClick={() => setTogglingDeal(deal)}
                                className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-300 bg-white px-2.5 py-1.5 text-xs font-medium text-neutral-700 shadow-2xs transition hover:border-amber-300 hover:bg-amber-50 hover:text-amber-800"
                                title="Click to temporarily hide this deal from customers"
                              >
                                <EyeOff className="size-3.5 text-neutral-500" />
                                <span>Hide</span>
                              </button>
                            )
                          )}

                          {/* View details */}
                          <button
                            type="button"
                            title="View deal details"
                            onClick={() =>
                              navigate(
                                APP_PATHS.STORE_OWNER_FLASH_DEAL_DETAIL.replace(":id", deal.id)
                              )
                            }
                            className="rounded-lg border border-neutral-200 bg-white p-1.5 text-neutral-500 shadow-2xs transition hover:border-neutral-300 hover:bg-neutral-50 hover:text-neutral-900"
                          >
                            <Eye className="size-3.5" />
                          </button>

                          {/* Edit */}
                          {state !== "ended" && (
                            <button
                              type="button"
                              title="Edit deal"
                              onClick={() =>
                                navigate(
                                  APP_PATHS.STORE_OWNER_FLASH_DEAL_EDIT.replace(":id", deal.id)
                                )
                              }
                              className="rounded-lg border border-neutral-200 bg-white p-1.5 text-neutral-500 shadow-2xs transition hover:border-neutral-300 hover:bg-neutral-50 hover:text-neutral-900"
                            >
                              <Edit2 className="size-3.5" />
                            </button>
                          )}

                          {/* Cancel - Opens Confirmation Modal */}
                          {state !== "ended" && (
                            <button
                              type="button"
                              title="Cancel deal"
                              onClick={() => setCancellingDeal(deal)}
                              className="rounded-lg border border-red-200 bg-red-50/50 p-1.5 text-red-600 shadow-2xs transition hover:border-red-300 hover:bg-red-100"
                            >
                              <XCircle className="size-3.5" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between border-t border-neutral-200 px-4 py-3 text-xs text-neutral-500">
          <span>
            Showing {filteredDeals.length} of {deals.length} Flash Deal{deals.length !== 1 ? "s" : ""}
          </span>
        </div>
      </div>

      {/* Pop-up Modal for Hide / Unhide Deal */}
      <ToggleVisibilityModal
        deal={togglingDeal}
        loading={toggleModalLoading}
        onClose={() => setTogglingDeal(null)}
        onConfirm={() => void handleConfirmToggleVisibility()}
      />

      {/* Pop-up Modal for Cancel Flash Deal */}
      <CancelFlashDealModal
        deal={cancellingDeal}
        loading={cancelModalLoading}
        onClose={() => setCancellingDeal(null)}
        onConfirm={() => void handleConfirmCancel()}
      />
    </div>
  );
}
