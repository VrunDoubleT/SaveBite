import { useCallback, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { CheckCircle2, Eye, EyeOff, Layers, Package, Plus, Search } from "lucide-react";
import { Pagination } from "@/shared/components/pagination";
import { productApi } from "../api/productApi";
import { useProductResource } from "../hooks/useProductResource";
import { useProductWorkspace } from "../hooks/useProductWorkspace";
import {
  Empty,
  ErrorState,
  primaryButton,
  ProductPhoto,
  secondaryButton,
} from "../components/ProductUi";
import { ProductListSkeleton } from "../components/ProductListSkeleton";
import { money } from "../utils/productForm";
import { useShopSuspended } from "@/shared/hooks/useShopSuspended";
import { useDebounce } from "@/shared/hooks/useDebounce";

const statuses: { value: number | ""; label: string }[] = [
  { value: 0, label: "Active" },
  { value: 1, label: "Hidden" },
];

export function ProductListPage() {
  const { shop, categories, productPath } = useProductWorkspace();
  const shopSuspended = useShopSuspended();
  const [searchInput, setSearchInput] = useState("");
  const debouncedSearch = useDebounce(searchInput, 350);
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [categoryId, setCategoryId] = useState("");
  const [status, setStatus] = useState<number | "">("");

  const query = useMemo(
    () => ({
      page,
      pageSize,
      search: debouncedSearch.trim(),
      categoryId,
      status,
    }),
    [page, pageSize, debouncedSearch, categoryId, status],
  );

  const loadSummary = useCallback((signal: AbortSignal) => productApi.summary(signal), []);
  const { data: summary } = useProductResource(`${shop.id}:product-summary`, loadSummary);
  const load = useCallback(
    (signal: AbortSignal) =>
      productApi.list(
        {
          ...query,
          categoryId: query.categoryId || undefined,
          status: query.status !== "" ? query.status : undefined,
        },
        signal,
      ),
    [query],
  );
  const { data, loading, error, reload } = useProductResource(
    `${shop.id}:${JSON.stringify(query)}`,
    load,
  );
  const filtered = Boolean(debouncedSearch.trim() || categoryId || status !== "");
  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Product Management</h1>
          <p className="mt-1 text-sm text-text-secondary">
            Manage your products, variants, and prices.
          </p>
        </div>
        {shopSuspended ? (
          <span
            aria-disabled="true"
            title="Creating products is unavailable while your shop is suspended."
            className={`${primaryButton} pointer-events-none cursor-not-allowed opacity-50`}
          >
            <Plus className="size-4" />
            Add product
          </span>
        ) : (
          <Link to={productPath("/new")} className={primaryButton}>
            <Plus className="size-4" />
            Add product
          </Link>
        )}
      </div>
      {/* Summary counters */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 sm:gap-4">
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <Package className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Total products</p>
              <p className="text-xl font-bold text-neutral-900">{summary?.totalProducts ?? "—"}</p>
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
              <CheckCircle2 className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Active</p>
              <p className="text-xl font-bold text-emerald-600">{summary?.activeProducts ?? "—"}</p>
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-amber-50 text-amber-600">
              <EyeOff className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Hidden</p>
              <p className="text-xl font-bold text-amber-600">{summary?.hiddenProducts ?? "—"}</p>
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-primary-50 text-primary-600">
              <Layers className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Total variants</p>
              <p className="text-xl font-bold text-primary-700">{summary?.totalVariants ?? "—"}</p>
            </div>
          </div>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        {/* Realtime Search Bar without Search Button */}
        <div className="relative min-w-56 flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-neutral-400" />
          <input
            aria-label="Search products by name"
            className="h-10 w-full rounded-lg border border-neutral-200 bg-white pl-9 pr-4 text-sm text-neutral-900 placeholder:text-neutral-400 outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20"
            placeholder="Search by product name…"
            maxLength={200}
            value={searchInput}
            onChange={(e) => {
              setSearchInput(e.target.value);
              setPage(1);
            }}
          />
        </div>
        <select
          aria-label="Filter by category"
          className="h-10 w-auto! max-w-44 rounded-lg border border-neutral-200 bg-white px-3 text-sm text-neutral-700 outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20"
          value={categoryId}
          onChange={(e) => {
            setCategoryId(e.target.value);
            setPage(1);
          }}
        >
          <option value="">All categories</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        <div role="group" aria-label="Filter by product status" className="flex gap-2">
          {statuses.map((s) => (
            <button
              key={s.value}
              type="button"
              aria-pressed={status === s.value}
              onClick={() => {
                setStatus(s.value);
                setPage(1);
              }}
              className={`rounded-xl border px-3 py-2.5 text-xs font-medium transition focus-visible:ring-2 focus-visible:ring-primary-400 ${status === s.value ? "border-primary-600 bg-primary-600 text-white" : "border-neutral-200 bg-white text-neutral-600 hover:border-primary-300"}`}
            >
              {s.label}
            </button>
          ))}
        </div>
      </div>
      <div className="overflow-hidden rounded-2xl border border-neutral-100 bg-white">
        {loading ? (
          <ProductListSkeleton />
        ) : error ? (
          <div className="px-5">
            <ErrorState error={error} retry={reload} />
          </div>
        ) : data?.items.length ? (
          <div className="overflow-x-auto">
            <table className="w-full min-w-190 text-left text-sm">
              <thead className="border-b border-neutral-100 text-[10px] uppercase tracking-wider text-neutral-400">
                <tr>
                  {["Products", "Category", "Price", "Variants", "Status", "Actions"].map(
                    (text) => (
                      <th key={text} scope="col" className="px-5 py-4 font-medium">
                        {text}
                      </th>
                    ),
                  )}
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-50">
                {data.items.map((product) => (
                  <tr key={product.id} className="transition hover:bg-primary-50/30">
                    <td className="px-5 py-4">
                      <Link
                        to={productPath(`/${product.id}`)}
                        className="group flex items-center gap-3"
                      >
                        <ProductPhoto
                          src={product.images[0]?.url}
                          alt={product.name}
                          className="size-12"
                        />
                        <div className="max-w-64">
                          <span className="line-clamp-2 font-medium group-hover:text-primary-700">
                            {product.name}
                          </span>
                          <span className="mt-1 inline-block rounded-full bg-neutral-50 px-2 py-0.5 text-[10px] text-neutral-500">
                            {product.variantCount} variants
                          </span>
                        </div>
                      </Link>
                    </td>
                    <td className="px-5 py-4 text-neutral-600">{product.categoryName}</td>
                    <td className="px-5 py-4 font-semibold text-primary-600">
                      {money(product.minDealPrice)}
                      {product.maxDealPrice != null &&
                        product.minDealPrice != null &&
                        product.maxDealPrice > product.minDealPrice && (
                          <>
                            <span> – </span>
                            <span className="block">{money(product.maxDealPrice)}</span>
                          </>
                        )}
                    </td>
                    <td className="px-5 py-4">
                      <span className="inline-flex items-center gap-1.5 rounded-full bg-neutral-100 px-3 py-1 text-xs text-neutral-500">
                        <Layers className="size-3" />
                        {product.variantCount} options
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <span
                        className={`inline-flex whitespace-nowrap rounded-full px-2.5 py-1 text-xs ${product.status === 1 ? "bg-neutral-100 text-neutral-600" : "bg-primary-50 text-primary-700"}`}
                      >
                        {product.status === 1 ? "Hidden" : "Active"}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <Link
                        className="inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-600 transition hover:bg-neutral-50 hover:text-neutral-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
                        to={productPath(`/${product.id}`)}
                        aria-label={`View ${product.name}`}
                        title="View"
                      >
                        <Eye className="size-3.5" aria-hidden="true" />
                        <span>View</span>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="p-5">
            <Empty
              title={filtered ? "No matching products" : "Add your first product"}
              description={
                filtered
                  ? "Try a different name or clear the filters."
                  : "Add product details, images, and variants for your shop."
              }
            >
              {filtered ? (
                <button
                  className={secondaryButton}
                  onClick={() => {
                    setSearchInput("");
                    setCategoryId("");
                    setStatus("");
                    setPage(1);
                  }}
                >
                  Clear filters
                </button>
              ) : shopSuspended ? (
                <span
                  aria-disabled="true"
                  title="Creating products is unavailable while your shop is suspended."
                  className={`${primaryButton} pointer-events-none cursor-not-allowed opacity-50`}
                >
                  <Plus className="size-4" />
                  Add product
                </span>
              ) : (
                <Link to={productPath("/new")} className={primaryButton}>
                  <Plus className="size-4" />
                  Add product
                </Link>
              )}
            </Empty>
          </div>
        )}
      </div>
      {!loading && !error && data && data.totalItems > 0 && (
        <Pagination
          currentPage={data.page}
          totalPages={data.totalPages}
          totalItems={data.totalItems}
          pageSize={data.pageSize}
          onPageChange={(nextPage) => setPage(nextPage)}
          itemLabel="products"
          showTotalItems={true}
          showFirstLast={true}
          className="w-full"
        />
      )}
    </div>
  );
}