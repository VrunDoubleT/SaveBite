import { useCallback, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowUpRight, CheckCircle2, EyeOff, Layers, Package, Plus, Search } from "lucide-react";
import { Pagination } from "@/shared/components/pagination";
import { productApi } from "../api/productApi";
import { useProductResource } from "../hooks/useProductResource";
import { useProductWorkspace } from "../hooks/useProductWorkspace";
import {
  Empty,
  ErrorState,
  inputClass,
  primaryButton,
  ProductPhoto,
  secondaryButton,
} from "../components/ProductUi";
import { ProductListSkeleton } from "../components/ProductListSkeleton";
import { money } from "../utils/productForm";

const statuses: { value: number | ""; label: string }[] = [
  { value: 0, label: "Active" },
  { value: 1, label: "Hidden" },
];

export function ProductListPage() {
  const { shop, categories, productPath } = useProductWorkspace();
  const [searchInput, setSearchInput] = useState("");
  const [query, setQuery] = useState<{
    page: number;
    pageSize: number;
    search: string;
    categoryId: string;
    status: number | "";
  }>({
    page: 1,
    pageSize: 20,
    search: "",
    categoryId: "",
    status: "",
  });
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
  const filtered = Boolean(query.search || query.categoryId || query.status !== "");
  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Product management</h1>
          <p className="mt-1 text-sm text-text-secondary">
            Manage your products, variants, and prices.
          </p>
        </div>
        <Link to={productPath("/new")} className={primaryButton}>
          <Plus className="size-4" />
          Add product
        </Link>
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
        <form
          className="flex min-w-56 flex-1 gap-2"
          onSubmit={(e) => {
            e.preventDefault();
            setQuery({ ...query, search: searchInput.trim(), page: 1 });
          }}
        >
          <div className="relative flex-1">
            <Search className="absolute top-3 left-3 size-4 text-neutral-400" />
            <input
              aria-label="Search products by name"
              className={`${inputClass} mt-0! pl-9`}
              placeholder="Search by product name…"
              maxLength={200}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <button className={secondaryButton} type="submit">
            Search
          </button>
        </form>
        <select
          aria-label="Filter by category"
          className={`${inputClass} mt-0! w-auto! max-w-44`}
          value={query.categoryId}
          onChange={(e) => setQuery({ ...query, categoryId: e.target.value, page: 1 })}
        >
          <option value="">All categories</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        <div role="group" aria-label="Filter by product status" className="flex gap-2">
          {statuses.map((status) => (
            <button
              key={status.value}
              type="button"
              aria-pressed={query.status === status.value}
              onClick={() => setQuery({ ...query, status: status.value, page: 1 })}
              className={`rounded-xl border px-3 py-2.5 text-xs font-medium transition focus-visible:ring-2 focus-visible:ring-primary-400 ${query.status === status.value ? "border-primary-600 bg-primary-600 text-white" : "border-neutral-200 bg-white text-neutral-600 hover:border-primary-300"}`}
            >
              {status.label}
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
            <table className="w-full min-w-[760px] text-left text-sm">
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
                        className="inline-flex size-9 items-center justify-center rounded-lg border border-neutral-200 hover:border-primary-400 hover:text-primary-700 focus-visible:ring-2 focus-visible:ring-primary-400"
                        to={productPath(`/${product.id}`)}
                        aria-label={`View ${product.name}`}
                      >
                        <ArrowUpRight className="size-4" />
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
                    setQuery({ page: 1, pageSize: 20, search: "", categoryId: "", status: "" });
                  }}
                >
                  Clear filters
                </button>
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
          onPageChange={(page) => setQuery((prev) => ({ ...prev, page }))}
          itemLabel="products"
          showTotalItems={true}
          showFirstLast={true}
          className="w-full"
        />
      )}
    </div>
  );
}
