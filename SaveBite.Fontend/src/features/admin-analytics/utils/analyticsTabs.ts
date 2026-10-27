import { APP_PATHS } from "@/app/router/paths";

// Each tab has its own URL: /admin/analytics/<tab id>, e.g. /admin/analytics/fees.

export const PLATFORM_ANALYTICS_TABS = [
  "overview",
  "revenue-orders",
  "fees",
  "users-shops",
  "deals",
  "payments-refunds",
] as const;

export type PlatformAnalyticsTab = (typeof PLATFORM_ANALYTICS_TABS)[number];

export const DEFAULT_PLATFORM_ANALYTICS_TAB: PlatformAnalyticsTab = "overview";

export function platformAnalyticsTabPath(tab: PlatformAnalyticsTab): string {
  return `${APP_PATHS.ADMIN_ANALYTICS}/${tab}`;
}

/** Returns the tab id when `segment` is one of the tabs, otherwise undefined. */
export function findPlatformAnalyticsTab(segment: string | undefined): PlatformAnalyticsTab | undefined {
  return PLATFORM_ANALYTICS_TABS.find((tab) => tab === segment);
}