import { useEffect, useMemo, useState } from "react";
import { Banknote, Receipt, Search, Store, Undo2 } from "lucide-react";
import { EmptyState, KpiGrid, Panel, ProgressBar } from "@/shared/components/analytics";
import { Pagination, usePagination } from "@/shared/components/pagination";
import { MediaImage } from "@/shared/components/ui";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { formatNumber, formatPercent, formatVnd } from "@/features/store-analytics/utils/formatAnalytics";
import type { AnalyticsPeriodQuery } from "@/features/store-analytics/types/storeAnalytics.types";
import { platformAnalyticsApi } from "../api/platformAnalyticsApi";
import type { PlatformShopRevenue, ShopRevenueItem } from "../types/platformAnalytics.types";

type SortKey = "netRevenue" | "completedOrders" | "paidRevenue" | "platformFeeAmount" | "shopName";

const SORT_OPTIONS: { id: SortKey; label: string }[] = [
  { id: "netRevenue", label: "Net revenue (high to low)" },
  { id: "paidRevenue", label: "Collected revenue (high to low)" },
  { id: "completedOrders", label: "Completed orders (high to low)" },
  { id: "platformFeeAmount", label: "Platform fee (high to low)" },
  { id: "shopName", label: "Shop name (A to Z)" },
];

const PAGE_SIZE = 10;

interface PlatformShopRevenueSectionProps {
  /** Same period query as the rest of the dashboard. */
  query: AnalyticsPeriodQuery;
  /** True when the dashboard is showing the "All time" view. */
  allTime: boolean;
  rangeError?: string | null;
  scopeLabel: string;
}

// FIX-45: revenue of every shop for the selected period, so an admin can see who earns what.
export function PlatformShopRevenueSection({
  query,
  allTime,
  rangeError,
  scopeLabel,
}: PlatformShopRevenueSectionProps) {
  const [data, setData] = useState<PlatformShopRevenue | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [keyword, setKeyword] = useState("");
  const [sortKey, setSortKey] = useState<SortKey>("netRevenue");

  const requestKey = JSON.stringify({ query, allTime });

  useEffect(() => {
    if (rangeError) return;
    let ignore = false;
    async function load() {
      try {
        setLoading(true);
        const parsed = JSON.parse(requestKey) as { query: AnalyticsPeriodQuery; allTime: boolean };
        const result = await platformAnalyticsApi.getShopRevenue(parsed.query, parsed.allTime);
        if (!ignore) {
          setData(result);
          setError(null);
        }
      } catch (err) {
        if (!ignore) setError(getApiErrorMessage(err) || "Unable to load revenue by shop.");
      } finally {
        if (!ignore) setLoading(false);
      }
    }
    void load();
    return () => {
      ignore = true;
    };
  }, [requestKey, rangeError]);

  const rows = useMemo(() => {
    const q = keyword.trim().toLowerCase();
    const filtered = (data?.shops ?? []).filter((shop) => !q || shop.shopName.toLowerCase().includes(q));
    return [...filtered].sort((a: ShopRevenueItem, b: ShopRevenueItem) =>
      sortKey === "shopName" ? a.shopName.localeCompare(b.shopName) : b[sortKey] - a[sortKey],
    );
  }, [data, keyword, sortKey]);

  const { currentPage, setCurrentPage, totalPages, paginatedItems, totalItems } = usePagination({
    items: rows,
    initialPageSize: PAGE_SIZE,
  });

  if (loading && !data) {
    return <div className="h-64 animate-pulse rounded-lg bg-neutral-100" role="status" aria-label="Loading revenue by shop" />;
  }

  if (error && !data) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">
        {error}
      </div>
    );
  }

  if (!data) return null;

  return (
    <div className={`space-y-6 transition-opacity ${loading ? "opacity-60" : ""}`} aria-busy={loading}>
      <Panel
        icon={Store}
        title={`Revenue by shop, ${scopeLabel}`}
        description="Money collected for completed orders minus refunds sent back, split per shop. The shops below add up to the net order revenue on the Revenue and orders tab."
      >
        <KpiGrid
          items={[
            {
              label: "Net revenue (all shops)",
              value: formatVnd(data.netRevenue),
              hint: `${formatVnd(data.paidRevenue)} collected, ${formatVnd(data.refundedAmount)} refunded`,
              icon: Banknote,
              tone: "primary",
            },
            {
              label: "Shops with revenue",
              value: formatNumber(data.shopsWithRevenue),
              hint: "Shops with at least one completed order",
              icon: Store,
            },
            {
              label: "Completed orders",
              value: formatNumber(data.completedOrders),
              icon: Receipt,
            },
            {
              label: "Refunds sent",
              value: formatVnd(data.refundedAmount),
              icon: Undo2,
            },
          ]}
        />

        <div className="flex flex-wrap items-center gap-3">
          <label className="relative min-w-56 flex-1 sm:max-w-xs">
            <span className="sr-only">Search shops</span>
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-neutral-400" />
            <input
              type="search"
              value={keyword}
              onChange={(event) => setKeyword(event.target.value)}
              placeholder="Search shop name"
              className="h-10 w-full rounded-md border border-neutral-200 bg-white pl-9 pr-3 text-sm text-neutral-900 outline-none focus:border-primary-500 focus:shadow-focus"
            />
          </label>
          <label className="flex items-center gap-2 text-sm text-neutral-600">
            Sort by
            <select
              value={sortKey}
              onChange={(event) => setSortKey(event.target.value as SortKey)}
              className="h-10 rounded-md border border-neutral-200 bg-white px-2 text-sm text-neutral-900 outline-none focus:border-primary-500"
            >
              {SORT_OPTIONS.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
        </div>

        {rows.length === 0 ? (
          <EmptyState
            icon={Store}
            title={keyword.trim() ? "No shops match your search" : "No shop revenue in this time"}
            description={
              keyword.trim()
                ? "Try a different shop name."
                : "A shop appears here once it has a completed order in the selected time."
            }
          />
        ) : (
          <>
            <div className="overflow-x-auto rounded-md border border-neutral-200">
              <table className="w-full min-w-[860px] text-left text-sm">
                <thead className="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-neutral-500">
                  <tr>
                    <th className="px-4 py-3">Shop</th>
                    <th className="px-4 py-3 text-right">Orders</th>
                    <th className="px-4 py-3 text-right">Collected</th>
                    <th className="px-4 py-3 text-right">Refunded</th>
                    <th className="px-4 py-3 text-right">Net revenue</th>
                    <th className="px-4 py-3">Share</th>
                    <th className="px-4 py-3 text-right">Platform fee</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-neutral-100">
                  {paginatedItems.map((shop) => (
                    <tr key={shop.shopId} className="transition hover:bg-neutral-50/70">
                      <td className="px-4 py-3">
                        <div className="flex min-w-0 items-center gap-3">
                          <MediaImage
                            src={shop.logoUrl || undefined}
                            alt={shop.shopName}
                            fallbackIcon={Store}
                            showFallbackLabel={false}
                            className="size-9 shrink-0 rounded-md"
                          />
                          <span className="min-w-0 truncate font-medium text-neutral-900" title={shop.shopName}>
                            {shop.shopName}
                          </span>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-right tabular-nums">{formatNumber(shop.completedOrders)}</td>
                      <td className="px-4 py-3 text-right tabular-nums">{formatVnd(shop.paidRevenue)}</td>
                      <td className="px-4 py-3 text-right tabular-nums text-neutral-500">
                        {formatVnd(shop.refundedAmount)}
                      </td>
                      <td className="px-4 py-3 text-right font-semibold tabular-nums text-neutral-900">
                        {formatVnd(shop.netRevenue)}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex min-w-32 items-center gap-2">
                          <div className="w-20 flex-1">
                            <ProgressBar percent={shop.sharePercent} label={`${shop.shopName} share of revenue`} />
                          </div>
                          <span className="w-12 text-right text-xs tabular-nums text-neutral-500">
                            {formatPercent(shop.sharePercent)}
                          </span>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-right tabular-nums">{formatVnd(shop.platformFeeAmount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {totalPages > 1 && (
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                totalItems={totalItems}
                pageSize={PAGE_SIZE}
                itemLabel="shops"
                onPageChange={setCurrentPage}
              />
            )}
          </>
        )}
      </Panel>
    </div>
  );
}
