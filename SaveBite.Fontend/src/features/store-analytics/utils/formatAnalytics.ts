import type { AnalyticsPeriodInfo } from "../types/storeAnalytics.types";

const VN_TIME_ZONE = "Asia/Ho_Chi_Minh";

export function formatVnd(amount: number): string {
  return `${Math.round(amount || 0).toLocaleString("vi-VN")} ₫`;
}

export function formatNumber(value: number): string {
  return (value || 0).toLocaleString("vi-VN");
}

/** OnlinePayment, PayAtShop and Cash as people say them. */
export function formatPaymentMethod(method: string): string {
  switch (method) {
    case "OnlinePayment":
      return "Online payment";
    case "PayAtShop":
      return "Pay at the shop";
    case "Cash":
      return "Cash";
    default:
      return method;
  }
}

/** Short money for chart axes: 1,2 tỷ / 3,5 tr / 120k. */
export function formatVndCompact(amount: number): string {
  const value = Math.abs(amount || 0);
  const sign = amount < 0 ? "-" : "";
  const fmt = (n: number) => n.toLocaleString("vi-VN", { maximumFractionDigits: 1 });
  if (value >= 1_000_000_000) return `${sign}${fmt(value / 1_000_000_000)} tỷ`;
  if (value >= 1_000_000) return `${sign}${fmt(value / 1_000_000)} tr`;
  if (value >= 1_000) return `${sign}${fmt(value / 1_000)}k`;
  return `${sign}${Math.round(value)}`;
}

export function formatPercent(value: number): string {
  return `${(value || 0).toLocaleString("vi-VN", { maximumFractionDigits: 1 })}%`;
}

// Reporting days follow the Vietnam business calendar (matches the backend), whatever the browser timezone.
export function formatShortDate(dateStr: string): string {
  try {
    return new Date(dateStr).toLocaleDateString("vi-VN", { timeZone: VN_TIME_ZONE });
  } catch {
    return dateStr;
  }
}

export function formatDateTime(dateStr: string): string {
  try {
    return new Date(dateStr).toLocaleString("vi-VN", { timeZone: VN_TIME_ZONE });
  } catch {
    return dateStr;
  }
}

/** yyyy-MM-dd to dd/MM/yyyy, without any timezone conversion (the backend already speaks Vietnam dates). */
function formatDateOnly(date: string): string {
  const [y, m, d] = date.split("-");
  return `${d}/${m}/${y}`;
}

/** Short name of the selected period, used as a column heading and in the page header. */
export function describePeriod(period: AnalyticsPeriodInfo): string {
  switch (period.preset) {
    case "today":
      return "Today";
    case "yesterday":
      return "Yesterday";
    case "7d":
      return "Last 7 days";
    case "month":
      return `Month ${period.startDate.slice(5, 7)}/${period.startDate.slice(0, 4)}`;
    case "year":
      return `Year ${period.startDate.slice(0, 4)}`;
    default:
      return period.startDate === period.endDate
        ? formatDateOnly(period.startDate)
        : `${formatDateOnly(period.startDate)} to ${formatDateOnly(period.endDate)}`;
  }
}

export function formatPeriodRange(period: AnalyticsPeriodInfo): string {
  const range =
    period.startDate === period.endDate
      ? formatDateOnly(period.startDate)
      : `${formatDateOnly(period.startDate)} to ${formatDateOnly(period.endDate)}`;
  return `${range} (Vietnam time)`;
}

export function currentMonthInVietnam(): string {
  return todayInVietnam().slice(0, 7);
}

export function currentYearInVietnam(): number {
  return Number(todayInVietnam().slice(0, 4));
}

/** Today's calendar date in Vietnam as yyyy-MM-dd, used as the max for date inputs. */
export function todayInVietnam(): string {
  return new Date().toLocaleDateString("en-CA", { timeZone: VN_TIME_ZONE });
}
