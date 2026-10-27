import { RefreshCw, X } from "lucide-react";
import type { AnalyticsPreset } from "../types/storeAnalytics.types";
import { MAX_CUSTOM_RANGE_DAYS } from "../hooks/useAnalyticsPeriod";
import { currentYearInVietnam, todayInVietnam } from "../utils/formatAnalytics";

const PRESETS: { value: AnalyticsPreset; label: string }[] = [
  { value: "today", label: "Today" },
  { value: "yesterday", label: "Yesterday" },
  { value: "7d", label: "7 days" },
  { value: "month", label: "Month" },
  { value: "year", label: "Year" },
];

const FOCUS =
  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500";
const FIELD = `rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm text-neutral-700 shadow-sm ${FOCUS}`;

function shiftDate(iso: string, days: number): string {
  const d = new Date(`${iso}T00:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}

interface AnalyticsPeriodFilterProps {
  preset: AnalyticsPreset;
  onSelectPreset: (preset: AnalyticsPreset) => void;
  month: string;
  onMonthChange: (value: string) => void;
  year: number;
  onYearChange: (value: number) => void;
  fromDate: string;
  onFromDateChange: (val: string) => void;
  toDate: string;
  onToDateChange: (val: string) => void;
  rangeError: string | null;
  loading: boolean;
  onRefresh: () => void;
}

export function AnalyticsPeriodFilter({
  preset,
  onSelectPreset,
  month,
  onMonthChange,
  year,
  onYearChange,
  fromDate,
  onFromDateChange,
  toDate,
  onToDateChange,
  rangeError,
  loading,
  onRefresh,
}: AnalyticsPeriodFilterProps) {
  const isCustomRange = Boolean(fromDate || toDate);
  const today = todayInVietnam();
  const thisYear = currentYearInVietnam();
  const years = Array.from({ length: 6 }, (_, i) => thisYear - i);

  // The two dates may be at most one month apart: each picker is limited by the other.
  const latestTo = fromDate ? [today, shiftDate(fromDate, MAX_CUSTOM_RANGE_DAYS - 1)].sort()[0] : today;
  const earliestFrom = toDate ? shiftDate(toDate, -(MAX_CUSTOM_RANGE_DAYS - 1)) : undefined;

  return (
    <div className="flex flex-col gap-3 lg:items-end">
      <div className="flex flex-wrap items-center gap-2 lg:justify-end">
        <div
          role="group"
          aria-label="Quick period"
          className="inline-flex rounded-lg border border-neutral-200 bg-white p-1 shadow-sm"
        >
          {PRESETS.map((p) => {
            const active = !isCustomRange && preset === p.value;
            return (
              <button
                key={p.value}
                type="button"
                aria-pressed={active}
                onClick={() => onSelectPreset(p.value)}
                className={`rounded-md px-3.5 py-1.5 text-sm font-medium transition-colors ${FOCUS} ${
                  active
                    ? "bg-primary-600 text-white shadow-sm"
                    : "text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900"
                }`}
              >
                {p.label}
              </button>
            );
          })}
        </div>

        {!isCustomRange && preset === "month" && (
          <input
            type="month"
            value={month}
            max={today.slice(0, 7)}
            onChange={(e) => onMonthChange(e.target.value)}
            aria-label="Choose month"
            className={FIELD}
          />
        )}
        {!isCustomRange && preset === "year" && (
          <select
            value={year}
            onChange={(e) => onYearChange(Number(e.target.value))}
            aria-label="Choose year"
            className={FIELD}
          >
            {years.map((y) => (
              <option key={y} value={y}>
                {y}
              </option>
            ))}
          </select>
        )}

        <button
          type="button"
          onClick={onRefresh}
          disabled={loading || Boolean(rangeError)}
          className={`inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-3.5 py-2.5 text-sm font-medium text-neutral-700 shadow-sm transition-colors hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-50 ${FOCUS}`}
        >
          <RefreshCw className={`size-4 ${loading ? "animate-spin motion-reduce:animate-none" : ""}`} />
          Refresh
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-2 lg:justify-end">
        <span className="text-sm text-neutral-500">Or pick two dates (up to {MAX_CUSTOM_RANGE_DAYS} days)</span>
        <div
          className={`flex items-center gap-1.5 rounded-lg border bg-white px-3 py-1.5 shadow-sm ${
            rangeError ? "border-danger" : isCustomRange ? "border-primary-500" : "border-neutral-200"
          }`}
        >
          <input
            type="date"
            value={fromDate}
            min={earliestFrom}
            max={toDate || today}
            onChange={(e) => onFromDateChange(e.target.value)}
            aria-label="From date"
            aria-invalid={Boolean(rangeError)}
            className={`bg-transparent py-1 text-sm text-neutral-700 ${FOCUS}`}
          />
          <span className="text-neutral-400" aria-hidden="true">
            to
          </span>
          <input
            type="date"
            value={toDate}
            min={fromDate || undefined}
            max={latestTo}
            onChange={(e) => onToDateChange(e.target.value)}
            aria-label="To date"
            aria-invalid={Boolean(rangeError)}
            className={`bg-transparent py-1 text-sm text-neutral-700 ${FOCUS}`}
          />
          {isCustomRange && (
            <button
              type="button"
              onClick={() => onSelectPreset(preset)}
              aria-label="Clear custom dates"
              className={`rounded p-1 text-neutral-400 transition-colors hover:bg-neutral-100 hover:text-neutral-700 ${FOCUS}`}
            >
              <X className="size-4" />
            </button>
          )}
        </div>
      </div>

      {rangeError && (
        <p role="alert" className="text-sm text-danger">
          {rangeError}
        </p>
      )}
    </div>
  );
}
