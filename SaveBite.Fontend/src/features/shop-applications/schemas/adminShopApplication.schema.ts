import type {
  ShopApplicationAdminFilter,
  ShopApplicationReviewDecision,
} from "../types/adminShopApplication.types";

export const ADMIN_SHOP_APPLICATION_FILTERS: readonly ShopApplicationAdminFilter[] = [
  "All",
  "Pending",
  "Approved",
  "Rejected",
  "Cancelled",
];

export const ADMIN_SHOP_APPLICATION_NOTE_MAX_LENGTH = 2000;

export function isShopApplicationReviewDecision(
  value: string,
): value is ShopApplicationReviewDecision {
  return value === "Approved" || value === "Rejected";
}

import { Ban, CheckCircle2, Clock3, XCircle, type LucideIcon } from "lucide-react";

export const dateTime = (value: string) =>
  new Date(value).toLocaleString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });

export const shortDate = (value: string) =>
  new Date(value).toLocaleDateString("en-US", { year: "numeric", month: "short", day: "numeric" });

export const formatCoordinate = (value?: number | string | null) => {
  if (value === null || value === undefined || value === "") return "—";
  const number = Number(value);
  return Number.isFinite(number) ? number.toFixed(4) : String(value);
};

export type StatusMeta = { label: string; icon: LucideIcon; badge: string; dot: string };

export const STATUS_META: Record<string, StatusMeta> = {
  Pending: {
    label: "Pending",
    icon: Clock3,
    badge: "bg-amber-50 text-amber-800 ring-amber-200",
    dot: "bg-amber-100 text-amber-700 ring-amber-200",
  },
  Approved: {
    label: "Approved",
    icon: CheckCircle2,
    badge: "bg-emerald-50 text-emerald-800 ring-emerald-200",
    dot: "bg-emerald-100 text-emerald-700 ring-emerald-200",
  },
  Rejected: {
    label: "Rejected",
    icon: XCircle,
    badge: "bg-red-50 text-red-800 ring-red-200",
    dot: "bg-red-100 text-red-700 ring-red-200",
  },
  Cancelled: {
    label: "Canceled",
    icon: Ban,
    badge: "bg-slate-100 text-slate-700 ring-slate-200",
    dot: "bg-slate-100 text-slate-600 ring-slate-200",
  },
};

export const getStatusMeta = (status: string): StatusMeta =>
  STATUS_META[status] ?? { ...STATUS_META.Draft, label: status };

export const focusRing =
  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 focus-visible:ring-offset-2";

export const FILTERS = ADMIN_SHOP_APPLICATION_FILTERS;
export const NOTE_MAX_LENGTH = ADMIN_SHOP_APPLICATION_NOTE_MAX_LENGTH;
