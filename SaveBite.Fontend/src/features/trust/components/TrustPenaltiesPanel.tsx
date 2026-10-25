import { useCallback, useState } from "react";
import { Navigate } from "react-router-dom";
import { Pagination } from "@/shared/components/pagination";
import { trustApi } from "../api/trustApi";
import { useApiResource } from "../hooks/useApiResource";
import { TRUST_PAGE_SIZE, useListParams } from "../hooks/useListParams";
import type { PenaltyStatus } from "../types/trust.types";
import { formatCurrency, formatDateTime } from "../utils/format";
import { ListFilter } from "./ListFilter";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";
import { PenaltyRecoveryModal } from "./PenaltyRecoveryModal";

const STATUS_FILTERS = ["Recovering", "Recovered"] as const;
const STATUS_OPTIONS = STATUS_FILTERS.map((value) => ({ value, label: value }));

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
  const {
    page,
    filter: status,
    setPage,
    setFilter,
    pageSearch,
  } = useListParams("status", STATUS_FILTERS);
  const [selectedPenaltyId, setSelectedPenaltyId] = useState<string | null>(null);

  const fetchPenalties = useCallback(
    () =>
      trustApi.getPenalties({
        page,
        pageSize: TRUST_PAGE_SIZE,
        status: status ?? undefined,
      }),
    [page, status],
  );
  const { data, error, isLoading, reload } = useApiResource(
    `trust:penalties:${page}:${status ?? "all"}`,
    fetchPenalties,
  );

  // A page number beyond the last page (typed in the URL, or left over after the data shrank)
  // is corrected to the last page instead of showing an empty list.
  if (data && data.page === page && page > Math.max(data.totalPages, 1)) {
    return <Navigate replace to={{ search: pageSearch(Math.max(data.totalPages, 1)) }} />;
  }

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-base font-bold text-text-primary">Penalty history</h3>
        <ListFilter
          label="Status"
          allLabel="All penalties"
          value={status}
          options={STATUS_OPTIONS}
          onChange={setFilter}
        />
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
            pageSize={TRUST_PAGE_SIZE}
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
