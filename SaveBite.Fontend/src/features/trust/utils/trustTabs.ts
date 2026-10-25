import { APP_PATHS } from "@/app/router/paths";

// Each tab has its own URL: <base path>/<tab id>, e.g. /account/trust-score/history.

export const CUSTOMER_TRUST_TABS = [
  { id: "overview", label: "Overview" },
  { id: "levels", label: "Levels & benefits" },
  { id: "history", label: "Score history" },
  { id: "penalties", label: "Penalties" },
] as const;

export const ADMIN_TRUST_TABS = [
  { id: "levels", label: "Levels" },
  { id: "penalty-rules", label: "Penalty rules" },
  { id: "score-rules", label: "Trust Score rules" },
  { id: "earning-rules", label: "Earning rules" },
] as const;

export type CustomerTrustTab = (typeof CUSTOMER_TRUST_TABS)[number]["id"];
export type AdminTrustTab = (typeof ADMIN_TRUST_TABS)[number]["id"];

export const CUSTOMER_TRUST_BASE_PATH = APP_PATHS.ACCOUNT_TRUST_SCORES;
export const ADMIN_TRUST_BASE_PATH = APP_PATHS.ADMIN_TRUST;

export function tabPath(basePath: string, tabId: string): string {
  return `${basePath}/${tabId}`;
}

/** Returns the tab id when `segment` is one of the tabs, otherwise undefined. */
export function findTab<T extends string>(
  tabs: ReadonlyArray<{ id: T }>,
  segment: string | undefined,
): T | undefined {
  return tabs.find((tab) => tab.id === segment)?.id;
}
