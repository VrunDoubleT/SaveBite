import { TableSkeletonRows } from "@/shared/components/ui/LoadingSkeleton";
import { InlineSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { useEffect, useState } from "react";
import { CalendarDays, CircleDollarSign, ShoppingBag } from "lucide-react";
import { platformFeeApi } from "@/features/platform-fees/api/platformFeeApi";
import type { ShopMonthlyPlatformFeeReport as MonthlyFeeReport } from "@/features/platform-fees/types/platformFee.types";
import { Pagination } from "@/shared/components/pagination";
import { getApiErrorMessage } from "@/shared/api/httpClient";

interface ShopMonthlyPlatformFeeReportProps {
  shopId: string;
}

const PAGE_SIZE = 10;
const currentYear = new Date().getUTCFullYear();
const currentMonth = new Date().getUTCMonth() + 1;
const yearOptions = Array.from({ length: 6 }, (_, index) => currentYear - index);

function formatVnd(amount: number) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(amount);
}

export function ShopMonthlyPlatformFeeReport({ shopId }: ShopMonthlyPlatformFeeReportProps) {
  const [year, setYear] = useState(currentYear);
  const [month, setMonth] = useState(currentMonth);
  const [page, setPage] = useState(1);
  const [report, setReport] = useState<MonthlyFeeReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [refreshVersion, setRefreshVersion] = useState(0);
  const [currentRate, setCurrentRate] = useState<number | null>(null);
  const [rateEffectiveFrom, setRateEffectiveFrom] = useState<string | null>(null);
  const [rateLoading, setRateLoading] = useState(true);
  const [rateError, setRateError] = useState<string | null>(null);
  const [rateRefreshVersion, setRateRefreshVersion] = useState(0);

  useEffect(() => {
    let active = true;
    void platformFeeApi
      .getShopCurrentRate(shopId)
      .then((result) => {
        if (!active) return;
        setCurrentRate(result.feePercentage);
        setRateEffectiveFrom(result.effectiveFrom);
        setRateError(null);
      })
      .catch((error: unknown) => {
        if (!active) return;
        setCurrentRate(null);
        setRateEffectiveFrom(null);
        setRateError(getApiErrorMessage(error) || "Unable to load the current commission rate.");
      })
      .finally(() => {
        if (active) setRateLoading(false);
      });

    return () => {
      active = false;
    };
  }, [shopId, rateRefreshVersion]);

  useEffect(() => {
    let active = true;
    void platformFeeApi
      .getShopMonthlyReport(shopId, {
        year,
        month,
        pageNumber: page,
        pageSize: PAGE_SIZE,
      })
      .then((result) => {
        if (!active) return;
        setReport(result);
        setErrorMessage(null);
      })
      .catch((error: unknown) => {
        if (!active) return;
        setReport(null);
        setErrorMessage(getApiErrorMessage(error) || "Unable to load the platform fee report.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [shopId, year, month, page, refreshVersion]);

  const reloadReport = () => {
    setLoading(true);
    setErrorMessage(null);
    setRefreshVersion((version) => version + 1);
  };

  const monthLabel = new Intl.DateTimeFormat("en-US", { month: "long" }).format(
    new Date(year, month - 1, 1),
  );
  const orderPage = report?.orders;

  return (
    <div className="space-y-4">
      <section
        aria-labelledby="current-platform-fee-rate-heading"
        className="rounded-xl border border-emerald-200 bg-emerald-50/60 p-4 shadow-2xs sm:p-5"
      >
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2
              id="current-platform-fee-rate-heading"
              className="text-base font-bold text-emerald-950"
            >
              Current Platform Fee Rate
            </h2>
            <p className="mt-1 text-sm text-emerald-900">
              Commission rate currently applied to completed orders.
            </p>
            {!rateLoading && !rateError && rateEffectiveFrom && (
              <p className="mt-1 text-xs text-emerald-800">
                Effective since {new Date(rateEffectiveFrom).toLocaleDateString("en-US")}
              </p>
            )}
          </div>
          <div
            aria-live="polite"
            className="text-2xl font-bold tabular-nums text-emerald-900 sm:text-3xl"
          >
            {rateLoading ? (
              <InlineSkeleton label="Loading rate" className="w-24" />
            ) : rateError ? (
              "Unavailable"
            ) : currentRate === null ? (
              "No active rate"
            ) : (
              `${new Intl.NumberFormat("en-US", { maximumFractionDigits: 4 }).format(currentRate)}%`
            )}
          </div>
        </div>
        {rateError && (
          <div className="mt-3 flex flex-wrap items-center justify-between gap-2 text-sm text-rose-800">
            <p role="alert">{rateError}</p>
            <button
              type="button"
              onClick={() => {
                setRateLoading(true);
                setRateError(null);
                setRateRefreshVersion((version) => version + 1);
              }}
              className="min-h-10 rounded-lg border border-rose-300 px-3 font-semibold hover:bg-rose-50"
            >
              Retry
            </button>
          </div>
        )}
      </section>

      <section
        aria-labelledby="monthly-platform-fees-heading"
        className="overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-2xs"
      >
        <div className="flex flex-col gap-4 border-b border-neutral-100 p-4 sm:flex-row sm:items-center sm:justify-between sm:p-5">
          <div>
            <h2 id="monthly-platform-fees-heading" className="text-base font-bold text-neutral-900">
              Monthly Platform Fees
            </h2>
            <p className="mt-1 text-sm text-neutral-500">
              Review your total commission fees and the fee breakdown for each order.
            </p>
          </div>
          <div className="flex flex-wrap items-end gap-3">
            <label className="flex flex-col gap-1 text-xs font-semibold text-neutral-600">
              Month
              <select
                value={month}
                onChange={(event) => {
                  setLoading(true);
                  setErrorMessage(null);
                  setMonth(Number(event.target.value));
                  setPage(1);
                }}
                className="min-h-10 rounded-lg border border-neutral-200 bg-white px-3 text-sm text-neutral-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
              >
                {Array.from({ length: 12 }, (_, index) => index + 1).map((value) => (
                  <option key={value} value={value}>
                    {new Intl.DateTimeFormat("en-US", { month: "long" }).format(
                      new Date(2000, value - 1, 1),
                    )}
                  </option>
                ))}
              </select>
            </label>
            <label className="flex flex-col gap-1 text-xs font-semibold text-neutral-600">
              Year
              <select
                value={year}
                onChange={(event) => {
                  setLoading(true);
                  setErrorMessage(null);
                  setYear(Number(event.target.value));
                  setPage(1);
                }}
                className="min-h-10 rounded-lg border border-neutral-200 bg-white px-3 text-sm text-neutral-800 focus:outline-none focus:ring-2 focus:ring-emerald-500"
              >
                {yearOptions.map((value) => (
                  <option key={value} value={value}>
                    {value}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>

        <div className="space-y-4 p-4 sm:p-5">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <div className="rounded-lg border border-emerald-100 bg-emerald-50/60 p-4">
              <div className="flex items-center gap-2 text-sm font-medium text-emerald-900">
                <CircleDollarSign aria-hidden="true" className="size-4" />
                Total platform fees
              </div>
              <p className="mt-2 text-xl font-bold tabular-nums text-emerald-800">
                {loading ? (
                  <InlineSkeleton label="Loading rate" className="w-24" />
                ) : (
                  formatVnd(report?.totalFeeAmount ?? 0)
                )}
              </p>
              <p className="mt-1 text-xs text-emerald-800">
                Accrued in {monthLabel} {year}
              </p>
            </div>
            <div className="rounded-lg border border-amber-100 bg-amber-50/60 p-4">
              <div className="flex items-center gap-2 text-sm font-medium text-amber-950">
                <CalendarDays aria-hidden="true" className="size-4" />
                Not yet invoiced
              </div>
              <p className="mt-2 text-xl font-bold tabular-nums text-amber-900">
                {loading ? (
                  <InlineSkeleton label="Loading rate" className="w-24" />
                ) : (
                  formatVnd(report?.unbilledFeeAmount ?? 0)
                )}
              </p>
              <p className="mt-1 text-xs text-amber-900">
                Fees from orders not yet included in a statement
              </p>
            </div>
            <div className="rounded-lg border border-neutral-200 bg-neutral-50 p-4">
              <div className="flex items-center gap-2 text-sm font-medium text-neutral-700">
                <ShoppingBag aria-hidden="true" className="size-4" />
                Orders with fees
              </div>
              <p className="mt-2 text-xl font-bold tabular-nums text-neutral-900">
                {loading ? (
                  <InlineSkeleton label="Loading order count" className="w-16" />
                ) : (
                  (report?.orderCount ?? 0).toLocaleString("en-US")
                )}
              </p>
              <p className="mt-1 text-xs text-neutral-600">
                Completed orders, including refunded orders
              </p>
            </div>
            <div className="rounded-lg border border-neutral-200 bg-neutral-50 p-4">
              <div className="text-sm font-medium text-neutral-700">Order gross sales</div>
              <p className="mt-2 text-xl font-bold tabular-nums text-neutral-900">
                {loading ? (
                  <InlineSkeleton label="Loading rate" className="w-24" />
                ) : (
                  formatVnd(report?.totalGrossAmount ?? 0)
                )}
              </p>
              <p className="mt-1 text-xs text-neutral-600">Original order sales for this month</p>
            </div>
          </div>

          <p className="text-xs leading-relaxed text-neutral-500">
            Total fees reflect completed refund credits. Order fee amounts show the remaining
            commission after any transferred refunds.
          </p>

          {errorMessage ? (
            <div
              role="alert"
              className="flex flex-col gap-3 rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800 sm:flex-row sm:items-center sm:justify-between"
            >
              <span>{errorMessage}</span>
              <button
                type="button"
                onClick={reloadReport}
                className="min-h-10 rounded-lg border border-rose-300 px-3 font-semibold hover:bg-rose-100"
              >
                Retry
              </button>
            </div>
          ) : (
            <div className="overflow-hidden rounded-lg border border-neutral-200">
              <div className="overflow-x-auto">
                <table className="w-full min-w-[760px] text-left text-sm">
                  <thead className="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-neutral-600">
                    <tr>
                      <th scope="col" className="px-4 py-3">
                        Order number
                      </th>
                      <th scope="col" className="px-4 py-3">
                        Sales subject to fee
                      </th>
                      <th scope="col" className="px-4 py-3">
                        Fee rate
                      </th>
                      <th scope="col" className="px-4 py-3">
                        Net order fee
                      </th>
                      <th scope="col" className="px-4 py-3">
                        Calculated at
                      </th>
                      <th scope="col" className="px-4 py-3">
                        Statement
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-neutral-100">
                    {loading ? (
                      <TableSkeletonRows columns={6} label="Loading order fee details" />
                    ) : orderPage?.items.length ? (
                      orderPage.items.map((order) => (
                        <tr key={order.id} className="text-neutral-700 hover:bg-neutral-50/70">
                          <td className="px-4 py-3 font-mono font-semibold text-neutral-900">
                            {order.orderCode}
                          </td>
                          <td className="px-4 py-3 tabular-nums">{formatVnd(order.grossAmount)}</td>
                          <td className="px-4 py-3 tabular-nums">
                            {new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 3 }).format(
                              order.feeRate * 100,
                            )}
                            %
                          </td>
                          <td className="px-4 py-3 font-semibold tabular-nums text-emerald-800">
                            <span>{formatVnd(order.feeAmount)}</span>
                            {order.refundAdjustmentAmount < 0 && (
                              <span className="mt-1 block text-xs font-normal text-amber-800">
                                Refund credit: {formatVnd(Math.abs(order.refundAdjustmentAmount))}{" "}
                                (original {formatVnd(order.originalFeeAmount)})
                              </span>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            {new Date(order.calculatedAt).toLocaleString("vi-VN")}
                          </td>
                          <td className="px-4 py-3">
                            {order.statementId ? (
                              <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-semibold text-emerald-800">
                                Invoiced
                              </span>
                            ) : (
                              <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-900">
                                Not invoiced
                              </span>
                            )}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={6} className="px-4 py-10 text-center text-neutral-500">
                          No orders with platform fees in {monthLabel} {year}.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
              {orderPage && orderPage.totalPages > 1 && (
                <div className="border-t border-neutral-100 bg-neutral-50/50 p-3">
                  <Pagination
                    currentPage={page}
                    totalPages={orderPage.totalPages}
                    totalItems={orderPage.totalItems ?? 0}
                    pageSize={PAGE_SIZE}
                    onPageChange={(nextPage) => {
                      setLoading(true);
                      setErrorMessage(null);
                      setPage(nextPage);
                    }}
                    itemLabel="orders"
                  />
                </div>
              )}
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
