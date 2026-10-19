import { useCallback, useState } from "react";
import { Pagination } from "@/shared/components/pagination";
import { trustApi } from "../api/trustApi";
import { useApiResource } from "../hooks/useApiResource";
import type { PenaltyStatus } from "../types/trust.types";
import { formatCurrency, formatDateTime } from "../utils/format";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";
import { PenaltyRecoveryModal } from "./PenaltyRecoveryModal";

const PAGE_SIZE = 10;

type StatusFilter = PenaltyStatus | "all";

function PenaltyStatusBadge({ status }: { status: PenaltyStatus }) {
  const recovered = status === "Recovered";
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-1 text-[11px] font-semibold ${
        recovered
          ? "border-primary-100 bg-primary-50 text-primary-700"
          : "border-accent-100 bg-accent-50 text-accent-700"
      }`}
    >
      {recovered ? "Recovered" : "Recovering"}
    </span>
  );
}

// UC-48 (history) and UC-49 (recovery progress of one penalty).
export function TrustPenaltiesPanel() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<StatusFilter>("all");
  const [selectedPenaltyId, setSelectedPenaltyId] = useState<string | null>(null);

  const fetchPenalties = useCallback(
    () =>
      trustApi.getPenalties({
        page,
        pageSize: PAGE_SIZE,
        status: status === "all" ? undefined : status,
      }),
    [page, status],
  );
  const { data, error, isLoading, reload } = useApiResource(
    `trust:penalties:${page}:${status}`,
    fetchPenalties,
  );

  const handleFilterChange = (value: StatusFilter) => {
    setStatus(value);
    setPage(1);
  };

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-base font-bold text-text-primary">Penalty history</h3>
        <label className="flex items-center gap-2 text-sm text-text-secondary">
          Status
          <select
            value={status}
            onChange={(event) => handleFilterChange(event.target.value as StatusFilter)}
            className="rounded-md border border-border-default bg-white px-3 py-2 text-sm text-text-primary focus:border-border-focus focus:outline-none focus:ring-1 focus:ring-border-focus"
          >
            <option value="all">All penalties</option>
            <option value="Recovering">Recovering</option>
            <option value="Recovered">Recovered</option>
          </select>
        </label>
      </div>

      {error && !data ? (
        <PanelError message={error} onRetry={reload} />
      ) : !data ? (
        <PanelSkeleton rows={3} />
      ) : data.items.length === 0 ? (
        <PanelEmpty
          title="No penalties"
          description="Keep picking up your orders on time to stay penalty-free."
        />
      ) : (
        <>
          <div
            className={`overflow-x-auto rounded-xl border border-border-default bg-bg-surface ${isLoading ? "opacity-60" : ""}`}
          >
            <table className="min-w-full divide-y divide-border-default text-left text-sm">
              <thead className="bg-neutral-50 text-text-secondary">
                <tr>
                  <th className="px-5 py-3 font-semibold">Date</th>
                  <th className="px-5 py-3 font-semibold">Order</th>
                  <th className="px-5 py-3 text-right font-semibold">Points</th>
                  <th className="px-5 py-3 font-semibold">Status</th>
                  <th className="px-5 py-3 font-semibold">Recovery</th>
                  <th className="px-5 py-3 text-right font-semibold">
                    <span className="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-default text-text-primary">
                {data.items.map((penalty) => (
                  <tr key={penalty.id}>
                    <td className="whitespace-nowrap px-5 py-3 text-text-secondary">
                      {formatDateTime(penalty.createdAt)}
                    </td>
                    <td className="px-5 py-3">
                      <p className="font-medium">{penalty.orderCode ?? "—"}</p>
                      <p className="text-xs text-text-secondary">
                        No-show #{penalty.occurrenceNumber} this month
                      </p>
                    </td>
                    <td className="px-5 py-3 text-right font-semibold tabular-nums text-danger">
                      -{penalty.scoreDeducted}
                    </td>
                    <td className="px-5 py-3">
                      <PenaltyStatusBadge status={penalty.status} />
                    </td>
                    <td className="px-5 py-3 text-xs text-text-secondary">
                      <p>
                        {penalty.completedOnlineOrders} / {penalty.requiredOnlineOrders} online
                        orders
                      </p>
                      <p>
                        {formatCurrency(penalty.completedOnlineOrderValue)} /{" "}
                        {formatCurrency(penalty.requiredOnlineOrderValue)}
                      </p>
                    </td>
                    <td className="px-5 py-3 text-right">
                      <button
                        type="button"
                        onClick={() => setSelectedPenaltyId(penalty.id)}
                        className="rounded-md border border-border-default bg-white px-3 py-1.5 text-xs font-semibold text-text-secondary transition-colors hover:bg-neutral-50"
                      >
                        View recovery
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <Pagination
            currentPage={data.page}
            totalPages={data.totalPages}
            onPageChange={setPage}
            totalItems={data.total}
            pageSize={PAGE_SIZE}
            itemLabel="penalties"
            hideOnSinglePage
          />
        </>
      )}

      {selectedPenaltyId && (
        <PenaltyRecoveryModal
          penaltyId={selectedPenaltyId}
          onClose={() => setSelectedPenaltyId(null)}
        />
      )}
    </section>
  );
}
