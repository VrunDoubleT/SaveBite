import { Skeleton } from "@/shared/components/ui/Content";
import { TableSkeletonRows } from "@/shared/components/ui/LoadingSkeleton";
import { Link } from "react-router-dom";
import { Eye, ImageOff, Store, Tag } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { RecordId } from "@/shared/components/RecordId";
import type { ProductListManagement } from "../types/product.types";

const formatPrice = (price: number | null | undefined) =>
  price == null ? "—" : `${price.toLocaleString("vi-VN")} ₫`;

const formatPriceRange = (min: number | null | undefined, max: number | null | undefined) => {
  if (min == null && max == null) return "—";
  if (min === max || max == null) return formatPrice(min);
  if (min == null) return formatPrice(max);
  return `${formatPrice(min)} – ${formatPrice(max)}`;
};

const STATUS: Record<number, { label: string; badge: string; dot: string }> = {
  0: { label: "Active", badge: "bg-green-50 text-green-700", dot: "bg-green-500" },
  1: { label: "Hidden", badge: "bg-neutral-100 text-neutral-600", dot: "bg-neutral-400" },
};

const REVIEW: Record<number, { label: string; badge: string; dot: string }> = {
  2: { label: "Approved", badge: "bg-green-50 text-green-700", dot: "bg-green-500" },
  3: { label: "Needs revision", badge: "bg-orange-50 text-orange-700", dot: "bg-orange-400" },
  4: { label: "Suspended", badge: "bg-red-50 text-red-700", dot: "bg-red-500" },
};

const PENDING = { label: "Pending", badge: "bg-amber-50 text-amber-700", dot: "bg-amber-400" };

function Pill({ label, badge, dot }: { label: string; badge: string; dot: string }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-semibold ${badge}`}
    >
      <span className={`size-1.5 rounded-full ${dot}`} aria-hidden="true" />
      {label}
    </span>
  );
}

function StatusPill({ status }: { status: number }) {
  return (
    <Pill
      {...(STATUS[status] ?? {
        label: `Unknown (${status})`,
        badge: "bg-yellow-50 text-yellow-700",
        dot: "bg-yellow-500",
      })}
    />
  );
}

function ReviewPill({ status }: { status?: number }) {
  return <Pill {...((status != null && REVIEW[status]) || PENDING)} />;
}

function EditSource({ product }: { product: ProductListManagement }) {
  const review = product.latestReview;
  if (review?.status !== 1) return <span className="text-text-muted">—</span>;
  if (!review.warningId)
    return <Pill label="Shop update" badge="bg-blue-50 text-blue-700" dot="bg-blue-500" />;
  return (
    <div className="space-y-1.5">
      <Pill label="Warning correction" badge="bg-violet-50 text-violet-700" dot="bg-violet-500" />
      <p
        className={`text-xs font-medium ${review.correctionSubmitted ? "text-green-700" : "text-orange-700"}`}
      >
        {review.correctionSubmitted
          ? "Correction submitted · awaiting review"
          : "No content changes submitted"}
      </p>
    </div>
  );
}

function Thumbnail({ product }: { product: ProductListManagement }) {
  const image =
    product.images?.find((item) => item.source === "product")?.url ?? product.images?.[0]?.url;

  return image ? (
    <img
      src={image}
      alt=""
      loading="lazy"
      className="size-14 shrink-0 rounded-2xl object-cover ring-1 ring-inset ring-black/5"
    />
  ) : (
    <div className="flex size-14 shrink-0 items-center justify-center rounded-2xl bg-neutral-100 text-text-muted">
      <ImageOff className="size-5" aria-hidden="true" />
    </div>
  );
}

function ProductMeta({ product }: { product: ProductListManagement }) {
  return (
    <div className="min-w-0 space-y-1">
      <p className="truncate font-semibold text-text-primary" title={product.name}>
        {product.name}
      </p>
      <p className="flex items-center gap-1.5 truncate text-xs text-text-secondary">
        <Store className="size-3.5 shrink-0 text-text-muted" aria-hidden="true" />
        <span className="truncate">{product.shopName || "—"}</span>
      </p>
      {product.categoryName && (
        <p className="flex items-center gap-1.5 truncate text-xs text-text-secondary">
          <Tag className="size-3.5 shrink-0 text-text-muted" aria-hidden="true" />
          <span className="truncate">{product.categoryName}</span>
        </p>
      )}
    </div>
  );
}

const manageLinkClass =
  "inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-600 transition hover:bg-neutral-50 hover:text-neutral-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700";

const manageHref = (id: string) => APP_PATHS.ADMIN_PRODUCT_CONTENT.replace(":productId", id);

export function ProductManagementTable({
  items,
  loading = false,
}: {
  items: ProductListManagement[];
  loading?: boolean;
}) {
  return (
    <>
      {/* Desktop table */}
      <div className="hidden min-w-0 2xl:block">
        <table className="w-full table-fixed text-left text-sm">
          <colgroup>
            <col className="w-[12%]" />
            <col className="w-[25%]" />
            <col className="w-[12%]" />
            <col className="w-[15%]" />
            <col className="w-[12%]" />
            <col className="w-[14%]" />
            <col className="w-[10%]" />
          </colgroup>
          <thead className="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-text-secondary">
            <tr>
              <th scope="col" className="px-4 py-4">
                ID
              </th>
              <th scope="col" className="px-4 py-4">
                Product
              </th>
              <th scope="col" className="px-4 py-4">
                Status
              </th>
              <th scope="col" className="px-4 py-4">
                Content review
              </th>
              <th scope="col" className="px-4 py-4">
                Edit source
              </th>
              <th scope="col" className="px-4 py-4 text-right">
                Deal price
              </th>
              <th scope="col" className="px-6 py-4 text-right">
                Actions
              </th>
            </tr>
          </thead>

          <tbody className="divide-y divide-border-default text-text-primary">
            {loading ? (
              <TableSkeletonRows columns={7} rows={5} label="Loading products" />
            ) : (
              items.map((product) => (
                <tr key={product.id} className="transition-colors hover:bg-neutral-50/70">
                  <td className="max-w-40 px-4 py-4">
                    <RecordId id={product.id} className="w-24 max-w-none shrink-0 justify-center" />
                  </td>
                  <td className="max-w-sm px-4 py-4">
                    <div className="flex items-center gap-4">
                      <Thumbnail product={product} />
                      <ProductMeta product={product} />
                    </div>
                  </td>

                  <td className="px-4 py-4">
                    <StatusPill status={product.status} />
                  </td>

                  <td className="px-4 py-4">
                    <ReviewPill status={product.latestReview?.status} />
                  </td>

                  <td className="px-4 py-4">
                    <EditSource product={product} />
                  </td>

                  <td className="px-4 py-4 text-right font-semibold tabular-nums">
                    {formatPriceRange(product.minDealPrice, product.maxDealPrice)}
                  </td>

                  <td className="px-6 py-4 text-right">
                    <Link
                      to={manageHref(product.id)}
                      className={manageLinkClass}
                      title="View"
                      aria-label="View product"
                    >
                      <Eye className="size-3.5" aria-hidden="true" />
                      <span>View</span>
                    </Link>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Mobile list */}
      <ul className="divide-y divide-border-default 2xl:hidden">
        {loading
          ? Array.from({ length: 5 }, (_, index) => (
              <li key={index} aria-hidden="true" className="min-w-0 space-y-4 p-4 sm:p-5">
                <div className="flex justify-between gap-3">
                  <Skeleton className="h-6 w-24" />
                  <Skeleton className="h-8 w-16" />
                </div>
                <div className="flex items-center gap-3">
                  <Skeleton className="size-14 shrink-0 rounded-2xl" />
                  <div className="min-w-0 flex-1 space-y-2">
                    <Skeleton className="h-5 w-2/3" />
                    <Skeleton className="h-4 w-1/2" />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4 border-t border-border-default pt-4 sm:grid-cols-4">
                  {[0, 1, 2, 3].map((cell) => (
                    <div key={cell} className="min-w-0 space-y-2">
                      <Skeleton className="h-3 w-16" />
                      <Skeleton className="h-6 w-full" />
                    </div>
                  ))}
                </div>
              </li>
            ))
          : items.map((product) => (
              <li key={product.id} className="min-w-0 space-y-4 p-4 sm:p-5">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div className="flex min-w-0 items-center gap-2 text-xs text-text-secondary">
                    <span>ID</span>
                    <RecordId id={product.id} className="w-24 max-w-none shrink-0 justify-center" />
                  </div>
                  <Link
                    to={manageHref(product.id)}
                    className={manageLinkClass}
                    title="View"
                    aria-label="View product"
                  >
                    <Eye className="size-3.5" aria-hidden="true" />
                    <span>View</span>
                  </Link>
                </div>

                <div className="flex min-w-0 items-start gap-3">
                  <Thumbnail product={product} />
                  <ProductMeta product={product} />
                </div>

                <dl className="grid min-w-0 grid-cols-2 gap-x-4 gap-y-3 border-t border-border-default pt-4 sm:grid-cols-4">
                  <div className="min-w-0 space-y-1">
                    <dt className="text-xs font-medium text-text-secondary">Status</dt>
                    <dd>
                      <StatusPill status={product.status} />
                    </dd>
                  </div>
                  <div className="min-w-0 space-y-1">
                    <dt className="text-xs font-medium text-text-secondary">Content review</dt>
                    <dd>
                      <ReviewPill status={product.latestReview?.status} />
                    </dd>
                  </div>
                  <div className="col-span-2 min-w-0 space-y-1 sm:col-span-1">
                    <dt className="text-xs font-medium text-text-secondary">Deal price</dt>
                    <dd className="break-words text-sm font-semibold tabular-nums text-text-primary">
                      {formatPriceRange(product.minDealPrice, product.maxDealPrice)}
                    </dd>
                  </div>
                  <div className="col-span-2 min-w-0 space-y-1 sm:col-span-1">
                    <dt className="text-xs font-medium text-text-secondary">Edit source</dt>
                    <dd className="min-w-0 text-sm">
                      <EditSource product={product} />
                    </dd>
                  </div>
                </dl>
              </li>
            ))}
      </ul>
    </>
  );
}
