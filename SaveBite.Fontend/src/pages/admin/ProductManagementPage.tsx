import { useCallback, useEffect, useState, type ReactNode } from "react";
import { AlertCircle, PackageSearch, RotateCw, SearchX } from "lucide-react";
import { productApi } from "@/features/products/api/productApi";
import { ProductManagementTable } from "@/features/products/components/ProductManagementTable";
import type { ProductListManagement } from "@/features/products/types/product.types";
import { Pagination } from "@/shared/components/pagination";
import { SearchInput } from "@/shared/components/modal/SearchInput";

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
  const [search, setSearch] = useState("");

  const [items, setItems] = useState<ProductListManagement[]>([]);
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

    void productApi
      .getProductsForManagement(
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
          setError("Unable to load products.");
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
            <h1 className="text-3xl font-bold tracking-tight text-text-primary">
              Product Management
            </h1>
            {!isInitialLoading && !error && (
              <span className="rounded-full bg-neutral-100 px-2.5 py-0.5 text-sm font-semibold tabular-nums text-text-secondary">
                {totalItems.toLocaleString("vi-VN")}
              </span>
            )}
          </div>
          <p className="text-sm text-text-secondary">
            Review products registered on the platform and moderate their content.
          </p>
        </div>

        <SearchInput
          value={searchInput}
          onChange={setSearchInput}
          placeholder="Search products or shops..."
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
            title="No matching products"
            description={`Nothing matches “${search}”. Try a different product or shop name.`}
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
          className={`space-y-5 transition-opacity ${loading ? "opacity-60" : "opacity-100"}`}
          aria-busy={loading}
        >
          <ProductManagementTable items={items} />

          <Pagination
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            totalItems={totalItems}
            pageSize={PAGE_SIZE}
            itemLabel="products"
            showFirstLast
          />
        </div>
      )}
    </div>
  );
}
