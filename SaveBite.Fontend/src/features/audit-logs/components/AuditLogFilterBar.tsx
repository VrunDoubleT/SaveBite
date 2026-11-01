import type { AuditLogFilterOptions } from "../types/auditLog.types";
import { SearchInput } from "@/shared/components/modal/SearchInput";
import { formatAuditAction } from "../utils/formatAuditLog";
import { todayInVietnam } from "@/features/store-analytics/utils/formatAnalytics";

interface AuditLogFilterBarProps {
  search: string;
  onSearchChange: (value: string) => void;
  actorType: "" | "User" | "System";
  onActorTypeChange: (value: "" | "User" | "System") => void;
  targetType: string;
  onTargetTypeChange: (value: string) => void;
  action: string;
  onActionChange: (value: string) => void;
  fromDate: string;
  onFromDateChange: (value: string) => void;
  toDate: string;
  onToDateChange: (value: string) => void;
  filterOptions: AuditLogFilterOptions;
  hasActiveFilters: boolean;
  onReset: () => void;
}

const CONTROL =
  "rounded-md border border-neutral-200 bg-white px-3 py-2 text-sm text-neutral-700 transition-colors focus-visible:border-primary-400 focus-visible:outline-none focus-visible:shadow-focus";

export function AuditLogFilterBar({
  search,
  onSearchChange,
  actorType,
  onActorTypeChange,
  targetType,
  onTargetTypeChange,
  action,
  onActionChange,
  fromDate,
  onFromDateChange,
  toDate,
  onToDateChange,
  filterOptions,
  hasActiveFilters,
  onReset,
}: AuditLogFilterBarProps) {
  const today = todayInVietnam();
  const rangeInvalid = Boolean(fromDate && toDate && fromDate > toDate);

  return (
    <div className="space-y-3 rounded-lg border border-neutral-200 bg-white p-4 shadow-sm">
      <div className="flex flex-wrap items-center gap-3">
        <SearchInput
          value={search}
          onChange={onSearchChange}
          placeholder="Search by person, action, record ID or reason"
          className="w-full lg:max-w-md lg:flex-1"
        />
        {hasActiveFilters && (
          <button
            type="button"
            onClick={onReset}
            className="rounded-md px-2.5 py-2 text-sm font-medium text-primary-700 transition-colors hover:bg-primary-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500"
          >
            Clear filters
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <select
          value={actorType}
          onChange={(e) => onActorTypeChange(e.target.value as "" | "User" | "System")}
          aria-label="Filter by who acted"
          className={CONTROL}
        >
          <option value="">Anyone</option>
          <option value="User">Administrators</option>
          <option value="System">System</option>
        </select>

        <select
          value={targetType}
          onChange={(e) => onTargetTypeChange(e.target.value)}
          aria-label="Filter by record type"
          className={CONTROL}
        >
          <option value="">All record types</option>
          {filterOptions.targetTypes.map((t) => (
            <option key={t} value={t}>
              {t}
            </option>
          ))}
        </select>

        <select
          value={action}
          onChange={(e) => onActionChange(e.target.value)}
          aria-label="Filter by action"
          className={CONTROL}
        >
          <option value="">All actions</option>
          {filterOptions.actions.map((a) => (
            <option key={a} value={a}>
              {formatAuditAction(a)}
            </option>
          ))}
        </select>

        <div
          className={`flex items-center gap-2 rounded-md border bg-white px-2.5 ${
            rangeInvalid ? "border-danger" : "border-neutral-200"
          }`}
        >
          <input
            type="date"
            value={fromDate}
            max={toDate || today}
            onChange={(e) => onFromDateChange(e.target.value)}
            aria-label="From date"
            aria-invalid={rangeInvalid}
            className="w-full min-w-0 bg-transparent py-2 text-sm text-neutral-700 focus-visible:outline-none"
          />
          <span className="text-sm text-neutral-400" aria-hidden="true">
            to
          </span>
          <input
            type="date"
            value={toDate}
            min={fromDate || undefined}
            max={today}
            onChange={(e) => onToDateChange(e.target.value)}
            aria-label="To date"
            aria-invalid={rangeInvalid}
            className="w-full min-w-0 bg-transparent py-2 text-sm text-neutral-700 focus-visible:outline-none"
          />
        </div>
      </div>

      {rangeInvalid && (
        <p role="alert" className="text-sm text-danger">
          The start date must be on or before the end date.
        </p>
      )}
    </div>
  );
}
