import { useCallback, useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import type { AnalyticsPeriodQuery, AnalyticsPreset } from "../types/storeAnalytics.types";
import { currentMonthInVietnam, currentYearInVietnam } from "../utils/formatAnalytics";

export const MAX_CUSTOM_RANGE_DAYS = 31;

export type DetailView = "selected" | "overall";

const PRESETS: readonly AnalyticsPreset[] = ["today", "yesterday", "7d", "month", "year"];
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
const MONTH_PATTERN = /^\d{4}-(0[1-9]|1[0-2])$/;

// URL parameters. Defaults are left out, so the first visit has a clean URL.
const P_PRESET = "preset";
const P_MONTH = "month";
const P_YEAR = "year";
const P_FROM = "from";
const P_TO = "to";
const P_VIEW = "view";
const VIEW_ALL_TIME = "all-time";

function daysBetweenInclusive(from: string, to: string): number {
  const ms = Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`);
  return Math.round(ms / 86_400_000) + 1;
}

/**
 * Period selection shared by the store and platform dashboards, kept in the URL query string
 * (?preset=month&month=2026-10, ?from=...&to=..., ?view=all-time) so a refresh or a shared link keeps it.
 * Default is today. The user can switch to yesterday, the last 7 days, a calendar month, a calendar year,
 * or two own dates (at most one month apart).
 */
export function useAnalyticsPeriod() {
  const [searchParams, setSearchParams] = useSearchParams();

  const rawPreset = searchParams.get(P_PRESET);
  const preset: AnalyticsPreset = PRESETS.find((p) => p === rawPreset) ?? "today";

  const rawMonth = searchParams.get(P_MONTH);
  const month = rawMonth && MONTH_PATTERN.test(rawMonth) ? rawMonth : currentMonthInVietnam();

  const rawYear = Number(searchParams.get(P_YEAR));
  const year = Number.isInteger(rawYear) && rawYear >= 2000 && rawYear <= 2100 ? rawYear : currentYearInVietnam();

  const rawFrom = searchParams.get(P_FROM);
  const rawTo = searchParams.get(P_TO);
  const fromDate = rawFrom && DATE_PATTERN.test(rawFrom) ? rawFrom : "";
  const toDate = rawTo && DATE_PATTERN.test(rawTo) ? rawTo : "";

  const view: DetailView = searchParams.get(P_VIEW) === VIEW_ALL_TIME ? "overall" : "selected";

  const customActive = Boolean(fromDate || toDate);

  const rangeError = useMemo(() => {
    if (!fromDate || !toDate) return null;
    if (fromDate > toDate) return "The start date must be on or before the end date.";
    if (daysBetweenInclusive(fromDate, toDate) > MAX_CUSTOM_RANGE_DAYS)
      return `Pick two dates at most ${MAX_CUSTOM_RANGE_DAYS} days apart, or use the Month or Year view.`;
    return null;
  }, [fromDate, toDate]);

  const query: AnalyticsPeriodQuery = customActive
    ? { fromDate: fromDate || undefined, toDate: toDate || undefined }
    : preset === "month"
      ? { preset, month }
      : preset === "year"
        ? { preset, year }
        : { preset };

  const update = useCallback(
    (mutate: (next: URLSearchParams) => void) => {
      setSearchParams((previous) => {
        const next = new URLSearchParams(previous);
        mutate(next);
        return next;
      });
    },
    [setSearchParams],
  );

  const selectPreset = useCallback(
    (next: AnalyticsPreset) =>
      update((params) => {
        params.delete(P_FROM);
        params.delete(P_TO);
        if (next !== "month") params.delete(P_MONTH);
        if (next !== "year") params.delete(P_YEAR);
        if (next === "today") params.delete(P_PRESET);
        else params.set(P_PRESET, next);
      }),
    [update],
  );

  const selectMonth = useCallback(
    (next: string) => {
      if (!next) return;
      update((params) => {
        params.delete(P_FROM);
        params.delete(P_TO);
        params.delete(P_YEAR);
        params.set(P_PRESET, "month");
        params.set(P_MONTH, next);
      });
    },
    [update],
  );

  const selectYear = useCallback(
    (next: number) =>
      update((params) => {
        params.delete(P_FROM);
        params.delete(P_TO);
        params.delete(P_MONTH);
        params.set(P_PRESET, "year");
        params.set(P_YEAR, String(next));
      }),
    [update],
  );

  const setFromDate = useCallback(
    (value: string) =>
      update((params) => {
        if (value) params.set(P_FROM, value);
        else params.delete(P_FROM);
      }),
    [update],
  );

  const setToDate = useCallback(
    (value: string) =>
      update((params) => {
        if (value) params.set(P_TO, value);
        else params.delete(P_TO);
      }),
    [update],
  );

  const setView = useCallback(
    (next: DetailView) =>
      update((params) => {
        if (next === "overall") params.set(P_VIEW, VIEW_ALL_TIME);
        else params.delete(P_VIEW);
      }),
    [update],
  );

  return {
    preset,
    month,
    year,
    fromDate,
    toDate,
    customActive,
    rangeError,
    query,
    view,
    setView,
    selectPreset,
    selectMonth,
    selectYear,
    setFromDate,
    setToDate,
  };
}