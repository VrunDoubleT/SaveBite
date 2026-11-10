import { useCallback, useEffect, useState, type ReactNode } from "react";
import { useSearchParams } from "react-router-dom";
import {
  AlertCircle,
  RotateCw,
  SearchX,
  Store,
  CheckCircle2,
  CirclePause,
  ShieldAlert,
  Sparkles,
} from "lucide-react";
import { shopApi } from "@/features/shops/api/shopApi";
import { ShopManagementTable } from "@/features/shops/components/ShopManagementTable";
import type { ShopListManagement } from "@/features/shops/types/shopManagement.types";
import { Pagination } from "@/shared/components/pagination";
import { SearchInput } from "@/shared/components/modal/SearchInput";
import { platformAnalyticsApi } from "@/features/admin-analytics/api/platformAnalyticsApi";
import { useDebounce } from "@/shared/hooks/useDebounce";

const PAGE_SIZE = 5;
const SEARCH_DEBOUNCE_MS = 400;

function ListSkeleton() {
  return (
    <div
      role="status"

      aria-busy="true"

      aria-label="Loading shops"

      className="overflow-hidden rounded-3xl border border-border-default bg-white motion-safe:animate-pulse"
    >
      <span className="sr-only">Loading shops…</span>

      <div className="h-11 bg-neutral-50" />

      <div className="divide-y divide-border-default">
        {Array.from({ length: PAGE_SIZE }, (_, i) => (
          <div key={i} className="flex items-center gap-4 px-6 py-4">
            <div className="size-12 shrink-0 rounded-2xl bg-neutral-100" />

            <div className="min-w-0 flex-1 space-y-2">
              <div className="h-4 w-2/5 rounded bg-neutral-100" />

              <div className="h-3 w-1/4 rounded bg-neutral-100" />
            </div>

            <div className="hidden h-4 w-24 rounded bg-neutral-100 sm:block" />

            <div className="hidden h-6 w-20 rounded-full bg-neutral-100 sm:block" />
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

export function ShopManagementPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const requestedPage = Number(searchParams.get("page") ?? 1);
  const page = Number.isInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1;
  const search = searchParams.get("search") ?? "";
  const [searchInput, setSearchInput] = useState(search);
  const debouncedSearch = useDebounce(searchInput, SEARCH_DEBOUNCE_MS);

  const [items, setItems] = useState<ShopListManagement[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [counts, setCounts] = useState({
    total: 0,
    active: 0,
    paused: 0,
    suspended: 0,
    withDeals: 0,
  });
  const [loadingStats, setLoadingStats] = useState(true);

  useEffect(() => {
    setSearchInput(search);
  }, [search]);

  useEffect(() => {
    const value = debouncedSearch.trim();
    if (value === search) return;
    setLoading(true);
    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current);
        if (value) next.set("search", value);
        else next.delete("search");
        next.delete("page");
        return next;
      },
      { preventScrollReset: true },
    );
  }, [debouncedSearch, search, setSearchParams]);

  const retry = useCallback(() => {
    setLoading(true);
    setError(null);
    setLoadingStats(true);
    setReloadKey((key) => key + 1);
  }, []);

  useEffect(() => {
    let cancelled = false;
    platformAnalyticsApi
      .getShops()
      .then((data) => {
        if (cancelled) return;
        if (data)
          setCounts({
            total: data.totalShops ?? 0,
            active: data.activeShops ?? 0,
            paused: data.pausedShops ?? 0,
            suspended: data.suspendedShops ?? 0,
            withDeals: data.shopsWithActiveDeals ?? 0,
          });
        setLoadingStats(false);
      })
      .catch(() => {
        if (!cancelled) setLoadingStats(false);
      });
    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  useEffect(() => {
    const controller = new AbortController();
    let cancelled = false;
    setLoading(true);
    shopApi
      .getShopsForManagement(
        { keyword: search.trim() || undefined, page, pageSize: PAGE_SIZE },
        controller.signal,
      )
      .then((data) => {
        if (cancelled || controller.signal.aborted) return;
        setItems(data?.items ?? []);
        setTotalItems(data?.totalItems ?? 0);
        setTotalPages(Math.max(1, data?.totalPages ?? 1));
        setError(null);
        setLoading(false);
      })
      .catch(() => {
        if (!cancelled && !controller.signal.aborted) {
          setError("Unable to load shops.");
          setLoading(false);
        }
      });
    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [page, search, reloadKey]);

  useEffect(() => {
    if (!loading && !error && page > totalPages) {
      setSearchParams(
        (current) => {
          const next = new URLSearchParams(current);
          if (totalPages <= 1) next.delete("page");
          else next.set("page", String(totalPages));
          return next;
        },
        { replace: true, preventScrollReset: true },
      );
    }
  }, [loading, error, page, totalPages, setSearchParams]);

  const handlePageChange = (nextPage: number) => {
    setLoading(true);
    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current);
        next.set("page", String(nextPage));
        return next;
      },
      { preventScrollReset: true },
    );
  };

  const isInitialLoading = loading && items.length === 0;

  return (
    <div className="space-y-6">
      <header className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Shop Management</h1>
          </div>

          <p className="text-sm text-text-secondary">
            Review shops registered on the platform and moderate their content.
          </p>
        </div>

        <SearchInput
          value={searchInput}

          onChange={(val) => {
            setSearchInput(val);

            if (val.trim() !== debouncedSearch.trim()) {
              setLoading(true);
            }
          }}

          placeholder="Search shops by name..."

          className="w-full sm:w-80"
        />
      </header>

      {/* 5 Overview Stat Cards with icons (no hover, non-clickable) */}

      <div
        className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5 sm:gap-4"

        role="region"

        aria-label="Shop summary metrics"
      >
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <Store className="size-5" />
            </span>

            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">All Stores</p>

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
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-amber-50 text-amber-600">
              <CirclePause className="size-5" />
            </span>

            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">Paused</p>

              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.paused}
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

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
              <Sparkles className="size-5" />
            </span>

            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">With Flash Deals</p>

              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.withDeals}
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
        search.trim() ? (
          <StateMessage
            icon={SearchX}

            title="No matching shops"

            description={`Nothing matches “${search}”. Try a different shop name.`}
          />
        ) : (
          <StateMessage
            icon={Store}

            title="No shops yet"

            description="Shops registered by sellers will appear here for review."
          />
        )
      ) : (
        <div
          className="overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm"

          aria-busy={loading}
        >
          <ShopManagementTable items={items} />

          {totalItems > 0 && (
            <div className="flex w-full items-center justify-around border-t border-border-default bg-neutral-50/30 px-6 py-5">
              <Pagination
                currentPage={page}

                totalPages={totalPages}

                onPageChange={handlePageChange}

                totalItems={totalItems}

                pageSize={PAGE_SIZE}

                itemLabel="shops"

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
