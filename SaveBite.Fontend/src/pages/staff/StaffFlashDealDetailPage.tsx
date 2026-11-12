import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { FlashDealStockHistory } from "@/features/restock/FlashDealStockHistory";
import { useRestockWorkspace } from "@/features/restock/restockContext";
import { useEffect, useState, useMemo } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  ArrowLeft,
  AlertCircle,
  EyeOff,
  Package,
  Layers,
  CheckCircle2,
  Clock3,
} from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import { FlashDealStatusHistory } from "@/features/flash-deals/components/FlashDealStatusHistory";
import {
  FlashDealStatusEnum,
  FlashDealVariantStatusEnum,
  type ShopFlashDealDetail,
} from "@/features/flash-deals/types/flashDeal.types";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

export function StaffFlashDealDetailPage() {
  const { shopId, id } = useParams<{ shopId: string; id: string }>();
  const navigate = useNavigate();
  const { revision: stockRevision } = useRestockWorkspace();

  const [deal, setDeal] = useState<ShopFlashDealDetail | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    let ignore = false;
    async function loadDetail() {
      try {
        setLoading(true);
        const data = await flashDealApi.getShopFlashDealDetail(id!, { shopId });
        if (!ignore) {
          setDeal(data);
        }
      } catch (error) {
        if (!ignore) toast.error(getApiErrorMessage(error));
      } finally {
        if (!ignore) setLoading(false);
      }
    }
    void loadDetail();
    return () => {
      ignore = true;
    };
  }, [id, shopId, stockRevision]);

  function formatDateTime(dateStr?: string) {
    if (!dateStr) return "-";
    try {
      const d = new Date(dateStr);
      const day = String(d.getDate()).padStart(2, "0");
      const month = String(d.getMonth() + 1).padStart(2, "0");
      const year = d.getFullYear();
      const hours = String(d.getHours()).padStart(2, "0");
      const minutes = String(d.getMinutes()).padStart(2, "0");
      return `${hours}:${minutes} ${day}/${month}/${year}`;
    } catch {
      return dateStr;
    }
  }

  // Summary calculations
  const summary = useMemo(() => {
    if (!deal)
      return { totalQty: 0, soldQty: 0, reservedQty: 0, remainingQty: 0, revenue: 0, percent: 0 };
    const totalQty = deal.variants.reduce((acc, v) => acc + v.totalQuantity, 0);
    const soldQty = deal.variants.reduce((acc, v) => acc + v.soldQuantity, 0);
    const reservedQty = deal.variants.reduce((acc, v) => acc + v.reservedQuantity, 0);
    const remainingQty = deal.variants.reduce((acc, v) => acc + v.availableQuantity, 0);
    const revenue = deal.variants.reduce((acc, v) => acc + v.soldQuantity * v.dealPrice, 0);
    const percent = totalQty > 0 ? Math.round((soldQty / totalQty) * 100) : 0;
    return { totalQty, soldQty, reservedQty, remainingQty, revenue, percent };
  }, [deal]);

  // Safety buffer minutes
  const safetyBuffer = useMemo(() => {
    if (!deal) return 30;
    try {
      const closing = new Date(deal.shopClosingTime).getTime();
      const orderEnd = new Date(deal.orderEndTime).getTime();
      const diffMins = Math.round((closing - orderEnd) / (1000 * 60));
      return diffMins > 0 ? diffMins : 30;
    } catch {
      return 30;
    }
  }, [deal]);

  if (loading) {
    return <LoadingSkeleton variant="detail" label="Loading flash deal" />;
  }

  if (!deal) {
    return (
      <div className="rounded-xl border border-neutral-200 bg-white p-8 text-center shadow-2xs">
        <AlertCircle className="mx-auto size-8 text-neutral-400" />
        <p className="mt-2 text-sm text-neutral-600">Flash Deal details not found.</p>
        <button
          type="button"
          onClick={() => navigate(`${APP_PATHS.STAFF}/shops/${shopId}/flash-deals`)}
          className="mt-4 text-xs font-semibold text-emerald-600 hover:underline"
        >
          Back to Flash Deals
        </button>
      </div>
    );
  }

  const isHidden = deal.status === FlashDealStatusEnum.Hidden;
  const isCancelled =
    deal.status === FlashDealStatusEnum.Cancelled || deal.status === FlashDealStatusEnum.Closed;

  return (
    <div className="space-y-6">
      {/* Back button */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Flash Deal Details</h1>
          <p className="mt-1 text-sm text-neutral-500">
            Monitor product, deal pricing, inventory, and schedule.
          </p>
        </div>
        <button
          type="button"
          onClick={() => navigate(`${APP_PATHS.STAFF}/shops/${shopId}/flash-deals`)}
          className="inline-flex items-center gap-1.5 self-start rounded-lg border border-neutral-200 bg-white px-3.5 py-2 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50"
        >
          <ArrowLeft className="size-3.5" />
          <span>Back to Flash Deals</span>
        </button>
      </div>

      {/* Prominent Warning Banner if Deal is Hidden (View-only) */}
      {isHidden && (
        <div className="flex flex-col gap-3 rounded-xl border border-amber-300 bg-amber-50 p-4 shadow-2xs sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3">
            <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-amber-100 text-amber-700 ring-1 ring-amber-500/20">
              <EyeOff className="size-5" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-amber-900">
                This Flash Deal is currently hidden from customers
              </h3>
              <p className="text-xs text-amber-700">
                Customers cannot discover or place orders for this deal. Only store owners can
                change visibility.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Product Card */}
      <div className="flex flex-col gap-4 rounded-xl border border-neutral-200 bg-white p-5 shadow-2xs sm:flex-row sm:items-center">
        <img
          src={
            deal.primaryImageUrl ||
            "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=160&auto=format&fit=crop&q=60"
          }
          alt={deal.productName}
          className="size-20 rounded-xl object-cover"
        />
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <h2 className="text-base font-bold text-neutral-900">{deal.productName}</h2>
            {deal.categoryName && (
              <span className="rounded bg-neutral-100 px-2 py-0.5 text-[11px] font-medium text-neutral-600">
                {deal.categoryName}
              </span>
            )}
            {isHidden ? (
              <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-[11px] font-bold text-amber-800">
                <EyeOff className="size-3 text-amber-700" />
                Hidden
              </span>
            ) : isCancelled ? (
              <span className="inline-flex items-center rounded-full bg-neutral-100 px-2.5 py-0.5 text-[11px] font-medium text-neutral-600">
                Ended
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2.5 py-0.5 text-[11px] font-bold text-emerald-700">
                <span className="size-1.5 rounded-full bg-emerald-500 animate-pulse" />
                Active
              </span>
            )}
          </div>
          <p className="text-xs text-neutral-400">Enrolled in store Flash Deal campaign</p>
          {deal.description && <p className="text-xs text-neutral-600">{deal.description}</p>}
        </div>
      </div>

      {/* 4 Stats Cards */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 sm:gap-4">
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <Package className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Total Quantity</p>
              <p className="text-xl font-bold text-neutral-900">{summary.totalQty}</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
              <CheckCircle2 className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Sold ({summary.percent}%)</p>
              <p className="text-xl font-bold text-emerald-600">{summary.soldQty}</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
              <Layers className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Remaining</p>
              <p className="text-xl font-bold text-blue-600">{summary.remainingQty}</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-amber-50 text-amber-600">
              <Clock3 className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Estimated Revenue</p>
              <p className="text-base font-bold text-neutral-900 sm:text-lg">
                {summary.revenue.toLocaleString("en-US")} VND
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Program Schedule Card */}
      <div className="rounded-xl border border-neutral-200 bg-white p-5 shadow-2xs space-y-4">
        <h3 className="text-sm font-bold text-neutral-900">Deal Schedule</h3>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div className="rounded-lg bg-neutral-50/75 p-3.5 border border-neutral-100">
            <p className="text-[11px] text-neutral-400">Sale Starts</p>
            <p className="mt-1 text-xs font-semibold text-neutral-800">
              {formatDateTime(deal.saleStartTime)}
            </p>
          </div>

          <div className="rounded-lg bg-neutral-50/75 p-3.5 border border-neutral-100">
            <p className="text-[11px] text-neutral-400">Order Cutoff</p>
            <p className="mt-1 text-xs font-semibold text-neutral-800">
              {formatDateTime(deal.orderEndTime)}
            </p>
          </div>

          <div className="rounded-lg bg-neutral-50/75 p-3.5 border border-neutral-100">
            <p className="text-[11px] text-neutral-400">Store Closing Time</p>
            <p className="mt-1 text-xs font-semibold text-neutral-800">
              {formatDateTime(deal.shopClosingTime)}
            </p>
          </div>

          <div className="rounded-lg bg-neutral-50/75 p-3.5 border border-neutral-100">
            <p className="text-[11px] text-neutral-400">Safety Buffer</p>
            <p className="mt-1 text-xs font-semibold text-neutral-800">{safetyBuffer} mins</p>
          </div>
        </div>

        <div className="flex items-center gap-4 text-[11px] text-neutral-400 pt-1">
          <span>Created at: {formatDateTime(deal.createdAt)}</span>
          <span>•</span>
          <span>Updated at: {formatDateTime(deal.updatedAt)}</span>
        </div>
      </div>

      {/* Variants Table Card */}
      <div className="rounded-xl border border-neutral-200 bg-white shadow-2xs overflow-hidden">
        <div className="border-b border-neutral-200 p-5">
          <h3 className="text-sm font-bold text-neutral-900">Configured Variants</h3>
          <p className="mt-0.5 text-xs text-neutral-500">
            Deal prices and inventory quantities configured per product variant.
          </p>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-neutral-200 text-left text-xs">
            <thead className="bg-neutral-50/75 text-neutral-500">
              <tr>
                <th scope="col" className="px-4 py-3 font-medium">
                  Variant
                </th>
                <th scope="col" className="px-4 py-3 font-medium">
                  Original Price
                </th>
                <th scope="col" className="px-4 py-3 font-medium">
                  Deal Price
                </th>
                <th scope="col" className="px-4 py-3 font-medium">
                  Discount
                </th>
                <th scope="col" className="px-4 py-3 font-medium">
                  Sold
                </th>
                <th
                  scope="col"
                  className="px-4 py-3 font-medium"
                  title="Includes stock held while awaiting a restock decision"
                >
                  Reserved
                </th>
                <th scope="col" className="px-4 py-3 font-medium">
                  Remaining
                </th>
                <th
                  scope="col"
                  className="px-4 py-3 font-medium"
                  title="Quantities marked unsellable or automatically stopped after the restock deadline"
                >
                  Stopped from sale
                </th>
                <th scope="col" className="px-4 py-3 font-medium">
                  Status
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-200 bg-white">
              {deal.variants.map((v) => {
                const soldPercent =
                  v.totalQuantity > 0
                    ? Math.min(100, Math.round((v.soldQuantity / v.totalQuantity) * 100))
                    : 0;

                return (
                  <tr key={v.id} className="hover:bg-neutral-50/60">
                    <td className="px-4 py-3.5">
                      <p className="font-semibold text-neutral-900">
                        {v.variantName || v.sku || "Default"}
                      </p>
                      {v.sku && <p className="text-[10px] text-neutral-400">{v.sku}</p>}
                    </td>
                    <td className="px-4 py-3.5 text-neutral-500">
                      {v.originalPrice.toLocaleString("en-US")} VND
                    </td>
                    <td className="px-4 py-3.5 font-bold text-red-600">
                      {v.dealPrice.toLocaleString("en-US")} VND
                    </td>
                    <td className="px-4 py-3.5 font-medium text-emerald-600">
                      {v.discountPercent}%
                    </td>
                    <td className="px-4 py-3.5 whitespace-nowrap">
                      <div className="w-28 space-y-1">
                        <span className="font-semibold text-neutral-800">
                          {v.soldQuantity} / {v.totalQuantity}
                        </span>
                        <div className="h-1 w-full overflow-hidden rounded-full bg-neutral-100">
                          <div
                            className="h-full rounded-full bg-emerald-500"
                            style={{ width: `${soldPercent}%` }}
                          />
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3.5 text-neutral-700">{v.reservedQuantity}</td>
                    <td className="px-4 py-3.5 font-semibold text-neutral-900">
                      {v.availableQuantity}
                    </td>
                    <td className="px-4 py-3.5 font-semibold text-orange-700">
                      {Number.isInteger(v.unsellableQuantity) ? v.unsellableQuantity : "—"}
                    </td>
                    <td className="px-4 py-3.5">
                      {v.status === FlashDealVariantStatusEnum.Hide || isHidden ? (
                        <span className="inline-flex items-center rounded-full bg-amber-50 px-2 py-0.5 text-[10px] font-medium text-amber-700">
                          Hidden
                        </span>
                      ) : (
                        <span className="inline-flex items-center rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-medium text-emerald-700">
                          Active
                        </span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
      <FlashDealStatusHistory history={deal.statusHistory ?? []} />
      <FlashDealStockHistory flashDealId={deal.id} />
    </div>
  );
}

export default StaffFlashDealDetailPage;
