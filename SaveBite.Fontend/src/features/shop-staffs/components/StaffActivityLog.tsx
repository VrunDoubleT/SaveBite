import { Link } from "react-router-dom";
import { Clock, MessageSquare, Package, X } from "lucide-react";
import type { StaffActivityLog as StaffActivityLogType } from "@/features/shop-staffs/types/shopStaff.types";
import { Pagination } from "@/shared/components/pagination";

export const ACTIVITY_PAGE_SIZE = 10;

interface StaffActivityLogProps {
  logs: StaffActivityLogType[];
  /** True while a page is being fetched (the filter bar stays mounted). */
  loading?: boolean;
  /** Date filter (yyyy-MM-dd, "" = not set). */
  fromDate: string;
  toDate: string;
  onFromDateChange: (value: string) => void;
  onToDateChange: (value: string) => void;
  onClearFilters: () => void;
  /** Server-side pagination. */
  page: number;
  totalPages: number;
  totalItems: number;
  onPageChange: (page: number) => void;
}

// Local calendar date as yyyy-MM-dd, used to stop the user from picking a future day.
function todayInputValue() {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

const ACTION_LABELS: Record<string, string> = {
  FeedbackReplied: "Replied to customer review",
};

function OrderLink({ orderId, orderCode }: { orderId: string; orderCode?: string | null }) {
  const label = orderCode ? `#${orderCode}` : `#${orderId.slice(0, 8).toUpperCase()}`;

  return (
    <Link
      to={`/store-owner/orders/${orderId}`}
      title="View order details"
      className="mt-1.5 inline-flex max-w-full items-center gap-1.5 rounded-lg border border-blue-200 bg-blue-50/70 px-2.5 py-1 text-xs text-blue-900 transition hover:border-blue-300 hover:bg-blue-100"
    >
      <Package size={13} className="shrink-0 text-blue-600" />
      <span className="font-medium text-blue-700">Order</span>
      <span className="truncate font-mono font-semibold text-blue-950 underline-offset-2 hover:underline">
        {label}
      </span>
    </Link>
  );
}

export function StaffActivityLog({
  logs,
  loading = false,
  fromDate,
  toDate,
  onFromDateChange,
  onToDateChange,
  onClearFilters,
  page,
  totalPages,
  totalItems,
  onPageChange,
}: StaffActivityLogProps) {
  const today = todayInputValue();
  const hasFilter = Boolean(fromDate || toDate);
  const rangeInvalid = Boolean(fromDate && toDate && fromDate > toDate);

  return (
    <section className="rounded-xl border border-neutral-200 bg-white shadow-2xs">
      <div className="border-b border-gray-100 px-6 py-5">
        <div className="flex items-center gap-2">
          <Clock size={19} className="text-gray-500" />

          <h2 className="text-base font-semibold text-gray-900">Activity History</h2>
        </div>

        <p className="mt-1 text-sm text-gray-500">Recent activities of this staff member.</p>

        {/* Date filter */}
        <div className="mt-4 flex flex-wrap items-end gap-3">
          <label className="flex flex-col gap-1 text-xs font-medium text-gray-500">
            From date
            <input
              type="date"
              value={fromDate}
              max={toDate || today}
              onChange={(e) => onFromDateChange(e.target.value)}
              aria-invalid={rangeInvalid}
              className="rounded-md border border-neutral-200 bg-white px-2.5 py-2 text-sm text-neutral-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-500"
            />
          </label>

          <label className="flex flex-col gap-1 text-xs font-medium text-gray-500">
            To date
            <input
              type="date"
              value={toDate}
              min={fromDate || undefined}
              max={today}
              onChange={(e) => onToDateChange(e.target.value)}
              aria-invalid={rangeInvalid}
              className="rounded-md border border-neutral-200 bg-white px-2.5 py-2 text-sm text-neutral-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-500"
            />
          </label>

          {hasFilter && (
            <button
              type="button"
              onClick={onClearFilters}
              className="inline-flex items-center gap-1.5 rounded-md border border-neutral-200 px-3 py-2 text-sm font-medium text-gray-600 transition hover:bg-gray-50"
            >
              <X size={14} />
              Clear filter
            </button>
          )}
        </div>

        {rangeInvalid && (
          <p role="alert" className="mt-2 text-sm text-red-600">
            The start date must be on or before the end date.
          </p>
        )}
      </div>

      {loading ? (
        <div className="space-y-5 px-6 py-5" aria-busy="true">
          {[1, 2, 3].map((item) => (
            <div key={item} className="flex gap-4">
              <div className="h-9 w-9 animate-pulse rounded-full bg-gray-100 motion-reduce:animate-none" />

              <div className="flex-1 space-y-2">
                <div className="h-4 w-48 animate-pulse rounded bg-gray-100 motion-reduce:animate-none" />
                <div className="h-3 w-32 animate-pulse rounded bg-gray-100 motion-reduce:animate-none" />
              </div>
            </div>
          ))}
        </div>
      ) : logs.length === 0 ? (
        <div className="px-6 py-10 text-center">
          <Clock size={28} className="mx-auto text-gray-300" />

          <p className="mt-3 text-sm font-medium text-gray-700">
            {hasFilter ? "No activity in this period" : "No activity yet"}
          </p>

          <p className="mt-1 text-sm text-gray-400">
            {hasFilter
              ? "Try a different date range or clear the filter."
              : "There are no recorded activities for this staff member."}
          </p>
        </div>
      ) : (
        <div className="space-y-6 px-6 py-5">
          <div className="space-y-6">
            {logs.map((log) => (
              <div key={log.id} className="relative flex gap-4">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                  {log.action === "FeedbackReplied" ? <MessageSquare size={16} /> : <Clock size={16} />}
                </div>

                <div className="min-w-0 flex-1">
                  <p className="text-sm font-semibold text-gray-900">{ACTION_LABELS[log.action] ?? log.action}</p>
                  {log.actorName && (
                    <p className="mt-1 text-xs font-medium text-gray-600">
                      Performed by {log.actorRole === "StoreOwner" ? "Shop owner" : log.actorName}
                    </p>
                  )}

                  {/* Link to the order instead of showing a raw ID */}
                  {log.orderId && <OrderLink orderId={log.orderId} orderCode={log.orderCode} />}

                  {log.description && (
                    <p className="mt-1 text-sm text-gray-500">{log.description}</p>
                  )}

                  <p className="mt-2 text-xs text-gray-400">{formatDateTime(log.createdAt)}</p>
                </div>
              </div>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="border-t border-gray-100 pt-4">
              <Pagination
                currentPage={page}
                totalPages={totalPages}
                totalItems={totalItems}
                pageSize={ACTIVITY_PAGE_SIZE}
                itemLabel="activities"
                onPageChange={onPageChange}
              />
            </div>
          )}
        </div>
      )}
    </section>
  );
}
