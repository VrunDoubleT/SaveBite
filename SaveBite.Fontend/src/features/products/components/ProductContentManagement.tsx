import { ProductRevisionHistory } from "./ProductRevisionHistory";
import { WarningCorrectionReview } from "./WarningCorrectionReview";
import { useState, type ReactNode } from "react";
import {
  CalendarClock,
  ClipboardCheck,
  ImageOff,
  Layers,
  ListChecks,
  Package,
  Store,
  Tag,
  type LucideIcon,
} from "lucide-react";
import type { ProductContentManagement as ProductContentManagementType } from "../types/product.types";

const formatPrice = (price: number | null | undefined) =>
  price == null ? "—" : `${price.toLocaleString("vi-VN")} ₫`;

const formatDate = (date: string | null | undefined) =>
  date ? new Date(date).toLocaleString("vi-VN") : "—";

/* ---------- Small building blocks ---------- */

function IconTile({
  icon: Icon,
  tone = "primary",
}: {
  icon: LucideIcon;
  tone?: "primary" | "neutral";
}) {
  return (
    <span
      className={`flex size-9 shrink-0 items-center justify-center rounded-xl ${
        tone === "primary" ? "bg-primary-50 text-primary-700" : "bg-neutral-100 text-text-secondary"
      }`}
    >
      <Icon className="size-[18px]" aria-hidden="true" />
    </span>
  );
}

function Section({
  title,
  icon,
  count,
  children,
  flush = false,
}: {
  title: string;
  icon: LucideIcon;
  count?: number;
  children: ReactNode;
  flush?: boolean;
}) {
  return (
    <section className="overflow-hidden rounded-3xl border border-border-default bg-white shadow-[0_1px_2px_rgba(16,24,40,0.04)]">
      <header className="flex items-center gap-3 px-6 pb-4 pt-5">
        <IconTile icon={icon} />
        <h2 className="text-base font-semibold text-text-primary">{title}</h2>
        {count != null && (
          <span className="rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-semibold tabular-nums text-text-secondary">
            {count}
          </span>
        )}
      </header>

      <div className={flush ? "border-t border-border-default" : "px-6 pb-6"}>{children}</div>
    </section>
  );
}

function EmptyHint({ children }: { children: ReactNode }) {
  return (
    <p className="rounded-2xl bg-neutral-50 px-4 py-8 text-center text-sm text-text-secondary">
      {children}
    </p>
  );
}

function StatusPill({ label, badge, dot }: { label: string; badge: string; dot: string }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${badge}`}
    >
      <span className={`size-1.5 rounded-full ${dot}`} aria-hidden="true" />
      {label}
    </span>
  );
}

function StatusBadge({ status }: { status: number }) {
  if (status === 0) {
    return <StatusPill label="Active" badge="bg-green-50 text-green-700" dot="bg-green-500" />;
  }
  if (status === 1) {
    return (
      <StatusPill label="Hidden" badge="bg-neutral-100 text-neutral-600" dot="bg-neutral-400" />
    );
  }
  return (
    <StatusPill
      label={`Unknown (${status})`}
      badge="bg-yellow-50 text-yellow-700"
      dot="bg-yellow-500"
    />
  );
}

function MetaItem({
  icon: Icon,
  label,
  value,
}: {
  icon: LucideIcon;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-center gap-3 rounded-2xl bg-neutral-50 px-3.5 py-3">
      <IconTile icon={Icon} tone="neutral" />
      <div className="min-w-0">
        <dt className="text-xs font-medium text-text-muted">{label}</dt>
        <dd className="truncate text-sm font-semibold text-text-primary" title={value}>
          {value}
        </dd>
      </div>
    </div>
  );
}

const REVIEW_STATUS: Record<number, { label: string; tone: string; accent: string }> = {
  1: {
    label: "Pending",
    tone: "bg-yellow-50 text-yellow-700 ring-yellow-200",
    accent: "border-l-yellow-400",
  },
  2: {
    label: "Approved",
    tone: "bg-green-50 text-green-700 ring-green-200",
    accent: "border-l-green-500",
  },
  3: {
    label: "Needs revision",
    tone: "bg-orange-50 text-orange-700 ring-orange-200",
    accent: "border-l-orange-400",
  },
  4: {
    label: "Suspended",
    tone: "bg-red-50 text-red-700 ring-red-200",
    accent: "border-l-red-500",
  },
};

const reviewMeta = (status: number) =>
  REVIEW_STATUS[status] ?? {
    label: `Unknown (${status})`,
    tone: "bg-neutral-100 text-neutral-700 ring-neutral-200",
    accent: "border-l-neutral-300",
  };

/* ---------- Main component ---------- */

export function ProductContentManagement({
  product: p,
}: {
  product: ProductContentManagementType;
}) {
  const [activeIndex, setActiveIndex] = useState(0);

  const activeImage = p.images[activeIndex] ?? p.images[0];

  const imageCaption = (image: (typeof p.images)[number]) =>
    image.source === "value"
      ? image.attributeName && image.value
        ? `${image.attributeName}: ${image.value}`
        : "Attribute image"
      : "Product image";

  const dealPrices = p.variants
    .map((v) => v.dealPrice)
    .filter((price): price is number => typeof price === "number");
  const minPrice = dealPrices.length ? Math.min(...dealPrices) : null;
  const maxPrice = dealPrices.length ? Math.max(...dealPrices) : null;
  const activeVariantCount = p.variants.filter((v) => v.isActive).length;

  const resolveValue = (id: string) => {
    const attribute = p.attributes.find((a) => a.values.some((v) => v.id === id));
    const value = attribute?.values.find((v) => v.id === id);
    return value ? { attribute: attribute?.name ?? "", value: value.value } : null;
  };

  const review = p.latestReview ? reviewMeta(p.latestReview.status) : null;

  return (
    <div className="space-y-6">
      {p.warningComparison && <WarningCorrectionReview comparison={p.warningComparison} />}
      <ProductRevisionHistory key={p.id} revisions={p.revisionHistory ?? []} />
      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        {/* ===== Main column ===== */}
        <div className="min-w-0 space-y-6">
          {/* Overview */}
          <section className="overflow-hidden rounded-3xl border border-border-default bg-white shadow-[0_1px_2px_rgba(16,24,40,0.04)]">
            <div className="grid gap-8 p-6 md:grid-cols-[300px_minmax(0,1fr)]">
              {/* Gallery */}
              <div className="space-y-3">
                <div className="group relative flex aspect-square items-center justify-center overflow-hidden rounded-2xl bg-neutral-50 ring-1 ring-inset ring-black/5">
                  {activeImage ? (
                    <a
                      href={activeImage.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="size-full focus:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary-500"
                    >
                      <img
                        src={activeImage.url}
                        alt={activeImage.value || p.name}
                        className="size-full object-cover transition-transform duration-300 group-hover:scale-[1.03] motion-reduce:transition-none"
                      />
                    </a>
                  ) : (
                    <div className="flex flex-col items-center gap-2 text-text-muted">
                      <ImageOff className="size-8" aria-hidden="true" />
                      <span className="text-xs">No images available</span>
                    </div>
                  )}

                  {activeImage && (
                    <span className="pointer-events-none absolute bottom-3 left-3 max-w-[calc(100%-1.5rem)] truncate rounded-full bg-black/60 px-2.5 py-1 text-xs font-medium text-white backdrop-blur">
                      {imageCaption(activeImage)}
                    </span>
                  )}
                </div>

                {p.images.length > 1 && (
                  <ul className="flex gap-2 overflow-x-auto pb-1">
                    {p.images.map((image, index) => (
                      <li key={image.id} className="shrink-0">
                        <button
                          type="button"
                          onClick={() => setActiveIndex(index)}
                          aria-label={`View image ${index + 1}: ${imageCaption(image)}`}
                          aria-current={index === activeIndex}
                          className={`block size-14 overflow-hidden rounded-xl ring-2 ring-offset-2 transition focus:outline-none focus-visible:ring-primary-500 ${
                            index === activeIndex
                              ? "ring-primary-600"
                              : "opacity-60 ring-transparent hover:opacity-100"
                          }`}
                        >
                          <img src={image.url} alt="" className="size-full object-cover" />
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              {/* Info */}
              <div className="min-w-0 space-y-6">
                <div className="space-y-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <StatusBadge status={p.status} />
                    {p.categoryName && (
                      <span className="inline-flex items-center gap-1.5 rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-text-secondary">
                        <Tag className="size-3" aria-hidden="true" />
                        {p.categoryName}
                      </span>
                    )}
                  </div>

                  <h2 className="text-balance text-2xl font-bold leading-tight tracking-tight text-text-primary">
                    {p.name}
                  </h2>

                  <p className="text-2xl font-semibold tabular-nums tracking-tight text-primary-700">
                    {minPrice == null
                      ? "—"
                      : minPrice === maxPrice
                        ? formatPrice(minPrice)
                        : `${formatPrice(minPrice)} – ${formatPrice(maxPrice)}`}
                  </p>
                </div>

                <dl className="grid gap-3 sm:grid-cols-2">
                  <MetaItem icon={Store} label="Shop" value={p.shopName || "—"} />
                  <MetaItem icon={Tag} label="Category" value={p.categoryName || "—"} />
                  <MetaItem icon={CalendarClock} label="Created" value={formatDate(p.createdAt)} />
                  <MetaItem icon={CalendarClock} label="Updated" value={formatDate(p.updatedAt)} />
                </dl>

                <div>
                  <h3 className="mb-2 text-sm font-semibold text-text-primary">Description</h3>
                  <p className="max-w-prose whitespace-pre-wrap text-sm leading-relaxed text-text-secondary">
                    {p.description || "No description"}
                  </p>
                </div>
              </div>
            </div>
          </section>

          {/* Attributes */}
          <Section title="Attributes" icon={ListChecks} count={p.attributes.length}>
            {p.attributes.length > 0 ? (
              <dl className="divide-y divide-border-default rounded-2xl border border-border-default">
                {p.attributes.map((attribute) => (
                  <div
                    key={attribute.id}
                    className="grid gap-2 px-4 py-3.5 sm:grid-cols-[160px_minmax(0,1fr)] sm:gap-6"
                  >
                    <dt className="text-sm font-semibold text-text-primary">{attribute.name}</dt>
                    <dd className="flex flex-wrap gap-2">
                      {attribute.values.length > 0 ? (
                        attribute.values.map((value) => (
                          <span
                            key={value.id}
                            className="rounded-lg bg-primary-50 px-2.5 py-1 text-xs font-medium text-primary-700"
                          >
                            {value.value}
                          </span>
                        ))
                      ) : (
                        <span className="text-sm text-text-secondary">No values.</span>
                      )}
                    </dd>
                  </div>
                ))}
              </dl>
            ) : (
              <EmptyHint>No attributes available.</EmptyHint>
            )}
          </Section>

          {/* Variants */}
          <Section
            title="Variants"
            icon={Layers}
            count={p.variants.length}
            flush={p.variants.length > 0}
          >
            {p.variants.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="min-w-full text-left text-sm">
                  <thead className="bg-neutral-50 text-xs text-text-secondary">
                    <tr>
                      {["SKU", "Values", "Original price", "Deal price", "Status"].map((column) => (
                        <th
                          key={column}
                          scope="col"
                          className="whitespace-nowrap px-6 py-3 font-semibold"
                        >
                          {column}
                        </th>
                      ))}
                    </tr>
                  </thead>

                  <tbody className="divide-y divide-border-default text-text-primary">
                    {p.variants.map((variant) => {
                      const hasDiscount =
                        variant.originalPrice != null &&
                        variant.dealPrice != null &&
                        variant.originalPrice > variant.dealPrice;

                      const discount = hasDiscount
                        ? Math.round(
                            ((variant.originalPrice! - variant.dealPrice!) /
                              variant.originalPrice!) *
                              100,
                          )
                        : 0;

                      return (
                        <tr
                          key={variant.id}
                          className={`transition-colors hover:bg-neutral-50/70 ${
                            variant.isActive ? "" : "opacity-60"
                          }`}
                        >
                          <td className="whitespace-nowrap px-6 py-4 font-mono text-xs font-medium">
                            {variant.sku || "—"}
                          </td>

                          <td className="px-6 py-4">
                            {variant.valueIds.length > 0 ? (
                              <div className="flex flex-wrap gap-1.5">
                                {variant.valueIds.map((id) => {
                                  const resolved = resolveValue(id);
                                  return (
                                    <span
                                      key={id}
                                      className="rounded-lg bg-neutral-100 px-2 py-1 text-xs text-text-secondary"
                                    >
                                      {resolved ? (
                                        <>
                                          <span className="text-text-muted">
                                            {resolved.attribute}:{" "}
                                          </span>
                                          <span className="font-medium text-text-primary">
                                            {resolved.value}
                                          </span>
                                        </>
                                      ) : (
                                        id
                                      )}
                                    </span>
                                  );
                                })}
                              </div>
                            ) : (
                              "—"
                            )}
                          </td>

                          <td
                            className={`whitespace-nowrap px-6 py-4 tabular-nums ${
                              hasDiscount ? "text-text-muted line-through" : ""
                            }`}
                          >
                            {formatPrice(variant.originalPrice)}
                          </td>

                          <td className="whitespace-nowrap px-6 py-4 tabular-nums">
                            <span className="font-semibold">{formatPrice(variant.dealPrice)}</span>
                            {hasDiscount && (
                              <span className="ml-2 rounded-md bg-red-50 px-1.5 py-0.5 text-xs font-semibold text-red-700">
                                -{discount}%
                              </span>
                            )}
                          </td>

                          <td className="px-6 py-4">
                            {variant.isActive ? (
                              <StatusPill
                                label="Active"
                                badge="bg-green-50 text-green-700"
                                dot="bg-green-500"
                              />
                            ) : (
                              <StatusPill
                                label="Inactive"
                                badge="bg-neutral-100 text-neutral-600"
                                dot="bg-neutral-400"
                              />
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            ) : (
              <EmptyHint>No variants available.</EmptyHint>
            )}
          </Section>
        </div>

        {/* ===== Sidebar ===== */}
        <aside className="space-y-6 lg:sticky lg:top-6">
          <section className="rounded-3xl border border-border-default bg-white p-5 shadow-[0_1px_2px_rgba(16,24,40,0.04)]">
            <h2 className="mb-4 text-base font-semibold text-text-primary">At a glance</h2>

            <dl className="grid grid-cols-3 gap-2 text-center">
              {[
                { label: "Images", value: p.images.length },
                { label: "Variants", value: p.variants.length },
                { label: "Active", value: activeVariantCount },
              ].map((stat) => (
                <div key={stat.label} className="rounded-2xl bg-neutral-50 px-2 py-3">
                  <dd className="text-xl font-bold tabular-nums text-text-primary">{stat.value}</dd>
                  <dt className="text-xs text-text-secondary">{stat.label}</dt>
                </div>
              ))}
            </dl>
          </section>

          <section
            className={`overflow-hidden rounded-3xl border border-l-4 border-border-default bg-white shadow-[0_1px_2px_rgba(16,24,40,0.04)] ${
              review ? review.accent : "border-l-neutral-200"
            }`}
          >
            <header className="flex items-center gap-3 px-5 pb-3 pt-5">
              <IconTile icon={ClipboardCheck} />
              <h2 className="text-base font-semibold text-text-primary">Latest content review</h2>
            </header>

            <div className="px-5 pb-5">
              {p.latestReview && review ? (
                <div className="space-y-4">
                  <span
                    className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ring-1 ring-inset ${review.tone}`}
                  >
                    {review.label}
                  </span>

                  <div className="rounded-2xl bg-neutral-50 p-4">
                    <p className="mb-1 text-xs font-medium text-text-muted">Note</p>
                    <p className="whitespace-pre-wrap text-sm leading-relaxed text-text-primary">
                      {p.latestReview.note || "No note"}
                    </p>
                  </div>

                  <p className="text-xs text-text-secondary">
                    Reviewed {formatDate(p.latestReview.reviewedAt)}
                  </p>
                </div>
              ) : (
                <div className="flex flex-col items-center gap-2 py-4 text-center text-text-secondary">
                  <Package className="size-8 text-text-muted" aria-hidden="true" />
                  <p className="text-sm">No content review recorded.</p>
                </div>
              )}
            </div>
          </section>
        </aside>
      </div>
    </div>
  );
}
