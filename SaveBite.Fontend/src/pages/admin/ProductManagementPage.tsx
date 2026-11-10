import { useCallback, useEffect, useState, type ReactNode } from "react";
import {
  AlertCircle,
  PackageSearch,
  RotateCw,
  SearchX,
  Package,
  CheckCircle2,
  EyeOff,
  Clock,
  ShieldAlert,
} from "lucide-react";
import { productApi } from "@/features/products/api/productApi";
import { ProductManagementTable } from "@/features/products/components/ProductManagementTable";
import type { ProductListManagement } from "@/features/products/types/product.types";
import { Pagination } from "@/shared/components/pagination";
import { SearchInput } from "@/shared/components/modal/SearchInput";
import { useDebounce } from "@/shared/hooks/useDebounce";

const PAGE_SIZE = 5;
const SEARCH_DEBOUNCE_MS = 400;

function ListSkeleton() {
  return (
    <div
      role="status"
      aria-busy="true"
      aria-label="Loading products"
      className="overflow-hidden rounded-3xl border border-border-default bg-white motion-safe:animate-pulse"
    >
      <span className="sr-only">Loading products…</span>
      <div className="h-11 bg-neutral-50" />
      <div className="divide-y divide-border-default">
        {Array.from({ length: PAGE_SIZE }, (_, i) => (
          <div key={i} className="flex items-center gap-4 px-6 py-4">
            <div className="size-14 shrink-0 rounded-2xl bg-neutral-100" />
            <div className="min-w-0 flex-1 space-y-2">
              <div className="h-4 w-2/5 rounded bg-neutral-100" />
              <div className="h-3 w-1/4 rounded bg-neutral-100" />
            </div>
            <div className="hidden h-6 w-20 rounded-full bg-neutral-100 sm:block" />
            <div className="hidden h-6 w-24 rounded-full bg-neutral-100 sm:block" />
          </div>
        ))}
      </div>
    </div>
  );
}

function StateMessage({
  icon: Icon,
  title,
  description,
  tone = "neutral",
  role,
  actions,
}: {
  icon: typeof AlertCircle;
  title: string;
  description?: string;
  tone?: "neutral" | "error";
  role?: "alert" | "status";
  actions?: ReactNode;
}) {
  const isError = tone === "error";

  return (
    <div
      role={role}
      className={`flex flex-col items-center gap-3 rounded-3xl border bg-white px-6 py-20 text-center ${
        isError ? "border-red-200" : "border-border-default"
      }`}
    >
      <span
        className={`flex size-14 items-center justify-center rounded-2xl ${
          isError ? "bg-red-50 text-red-600" : "bg-neutral-100 text-text-muted"
        }`}
      >
        <Icon className="size-7" aria-hidden="true" />
      </span>
      <p
        className={`text-lg font-semibold tracking-tight ${
          isError ? "text-red-700" : "text-text-primary"
        }`}
      >
        {title}
      </p>
      {description && <p className="max-w-sm text-sm text-text-secondary">{description}</p>}
      {actions && <div className="mt-2">{actions}</div>}
    </div>
  );
}

export function ProductManagementPage() {
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const debouncedSearch = useDebounce(searchInput, SEARCH_DEBOUNCE_MS);

  const [items, setItems] = useState<ProductListManagement[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  // Summary counts state
  const [counts, setCounts] = useState<{
    total: number;
    active: number;
    hidden: number;
    pendingReview: number;
    suspended: number;
  }>({
    total: 0,
    active: 0,
    hidden: 0,
    pendingReview: 0,
    suspended: 0,
  });
  const [loadingStats, setLoadingStats] = useState(true);

  const retry = useCallback(() => {
    setLoading(true);
    setError(null);
    setLoadingStats(true);
    setReloadKey((key) => key + 1);
  }, []);

  // Stats effect
  useEffect(() => {
    let cancelled = false;

    productApi
      .getProductsForManagement({ page: 1, pageSize: 1000 })
      .then((data) => {
        if (!cancelled && data) {
          const all = data.items ?? [];
          const total = data.totalItems ?? all.length;
          const active = all.filter((p) => p.status === 0).length;
          const hidden = all.filter((p) => p.status === 1).length;
          const pendingReview = all.filter((p) => p.latestReview?.status === 1).length;
          const suspended = all.filter((p) => p.latestReview?.status === 4).length;
          setCounts({ total, active, hidden, pendingReview, suspended });
          setLoadingStats(false);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setLoadingStats(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  // Main data fetch effect: tự động fetch khi debouncedSearch thay đổi
  useEffect(() => {
    const controller = new AbortController();
    let cancelled = false;

    productApi
      .getProductsForManagement(
        {
          keyword: debouncedSearch.trim() || undefined,
          page,
          pageSize: PAGE_SIZE,
        },
        controller.signal,
      )
      .then((data) => {
        if (cancelled || controller.signal.aborted) return;
        setItems(data?.items ?? []);
        setTotalItems(data?.totalItems ?? 0);
        setTotalPages(Math.max(1, data?.totalPages ?? 1));
        setLoading(false);
      })
      .catch(() => {
        if (!cancelled && !controller.signal.aborted) {
          setError("Unable to load products.");
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [page, debouncedSearch, reloadKey]);

  const isInitialLoading = loading && items.length === 0;

  return (
    <div className="space-y-6">
      <header className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight text-neutral-900">
              Product Management
            </h1>
          </div>
          <p className="text-sm text-text-secondary">
            Review products registered on the platform and moderate their content.
          </p>
        </div>

        <SearchInput
          value={searchInput}
          onChange={(val) => {
            setSearchInput(val);
            if (val.trim() !== debouncedSearch.trim()) {
              setLoading(true);
            }
            setPage(1);
          }}
          placeholder="Search products or shops..."
          className="w-full sm:w-80"
        />
      </header>

      {/* 5 Overview Stat Cards with icons (no hover, non-clickable) */}
      <div
        className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5 sm:gap-4 [&>*:last-child:nth-child(odd)]:col-span-2 sm:[&>*:last-child:nth-child(odd)]:col-span-1"
        role="region"
        aria-label="Product summary metrics"
      >
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <Package className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">All Products</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.total}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
              <CheckCircle2 className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Active</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.active}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-500">
              <EyeOff className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Hidden</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.hidden}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-amber-50 text-amber-600">
              <Clock className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Pending Review</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.pendingReview}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-rose-50 text-rose-600">
              <ShieldAlert className="size-5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Suspended</p>
              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.suspended}
              </p>
            </div>
          </div>
        </div>
      </div>

      {isInitialLoading ? (
        <ListSkeleton />
      ) : error ? (
        <StateMessage
          role="alert"
          tone="error"
          icon={AlertCircle}
          title={error}
          description="Check your connection, then try again."
          actions={
            <button
              type="button"
              onClick={retry}
              className="inline-flex items-center gap-2 rounded-xl bg-primary-700 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2"
            >
              <RotateCw className="size-4" aria-hidden="true" />
              Try again
            </button>
          }
        />
      ) : items.length === 0 ? (
        debouncedSearch ? (
          <StateMessage
            icon={SearchX}
            title="No matching products"
            description={`Nothing matches “${debouncedSearch}”. Try a different product or shop name.`}
          />
        ) : (
          <StateMessage
            icon={PackageSearch}
            title="No products yet"
            description="Products registered by sellers will appear here for review."
          />
        )
      ) : (
        <div
          className="overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm"
          aria-busy={loading}
        >
          <ProductManagementTable items={items} />

          {totalItems > 0 && (
            <div className="flex w-full items-center justify-around border-t border-border-default bg-neutral-50/30 px-6 py-5">
              <Pagination
                currentPage={page}
                totalPages={totalPages}
                onPageChange={(p) => {
                  setLoading(true);
                  setPage(p);
                }}
                totalItems={totalItems}
                pageSize={PAGE_SIZE}
                itemLabel="products"
                showTotalItems={true}
                showFirstLast={true}
                className="w-full"
              />
            </div>
          )}
        </div>
      )}
    </div>
  );
}