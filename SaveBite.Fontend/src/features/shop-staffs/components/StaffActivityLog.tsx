import { useState } from "react";
import { Check, Clock, Copy, Package } from "lucide-react";
import type { StaffActivityLog as StaffActivityLogType } from "@/features/shop-staffs/types/shopStaff.types";
import { Pagination, usePagination } from "@/shared/components/pagination";
import { toast } from "@/shared/stores/toastStore";

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

function OrderIdBadge({ orderId }: { orderId: string }) {
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    void navigator.clipboard.writeText(orderId);
    setCopied(true);
    toast.success("Order ID copied to clipboard");
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="mt-1.5 inline-flex max-w-full items-center gap-1.5 rounded-lg border border-blue-200 bg-blue-50/70 px-2.5 py-1 text-xs text-blue-900">
      <Package size={13} className="shrink-0 text-blue-600" />
      <span className="font-medium text-blue-700">Order ID:</span>
      <span className="truncate font-mono font-semibold text-blue-950" title={orderId}>
        {orderId}
      </span>
      <button
        type="button"
        onClick={handleCopy}
        title="Copy order ID"
        className="ml-0.5 rounded p-0.5 text-blue-500 transition hover:bg-blue-100 hover:text-blue-800"
      >
        {copied ? <Check size={12} className="text-emerald-600" /> : <Copy size={12} />}
      </button>
    </div>
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
      <div className="rounded-2xl border border-gray-200 bg-white p-6">
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
    <section className="rounded-2xl border border-gray-200 bg-white">
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
                  <Clock size={16} />
                </div>

                <div className="min-w-0 flex-1">
                  <p className="text-sm font-semibold text-gray-900">{log.action}</p>
                  {log.actorName && (
                    <p className="mt-1 text-xs font-medium text-gray-600">
                      Performed by {log.actorRole === "StoreOwner" ? "Shop owner" : log.actorName}
                    </p>
                  )}

                  {/* Hiển thị Order ID trực tiếp từ API */}
                  {log.orderId && <OrderIdBadge orderId={log.orderId} />}

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
