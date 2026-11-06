import { Link } from "react-router-dom";
import { Clock, MessageSquare, Package } from "lucide-react";
import type { StaffActivityLog as StaffActivityLogType } from "@/features/shop-staffs/types/shopStaff.types";
import { Pagination, usePagination } from "@/shared/components/pagination";

interface StaffActivityLogProps {
  logs: StaffActivityLogType[];
  loading?: boolean;
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

export function StaffActivityLog({ logs, loading = false }: StaffActivityLogProps) {
  const {
    currentPage,
    setCurrentPage,
    totalPages,
    paginatedItems: paginatedLogs,
    totalItems,
  } = usePagination({
    items: logs,
    initialPageSize: 8,
  });

  if (loading) {
    return (
      <div className="rounded-xl border border-neutral-200 bg-white shadow-2xs p-6">
        <div className="h-5 w-40 animate-pulse rounded bg-gray-100" />

        <div className="mt-6 space-y-5">
          {[1, 2, 3].map((item) => (
            <div key={item} className="flex gap-4">
              <div className="h-9 w-9 animate-pulse rounded-full bg-gray-100" />

              <div className="flex-1 space-y-2">
                <div className="h-4 w-48 animate-pulse rounded bg-gray-100" />
                <div className="h-3 w-32 animate-pulse rounded bg-gray-100" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <section className="rounded-xl border border-neutral-200 bg-white shadow-2xs">
      <div className="border-b border-gray-100 px-6 py-5">
        <div className="flex items-center gap-2">
          <Clock size={19} className="text-gray-500" />

          <h2 className="text-base font-semibold text-gray-900">Activity History</h2>
        </div>

        <p className="mt-1 text-sm text-gray-500">Recent activities of this staff member.</p>
      </div>

      {logs.length === 0 ? (
        <div className="px-6 py-10 text-center">
          <Clock size={28} className="mx-auto text-gray-300" />

          <p className="mt-3 text-sm font-medium text-gray-700">No activity yet</p>

          <p className="mt-1 text-sm text-gray-400">
            There are no recorded activities for this staff member.
          </p>
        </div>
      ) : (
        <div className="space-y-6 px-6 py-5">
          <div className="space-y-6">
            {paginatedLogs.map((log) => (
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
                currentPage={currentPage}
                totalPages={totalPages}
                totalItems={totalItems}
                pageSize={8}
                itemLabel="activities"
                onPageChange={setCurrentPage}
              />
            </div>
          )}
        </div>
      )}
    </section>
  );
}
