import {
  CircleCheck,
  CircleHelp,
  CirclePause,
  Clock3,
  Pencil,
  ShieldAlert,
  Store,
  type LucideIcon,
} from "lucide-react";
import type { OwnerShop } from "@/features/shops/types/ownerShop.types";

export type ShopStatusMeta = {
  label: string;
  description: string;
  icon: LucideIcon;
  badge: string;
  dot: string;
  tile: string;
  band: string;
};

export const SHOP_STATUS: Record<string, ShopStatusMeta> = {
  Active: {
    label: "Active",
    description: "Your shop is visible to customers and can receive orders.",
    icon: CircleCheck,
    badge: "bg-emerald-50 text-emerald-700 ring-emerald-200",
    dot: "bg-emerald-500",
    tile: "bg-emerald-100 text-emerald-700",
    band: "border-emerald-200 bg-emerald-50/70",
  },
  Paused: {
    label: "Paused",
    description: "Your shop is hidden from customers. Resume it when you are ready to sell again.",
    icon: CirclePause,
    badge: "bg-amber-50 text-amber-700 ring-amber-200",
    dot: "bg-amber-500",
    tile: "bg-amber-100 text-amber-700",
    band: "border-amber-200 bg-amber-50/70",
  },
  Suspended: {
    label: "Suspended",
    description:
      "Your shop was suspended by the platform. Contact support to find out how to restore it.",
    icon: ShieldAlert,
    badge: "bg-red-50 text-red-700 ring-red-200",
    dot: "bg-red-500",
    tile: "bg-red-100 text-red-700",
    band: "border-red-200 bg-red-50/70",
  },
};

export function getShopStatus(status?: string | null): ShopStatusMeta {
  return (
    SHOP_STATUS[status ?? ""] ?? {
      label: status || "Unknown",
      description: "The status of this shop could not be determined.",
      icon: CircleHelp,
      badge: "bg-slate-100 text-slate-700 ring-slate-200",
      dot: "bg-slate-400",
      tile: "bg-slate-200 text-slate-600",
      band: "border-slate-200 bg-slate-50",
    }
  );
}

export function ShopStatusBadge({ status }: { status?: string | null }) {
  const meta = getShopStatus(status);
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-semibold ring-1 ring-inset ${meta.badge}`}
    >
      <span aria-hidden className={`size-1.5 rounded-full ${meta.dot}`} />
      {meta.label}
    </span>
  );
}

export function toMinutes(value?: string | null): number | null {
  if (!value) return null;
  const [h, m] = value.split(":").map(Number);
  if (Number.isNaN(h) || Number.isNaN(m)) return null;
  return h * 60 + m;
}

export function isOpenNow(open?: string | null, close?: string | null, now = new Date()) {
  const start = toMinutes(open);
  const end = toMinutes(close);
  if (start === null || end === null) return null;
  const current = now.getHours() * 60 + now.getMinutes();
  return start <= end ? current >= start && current < end : current >= start || current < end;
}

export function OpenStateBadge({
  open,
  close,
  className = "",
}: {
  open?: string | null;
  close?: string | null;
  className?: string;
}) {
  const isOpen = isOpenNow(open, close);
  if (isOpen === null) return null;
  return (
    <span
      className={`inline-flex items-center gap-2 rounded-full py-1.5 pl-3 pr-4 text-sm font-semibold ring-1 ring-inset ${
        isOpen
          ? "bg-emerald-50 text-emerald-700 ring-emerald-200"
          : "bg-rose-50 text-rose-700 ring-rose-200"
      } ${className}`}
    >
      <span aria-hidden className="relative flex size-2.5">
        {isOpen && (
          <span className="absolute inline-flex size-full animate-ping rounded-full bg-emerald-400 opacity-60 motion-reduce:animate-none" />
        )}
        <span
          className={`relative inline-flex size-2.5 rounded-full ${
            isOpen ? "bg-emerald-500" : "bg-rose-500"
          }`}
        />
      </span>
      {isOpen ? "Open now" : "Closed now"}
    </span>
  );
}

export function ShopProfileHeader({ shop, onEdit }: { shop: OwnerShop; onEdit: () => void }) {
  const area = [shop.district, shop.city].filter(Boolean).join(", ");
  const open = shop.openingTime?.slice(0, 5);
  const close = shop.closingTime?.slice(0, 5);
  const hasHours = Boolean(open && close);

  return (
    <section className="rounded-[2rem] border border-border-default bg-bg-surface shadow-sm">
      {/* Cover */}
      <div className="relative h-48 overflow-hidden rounded-t-[2rem] sm:h-64">
        {shop.coverImageUrl ? (
          <>
            <img src={shop.coverImageUrl} alt="Shop cover" className="h-full w-full object-cover" />
            <div
              aria-hidden
              className="absolute inset-0 bg-gradient-to-t from-black/35 via-black/0 to-black/0"
            />
          </>
        ) : (
          <div className="relative flex h-full flex-col items-center justify-center gap-2 bg-gradient-to-br from-primary-200 via-primary-100 to-bg-subtle text-primary-700/70">
            <span
              aria-hidden
              className="absolute -right-10 -top-16 size-64 rounded-full bg-white/30 blur-2xl"
            />
            <span
              aria-hidden
              className="absolute -bottom-20 left-10 size-56 rounded-full bg-primary-300/30 blur-2xl"
            />
            <Store size={40} className="relative" />
            <span className="relative px-4 text-center text-xs font-medium">
              Add a cover image to make your shop stand out
            </span>
          </div>
        )}
      </div>

      {/* Identity */}
      <div className="px-5 pb-7 sm:px-8">
        <div className="flex items-end justify-between gap-4">
          <div className="relative z-10 -mt-14 flex size-28 shrink-0 items-center justify-center overflow-hidden rounded-[1.75rem] bg-bg-subtle shadow-lg ring-4 ring-bg-surface sm:-mt-16 sm:size-32">
            {shop.logoUrl ? (
              <img
                src={shop.logoUrl}
                alt={`${shop.name} logo`}
                className="h-full w-full object-cover"
              />
            ) : (
              <Store className="text-text-muted" size={36} />
            )}
          </div>

          <button
            type="button"
            onClick={onEdit}
            className="inline-flex shrink-0 items-center justify-center gap-2 rounded-full bg-primary-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-primary-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500"
          >
            <Pencil size={16} aria-hidden />
            <span className="hidden sm:inline">Edit business info</span>
            <span className="sm:hidden">Edit</span>
          </button>
        </div>

        <div className="mt-5 min-w-0">
          <h1 className="min-w-0 max-w-full truncate text-2xl font-bold tracking-tight text-text-primary sm:text-4xl">
            {shop.name}
          </h1>

          {(area || hasHours) && (
            <div className="mt-4 flex flex-wrap items-center gap-x-5 gap-y-3">
              {hasHours && (
                <div className="inline-flex items-center gap-3 rounded-full border border-border-default bg-bg-surface p-1 pr-4 shadow-sm">
                  <OpenStateBadge open={open} close={close} />
                  <span className="inline-flex items-center gap-1.5 text-sm font-medium tabular-nums text-text-primary">
                    <Clock3 size={15} className="text-text-muted" aria-hidden />
                    {open} – {close}
                  </span>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </section>
  );
}
