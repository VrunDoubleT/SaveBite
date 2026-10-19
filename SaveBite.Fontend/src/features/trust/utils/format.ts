export function formatCurrency(value: number): string {
  return Number.isFinite(value) ? `${Math.round(value).toLocaleString("vi-VN")} VND` : "—";
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "—";
  return new Date(value).toLocaleString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString("vi-VN");
}

/** +5 / -20 / 0 */
export function formatSignedPoints(value: number): string {
  return value > 0 ? `+${value}` : `${value}`;
}

export const METRIC_LABELS: Record<string, string> = {
  score: "Trust Score",
  completedOrders: "Completed orders",
  orderValue: "Total order value",
  onlineOrders: "Online orders",
  onlineOrderValue: "Online order value",
};

/** Metrics measured in money are shown as currency, the rest as plain numbers. */
export function formatMetricValue(key: string, value: number): string {
  return key === "orderValue" || key === "onlineOrderValue" ? formatCurrency(value) : String(value);
}
