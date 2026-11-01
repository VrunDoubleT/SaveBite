export type ChartGranularity = "hour" | "day" | "month";

/** Axis label: 14h for hourly points, 24/10 for daily points, 10/2026 for monthly points. */
export function formatBucketLabel(key: string, granularity: ChartGranularity): string {
  if (granularity === "hour") return `${key.split("T")[1] ?? ""}h`;
  const [year, month, day] = key.split("-");
  if (granularity === "month") return `${month}/${year}`;
  return month && day ? `${day}/${month}` : key;
}

/** Tooltip heading with the full date. */
export function formatBucketTitle(key: string, granularity: ChartGranularity): string {
  const [datePart, hour] = key.split("T");
  const [year, month, day] = datePart.split("-");
  if (granularity === "hour") return `${day}/${month}/${year}, ${hour}:00 to ${hour}:59`;
  if (granularity === "month") return `Month ${month}/${year}`;
  return `${day}/${month}/${year}`;
}
