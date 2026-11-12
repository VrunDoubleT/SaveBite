import { TableSkeletonRows } from "@/shared/components/ui/LoadingSkeleton";
import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { PlatformFeeStatisticsModal } from "@/features/platform-fees/components/PlatformFeeStatisticsModal";
import { useEffect, useState, useCallback } from "react";
import {
  ChartNoAxesCombined,
  CheckCircle2,
  Clock3,
  AlertTriangle,
  CircleCheck,
  Receipt,
  Search,
  Store,
  Eye,
} from "lucide-react";
import { platformFeeApi } from "@/features/platform-fees/api/platformFeeApi";
import type {
  PlatformFeeStatementSummary,
  PlatformFeePeriodStatistics,
} from "@/features/platform-fees/types/platformFee.types";
import { StatusBadge } from "@/features/platform-fees/components/StatusBadge";
import { Pagination } from "@/shared/components/pagination";
import { RecordId } from "@/shared/components/RecordId";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

const formatVnd = (amount: number) => {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(amount);
};

const formatDateTime = (dateStr?: string | null) => {
  if (!dateStr) return "N/A";

  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return dateStr;

  return d.toLocaleString("en-US", {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
};

export function PlatformFeeManagementPage() {
  const [statisticsOpen, setStatisticsOpen] = useState(false);

  const [statements, setStatements] = useState<PlatformFeeStatementSummary[]>([]);

  const [loadingStatements, setLoadingStatements] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");

  const [defaultBillingPeriod] = useState(() => {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth() - 1, 1);
  });

  const [filterYear, setFilterYear] = useState<number | string>(defaultBillingPeriod.getFullYear());

  const [filterMonth, setFilterMonth] = useState<string>(() =>
    String(defaultBillingPeriod.getMonth() + 1),
  );

  const [filterStatus, setFilterStatus] = useState<string>("All");

  const [page, setPage] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);

  const summaryKey = `${filterYear}:${filterMonth}`;

  const [summaryResult, setSummaryResult] = useState<{
    key: string;
    data?: PlatformFeePeriodStatistics;
    error?: boolean;
  } | null>(null);

  const summary = summaryResult?.key === summaryKey ? summaryResult.data : undefined;

  const summaryError = summaryResult?.key === summaryKey && summaryResult.error;

  // Load statistics
  useEffect(() => {
    const controller = new AbortController();

    void platformFeeApi
      .getPeriodStatistics(
        filterYear === "All" ? undefined : Number(filterYear),
        filterMonth === "All" ? undefined : Number(filterMonth),
        controller.signal,
      )
      .then((data) => {
        if (!controller.signal.aborted) {
          setSummaryResult({ key: summaryKey, data });
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setSummaryResult({ key: summaryKey, error: true });
        }
      });

    return () => controller.abort();
  }, [filterYear, filterMonth, summaryKey]);

  // Load statements
  const loadStatements = useCallback(async () => {
    setLoadingStatements(true);

    try {
      const result = await platformFeeApi.getAdminStatements({
        year: filterYear === "All" ? undefined : Number(filterYear),
        month: filterMonth === "All" ? undefined : parseInt(filterMonth, 10),
        status: filterStatus === "All" ? undefined : filterStatus,
        search: searchTerm.trim() || undefined,
        page,
        pageSize: 10,
      });

      setStatements(result.items || []);
      setTotalPages(result.totalPages || 1);
      setTotalCount(result.totalItems ?? result.totalCount ?? (result.items?.length || 0));
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to load platform fee statements.");
    } finally {
      setLoadingStatements(false);
    }
  }, [filterYear, filterMonth, filterStatus, searchTerm, page]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadStatements();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadStatements]);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">
            Shop Fee Statements
          </h1>
          <p className="mt-1.5 text-sm text-text-secondary">
            Review monthly shop fees, payments and overdue balances.
          </p>
        </div>
      </div>

      {/* Summary metrics */}
      <section
        aria-label="Statement summary metrics"
        className="grid grid-cols-2 gap-4 lg:grid-cols-5"
      >
        {[
          {
            label: "All Statements",
            value: summary?.totalStatements,
            icon: Receipt,
            tone: "bg-neutral-100 text-neutral-600",
          },
          {
            label: "Paid",
            value: summary?.paidStatements,
            icon: CheckCircle2,
            tone: "bg-emerald-50 text-emerald-600",
          },
          {
            label: "Unpaid",
            value: summary?.unpaidStatements,
            icon: Clock3,
            tone: "bg-amber-50 text-amber-600",
          },
          {
            label: "Overdue",
            value: summary?.overdueStatements,
            icon: AlertTriangle,
            tone: "bg-red-50 text-red-600",
          },
          {
            label: "No Payment Required",
            value: summary?.zeroFeeStatements,
            icon: CircleCheck,
            tone: "bg-slate-100 text-slate-600",
          },
        ].map(({ label, value, icon: Icon, tone }) => (
          <div key={label} className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
            <div className="flex items-center gap-3">
              <span
                className={`flex size-10 shrink-0 items-center justify-center rounded-lg ${tone}`}
              >
                <Icon aria-hidden="true" className="size-5" />
              </span>

              <div className="min-w-0">
                <p className="text-xs font-medium text-slate-500">{label}</p>
                <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                  {summaryError ? "—" : (value ?? "–")}
                </p>
              </div>
            </div>
          </div>
        ))}
      </section>

      {summaryError && (
        <p role="status" className="text-sm text-red-700">
          Unable to load statement statistics.
        </p>
      )}

      {/* Statements section */}
      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xs">
        {/* Section header and filters */}
        <div className="flex flex-col gap-3 border-b border-slate-200 px-4 py-4 xl:flex-row xl:items-center">
          <div className="shrink-0">
            <div className="flex items-center gap-2">
              <span className="flex size-9 items-center justify-center rounded-lg bg-emerald-50 text-emerald-700">
                <Store aria-hidden="true" className="size-4.5" />
              </span>

              <div>
                <h2 className="text-base font-bold text-slate-900">Shop fee statements</h2>
                <p className="mt-0.5 text-xs text-slate-500">
                  {totalCount.toLocaleString("en-US")} statements
                </p>
              </div>
            </div>
          </div>

          {/* Filters */}
          <div className="grid grid-cols-2 gap-2 sm:flex sm:flex-wrap sm:items-center xl:ml-auto xl:flex-nowrap">
            {/* Search */}
            <div className="relative col-span-2 sm:col-span-1">
              <Search
                aria-hidden="true"
                className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400"
              />

              <input
                type="text"
                placeholder="Search shop or ID..."
                value={searchTerm}
                onChange={(e) => {
                  setSearchTerm(e.target.value);
                  setPage(1);
                }}
                className="min-h-10 w-full rounded-lg border border-slate-200 bg-white pl-9 pr-3 text-sm text-slate-700 placeholder-slate-400 focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 sm:w-52"
              />
            </div>

            {/* Year */}
            <select
              value={filterYear}
              onChange={(e) => {
                setFilterYear(e.target.value === "All" ? "All" : parseInt(e.target.value, 10));
                setPage(1);
              }}
              className="min-h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm font-medium text-slate-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600"
            >
              <option value="All">All Years</option>
              {[2024, 2025, 2026, 2027].map((y) => (
                <option key={y} value={y}>
                  Year {y}
                </option>
              ))}
            </select>

            {/* Month */}
            <select
              value={filterMonth}
              onChange={(e) => {
                setFilterMonth(e.target.value);
                setPage(1);
              }}
              className="min-h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm font-medium text-slate-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600"
            >
              <option value="All">All Months</option>
              {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
                <option key={m} value={m}>
                  Month {m}
                </option>
              ))}
            </select>

            {/* Status */}
            <select
              value={filterStatus}
              onChange={(e) => {
                setFilterStatus(e.target.value);
                setPage(1);
              }}
              className="min-h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm font-medium text-slate-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600"
            >
              <option value="All">All Statuses</option>
              <option value="Issued">Issued</option>
              <option value="Paid">Paid</option>
              <option value="Overdue">Overdue</option>
            </select>
          </div>

          {/* Statistics button */}
          <button
            type="button"
            onClick={() => setStatisticsOpen(true)}
            className="inline-flex min-h-11 shrink-0 items-center justify-center gap-2 rounded-lg border border-emerald-200 bg-emerald-50 px-4 text-sm font-semibold text-emerald-800 transition-colors hover:border-emerald-300 hover:bg-emerald-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-600"
          >
            <ChartNoAxesCombined aria-hidden="true" className="size-4" />
            Statistics
          </button>
        </div>

        {/* Table content */}
        <div>
          {/* Desktop table */}
          <div className="hidden xl:block">
            <table className="w-full table-fixed text-left text-sm">
              <colgroup>
                <col className="w-[22%]" />
                <col className="w-[12%]" />
                <col className="w-[16%]" />
                <col className="w-[11%]" />
                <col className="w-[15%]" />
                <col className="w-[15%]" />
                <col className="w-[9%]" />
              </colgroup>

              <thead className="border-b border-border-default bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-text-secondary">
                <tr>
                  <th className="px-3 py-3">Shop</th>
                  <th className="px-3 py-3">Billing period</th>
                  <th className="px-3 py-3">Fee amount</th>
                  <th className="px-3 py-3">Status</th>
                  <th className="px-3 py-3">Due date</th>
                  <th className="px-3 py-3">Paid date</th>
                  <th className="px-3 py-3 text-right">Actions</th>
                </tr>
              </thead>

              <tbody className="divide-y divide-border-default text-sm text-text-primary">
                {loadingStatements ? (
                  <TableSkeletonRows columns={7} />
                ) : statements.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="py-10 text-center text-sm text-slate-500">
                      <Receipt aria-hidden="true" className="mx-auto mb-2 size-7 text-slate-300" />
                      No fee statements found for the selected criteria.
                    </td>
                  </tr>
                ) : (
                  statements.map((stmt) => (
                    <tr key={stmt.id} className="transition-colors hover:bg-slate-50/70">
                      {/* Shop */}
                      <td className="px-3 py-3 font-semibold text-slate-900">
                        <div className="flex min-w-0 items-center gap-2.5">
                          {stmt.shopLogoUrl ? (
                            <img
                              src={stmt.shopLogoUrl}
                              alt={stmt.shopName || "Shop"}
                              className="size-8 shrink-0 rounded-lg border border-slate-200 object-cover"
                            />
                          ) : (
                            <div className="flex size-8 shrink-0 items-center justify-center rounded-lg border border-emerald-100 bg-emerald-50 text-emerald-700">
                              <Store aria-hidden="true" className="size-4" />
                            </div>
                          )}

                          <div className="min-w-0">
                            <div className="truncate" title={stmt.shopName || "Shop"}>
                              {stmt.shopName || "Shop"}
                            </div>
                            <div className="mt-1">
                              <RecordId id={stmt.id} />
                            </div>
                          </div>
                        </div>
                      </td>

                      {/* Billing period */}
                      <td className="px-3 py-3 font-medium text-slate-700">
                        Month {stmt.billingMonth}/{stmt.billingYear}
                      </td>

                      {/* Amount */}
                      <td className="px-3 py-3 font-semibold tabular-nums text-slate-900">
                        {formatVnd(stmt.totalFeeAmount)}
                      </td>

                      {/* Status */}
                      <td className="px-3 py-3">
                        <StatusBadge status={stmt.status} />
                      </td>

                      {/* Due date */}
                      <td className="px-3 py-3 text-xs text-slate-600">
                        {stmt.dueAt ? formatDateTime(stmt.dueAt) : "—"}
                      </td>

                      {/* Paid date */}
                      <td className="px-3 py-3 text-xs text-slate-600">
                        {stmt.paidAt ? formatDateTime(stmt.paidAt) : "—"}
                      </td>

                      {/* Actions - merged */}
                      <td className="px-3 py-3 text-right">
                        <div className="inline-flex items-center justify-end gap-1.5">
                          <Link
                            to={`${APP_PATHS.ADMIN_PLATFORM_FEE}/${stmt.id}`}
                            title="View statement details"
                            aria-label="View statement details"
                            className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600"
                          >
                            <Eye aria-hidden="true" className="size-3.5 text-slate-500" />
                            Details
                          </Link>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Mobile / Tablet */}
          <ul className="divide-y divide-border-default xl:hidden">
            {loadingStatements ? (
              <li className="p-4">
                <LoadingSkeleton variant="list" label="Loading statements" />
              </li>
            ) : statements.length === 0 ? (
              <li className="px-4 py-10 text-center text-sm text-slate-500">
                <Receipt aria-hidden="true" className="mx-auto mb-2 size-7 text-slate-300" />
                No fee statements found for the selected criteria.
              </li>
            ) : (
              statements.map((stmt) => (
                <li key={stmt.id} className="space-y-4 p-4">
                  {/* Shop info */}
                  <div className="flex min-w-0 items-center gap-3">
                    {stmt.shopLogoUrl ? (
                      <img
                        src={stmt.shopLogoUrl}
                        alt={stmt.shopName || "Shop"}
                        className="size-10 shrink-0 rounded-lg border border-slate-200 object-cover"
                      />
                    ) : (
                      <div className="flex size-10 shrink-0 items-center justify-center rounded-lg border border-emerald-100 bg-emerald-50 text-emerald-700">
                        <Store aria-hidden="true" className="size-4" />
                      </div>
                    )}

                    <div className="min-w-0 flex-1">
                      <p
                        className="truncate font-semibold text-slate-900"
                        title={stmt.shopName || "Shop"}
                      >
                        {stmt.shopName || "Shop"}
                      </p>
                      <RecordId id={stmt.id} />
                    </div>

                    <StatusBadge status={stmt.status} />
                  </div>

                  {/* Statement details */}
                  <dl className="grid grid-cols-2 gap-x-4 gap-y-3 border-t border-slate-100 pt-3 text-sm">
                    <div className="min-w-0">
                      <dt className="text-xs font-medium text-slate-500">Billing period</dt>
                      <dd className="mt-0.5 font-medium text-slate-700">
                        Month {stmt.billingMonth}/{stmt.billingYear}
                      </dd>
                    </div>

                    <div className="min-w-0">
                      <dt className="text-xs font-medium text-slate-500">Fee amount</dt>
                      <dd className="mt-0.5 break-words font-semibold tabular-nums text-slate-900">
                        {formatVnd(stmt.totalFeeAmount)}
                      </dd>
                    </div>

                    <div className="min-w-0">
                      <dt className="text-xs font-medium text-slate-500">Due date</dt>
                      <dd className="mt-0.5 text-xs text-slate-600">
                        {stmt.dueAt ? formatDateTime(stmt.dueAt) : "—"}
                      </dd>
                    </div>

                    <div className="min-w-0">
                      <dt className="text-xs font-medium text-slate-500">Paid date</dt>
                      <dd className="mt-0.5 text-xs text-slate-600">
                        {stmt.paidAt ? formatDateTime(stmt.paidAt) : "—"}
                      </dd>
                    </div>
                  </dl>

                  {/* Mobile actions - merged */}
                  <div className="flex justify-end border-t border-slate-100 pt-3">
                    <Link
                      to={`${APP_PATHS.ADMIN_PLATFORM_FEE}/${stmt.id}`}
                      title="View statement details"
                      aria-label="View statement details"
                      className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600"
                    >
                      <Eye aria-hidden="true" className="size-3.5 text-slate-500" />
                      Details
                    </Link>
                  </div>
                </li>
              ))
            )}
          </ul>

          {/* Pagination */}
          {totalCount > 0 && (
            <div className="border-t border-slate-100 bg-slate-50/50 px-4 py-3">
              <Pagination
                currentPage={page}
                totalPages={totalPages}
                totalItems={totalCount}
                pageSize={10}
                onPageChange={(p) => setPage(p)}
                itemLabel="statements"
              />
            </div>
          )}
        </div>
      </section>

      {/* Statistics modal */}
      {statisticsOpen && <PlatformFeeStatisticsModal onClose={() => setStatisticsOpen(false)} />}
    </div>
  );
}
