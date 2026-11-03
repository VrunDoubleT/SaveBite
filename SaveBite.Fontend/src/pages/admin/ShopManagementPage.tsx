import { useCallback, useEffect, useState, type ReactNode } from "react";
import { AlertCircle, RotateCw, SearchX, Store } from "lucide-react";
import { shopApi } from "@/features/shops/api/shopApi";
import { ShopManagementTable } from "@/features/shops/components/ShopManagementTable";
import type { ShopListManagement } from "@/features/shops/types/shopManagement.types";
import { Pagination } from "@/shared/components/pagination";
import { SearchInput } from "@/shared/components/modal/SearchInput";

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
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");

  const [items, setItems] = useState<ShopListManagement[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const retry = useCallback(() => setReloadKey((key) => key + 1), []);

  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, SEARCH_DEBOUNCE_MS);

    return () => clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    const controller = new AbortController();

    setLoading(true);
    setError(null);

    void shopApi
      .getShopsForManagement(
        {
          keyword: search || undefined,
          page,
          pageSize: PAGE_SIZE,
        },
        controller.signal,
      )
      .then((data) => {
        if (controller.signal.aborted) return;

        setItems(data?.items ?? []);
        setTotalItems(data?.totalItems ?? 0);
        setTotalPages(Math.max(1, data?.totalPages ?? 1));
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setError("Unable to load shops.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });

    return () => controller.abort();
  }, [page, search, reloadKey]);

  const isInitialLoading = loading && items.length === 0;

  return (
    <div className="space-y-6">
      <header className="flex flex-col justify-between gap-4 border-b border-border-default pb-5 sm:flex-row sm:items-end">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold tracking-tight text-text-primary">Shop Management</h1>
            {!isInitialLoading && !error && (
              <span className="rounded-full bg-neutral-100 px-2.5 py-0.5 text-sm font-semibold tabular-nums text-text-secondary">
                {totalItems.toLocaleString("vi-VN")}
              </span>
            )}
          </div>
          <p className="text-sm text-text-secondary">
            Review shops registered on the platform and moderate their content.
          </p>
        </div>

        <SearchInput
          value={searchInput}
          onChange={setSearchInput}
          placeholder="Search shops by name..."
          className="w-full sm:w-80"
        />
      </header>

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
        search ? (
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
          className={`space-y-5 transition-opacity ${loading ? "opacity-60" : "opacity-100"}`}
          aria-busy={loading}
        >
          <ShopManagementTable items={items} />

          <Pagination
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            totalItems={totalItems}
            pageSize={PAGE_SIZE}
            itemLabel="shops"
            showFirstLast
          />
        </div>
      )}
    </div>
  );
}
