import { Link } from "react-router-dom";
import { Eye, ImageOff, Store, Tag } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
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
  "inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-3 py-1.5 text-xs font-semibold text-text-secondary transition-colors hover:border-primary-300 hover:bg-primary-50 hover:text-primary-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500";

const manageHref = (id: string) => APP_PATHS.ADMIN_PRODUCT_CONTENT.replace(":productId", id);

export function ProductManagementTable({ items }: { items: ProductListManagement[] }) {
  return (
    <>
      {/* Desktop table */}
      <div className="hidden overflow-hidden rounded-3xl border border-border-default bg-white shadow-[0_1px_2px_rgba(16,24,40,0.04)] md:block">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-neutral-50 text-xs text-text-secondary">
            <tr>
              <th scope="col" className="px-6 py-3.5 font-semibold">
                Product
              </th>
              <th scope="col" className="px-4 py-3.5 font-semibold">
                Status
              </th>
              <th scope="col" className="px-4 py-3.5 font-semibold">
                Content review
              </th>
              <th scope="col" className="px-4 py-3.5 text-right font-semibold">
                Deal price
              </th>
              <th scope="col" className="px-6 py-3.5 text-right font-semibold">
                <span className="sr-only">Actions</span>
              </th>
            </tr>
          </thead>

          <tbody className="divide-y divide-border-default text-text-primary">
            {items.map((product) => (
              <tr key={product.id} className="transition-colors hover:bg-neutral-50/70">
                <td className="max-w-sm px-6 py-4">
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

                <td className="whitespace-nowrap px-4 py-4 text-right font-semibold tabular-nums">
                  {formatPriceRange(product.minDealPrice, product.maxDealPrice)}
                </td>

                <td className="px-6 py-4 text-right">
                  <Link to={manageHref(product.id)} className={manageLinkClass}>
                    <Eye className="size-3.5" aria-hidden="true" />
                    Manage content
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile cards */}
      <ul className="space-y-3 md:hidden">
        {items.map((product) => (
          <li
            key={product.id}
            className="space-y-4 rounded-3xl border border-border-default bg-white p-4 shadow-[0_1px_2px_rgba(16,24,40,0.04)]"
          >
            <div className="flex items-start gap-3">
              <Thumbnail product={product} />
              <ProductMeta product={product} />
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <StatusPill status={product.status} />
              <ReviewPill status={product.latestReview?.status} />
            </div>

            <div className="flex items-center justify-between gap-3 border-t border-border-default pt-3">
              <p className="text-sm font-semibold tabular-nums text-text-primary">
                {formatPriceRange(product.minDealPrice, product.maxDealPrice)}
              </p>
              <Link to={manageHref(product.id)} className={manageLinkClass}>
                <Eye className="size-3.5" aria-hidden="true" />
                Manage content
              </Link>
            </div>
          </li>
        ))}
      </ul>
    </>
  );
}
