import { useCallback, useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { AccountPagination as Pagination } from "@/shared/components/pagination";
import { trustApi } from "../api/trustApi";
import { useApiResource } from "../hooks/useApiResource";
import { TRUST_PAGE_SIZE, useListParams } from "../hooks/useListParams";
import type { PenaltyStatus } from "../types/trust.types";
import { formatCurrency, formatDateTime } from "../utils/format";
import { Badge, Button, Card, FilterBar } from "@/shared/components/ui";
import { APP_PATHS } from "@/app/router/paths";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";
import { PenaltyRecoveryModal } from "./PenaltyRecoveryModal";

const STATUS_FILTERS = ["Recovering", "Recovered"] as const;
const STATUS_OPTIONS = STATUS_FILTERS.map((value) => ({ value, label: value }));

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
      <FilterBar
        filters={[
          {
            label: "Penalty status",
            value: status ?? "All",
            options: [{ value: "All", label: "All penalties" }, ...STATUS_OPTIONS],
            onChange: (value) => setFilter(value === "All" ? null : (value as PenaltyStatus)),
          },
        ]}
      />

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
            role="region"
            aria-label="Penalty history"
            className={`space-y-4 ${isLoading ? "opacity-60" : ""}`}
          >
            {data.items.map((penalty) => (
              <Card key={penalty.id} className="space-y-3 !p-3 sm:!p-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <h2 className="text-body font-semibold text-text-primary">
                      No-show #{penalty.occurrenceNumber} this month
                    </h2>
                    <p className="mt-1 text-caption text-text-secondary">
                      {formatDateTime(penalty.createdAt)}
                    </p>
                  </div>
                  <Badge tone={penalty.status === "Recovered" ? "primary" : "warning"}>
                    {penalty.status}
                  </Badge>
                </div>
                {penalty.reason && (
                  <p className="text-body text-text-secondary [overflow-wrap:anywhere]">
                    {penalty.reason}
                  </p>
                )}
                <div className="flex flex-wrap items-center justify-between gap-3 text-body">
                  <Link
                    to={APP_PATHS.ORDER_DETAIL.replace(
                      ":orderId",
                      encodeURIComponent(penalty.orderId),
                    )}
                    className="font-semibold text-primary-700 hover:underline"
                  >
                    Order {penalty.orderCode ?? "—"}
                  </Link>
                  <span className="font-semibold tabular-nums text-danger-strong">
                    -{penalty.scoreDeducted} points
                  </span>
                </div>
                <dl className="grid gap-3 sm:grid-cols-2">
                  <div className="rounded-md bg-neutral-50 p-3">
                    <dt className="text-caption text-text-secondary">Recovery orders</dt>
                    <dd className="mt-1 text-body font-semibold text-text-primary">
                      {penalty.completedOnlineOrders} / {penalty.requiredOnlineOrders} online orders
                    </dd>
                  </div>
                  <div className="rounded-md bg-secondary-50 p-3">
                    <dt className="text-caption text-secondary-700">Recovery order value</dt>
                    <dd className="mt-1 break-words text-body font-semibold text-secondary-700">
                      {formatCurrency(penalty.completedOnlineOrderValue)} /{" "}
                      {formatCurrency(penalty.requiredOnlineOrderValue)}
                    </dd>
                  </div>
                </dl>
                {penalty.permanentOfflineLock && (
                  <p className="rounded-md bg-danger-soft p-3 text-caption text-danger-strong">
                    Pay at shop is permanently unavailable for this account.
                  </p>
                )}
                <div className="flex justify-end border-t border-border-default pt-3">
                  <Button
                    variant="secondary"
                    onClick={() => setSelectedPenaltyId(penalty.id)}
                    className="min-h-11"
                  >
                    View recovery
                  </Button>
                </div>
              </Card>
            ))}
          </div>

          <Pagination
            currentPage={data.page}
            totalPages={data.totalPages}
            onPageChange={setPage}
            totalItems={data.total}
            pageSize={TRUST_PAGE_SIZE}
            itemLabel="penalties"
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
