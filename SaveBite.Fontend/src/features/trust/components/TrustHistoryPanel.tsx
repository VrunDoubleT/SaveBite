import { useCallback } from "react";
import { Link, Navigate } from "react-router-dom";
import { AccountPagination as Pagination } from "@/shared/components/pagination";
import { trustApi } from "../api/trustApi";
import { useApiResource } from "../hooks/useApiResource";
import { TRUST_PAGE_SIZE, useListParams } from "../hooks/useListParams";
import type { TrustScoreEventType } from "../types/trust.types";
import { formatCurrency, formatDateTime, formatSignedCurrency, formatSignedPoints } from "../utils/format";
import { Badge, Card, FilterBar } from "@/shared/components/ui";
import { APP_PATHS } from "@/app/router/paths";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";

const EVENT_LABELS: Record<TrustScoreEventType, string> = {
  OrderCompleted: "Order completed",
  NoShow: "No-show",
  RefundReclaimed: "Refund adjustment",
};

const EVENT_FILTERS = ["OrderCompleted", "NoShow", "RefundReclaimed"] as const;
const EVENT_OPTIONS = EVENT_FILTERS.map((value) => ({ value, label: EVENT_LABELS[value] }));

export function TrustHistoryPanel() {
  const {
    page,
    filter: eventType,
    setPage,
    setFilter,
    pageSearch,
  } = useListParams("event", EVENT_FILTERS);

  const fetchHistory = useCallback(
    () =>
      trustApi.getHistory({
        page,
        pageSize: TRUST_PAGE_SIZE,
        eventType: eventType ?? undefined,
      }),
    [page, eventType],
  );
  const { data, error, isLoading, reload } = useApiResource(
    `trust:history:${page}:${eventType ?? "all"}`,
    fetchHistory,
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
            label: "Event",
            value: eventType ?? "All",
            options: [{ value: "All", label: "All events" }, ...EVENT_OPTIONS],
            onChange: (value) => setFilter(value === "All" ? null : (value as TrustScoreEventType)),
          },
        ]}
      />

      {error && !data ? (
        <PanelError message={error} onRetry={reload} />
      ) : !data ? (
        <PanelSkeleton rows={4} />
      ) : data.items.length === 0 ? (
        <PanelEmpty
          title="No history yet"
          description="Your score and accumulated order value changes will appear here after you complete an order."
        />
      ) : (
        <>
          <div
            role="region"
            aria-label="Score and value changes"
            className={`space-y-4 ${isLoading ? "opacity-60" : ""}`}
          >
            {data.items.map((item) => (
              <Card padding="compact" key={item.id} className="space-y-2">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <h2 className="text-body font-semibold text-text-primary">
                      {EVENT_LABELS[item.eventType] ?? item.eventType}
                    </h2>
                    <p className="mt-1 text-caption text-text-secondary">
                      {formatDateTime(item.createdAt)}
                    </p>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    {/* Hide a zero change so a refund row shows only the money badge (and vice versa). */}
                    {item.scoreDelta !== 0 && (
                      <Badge
                        tone={item.scoreDelta > 0 ? "primary" : "danger"}
                        className="!text-body tabular-nums"
                      >
                        {formatSignedPoints(item.scoreDelta)} points
                      </Badge>
                    )}
                    {item.valueDelta !== 0 && (
                      <Badge
                        tone={item.valueDelta > 0 ? "primary" : "danger"}
                        className="!text-body tabular-nums"
                      >
                        {formatSignedCurrency(item.valueDelta)}
                      </Badge>
                    )}
                  </div>
                </div>
                {/* Trust score: before -> after this event */}
                <p className="text-caption font-semibold uppercase tracking-wide text-text-secondary">
                  Trust score
                </p>
                <div className="grid grid-cols-3 gap-2 rounded-lg bg-neutral-50 p-3 text-center">
                  <div>
                    <p className="text-caption text-text-secondary">Before</p>
                    <p className="text-body font-semibold tabular-nums text-text-primary">
                      {item.scoreBefore}
                    </p>
                  </div>
                  <div>
                    <p className="text-caption text-text-secondary">Change</p>
                    <p
                      className={`text-body font-semibold tabular-nums ${
                        item.scoreDelta >= 0 ? "text-primary-700" : "text-red-700"
                      }`}
                    >
                      {formatSignedPoints(item.scoreDelta)}
                    </p>
                  </div>
                  <div>
                    <p className="text-caption text-text-secondary">After</p>
                    <p className="text-body font-semibold tabular-nums text-text-primary">
                      {item.scoreAfter}
                    </p>
                  </div>
                </div>
                {/* Accumulated order value (money): before -> after this event */}
                <p className="text-caption font-semibold uppercase tracking-wide text-text-secondary">
                  Accumulated order value
                </p>
                <div className="grid grid-cols-3 gap-2 rounded-lg bg-neutral-50 p-3 text-center">
                  <div>
                    <p className="text-caption text-text-secondary">Before</p>
                    <p className="text-body font-semibold tabular-nums text-text-primary">
                      {formatCurrency(item.valueBefore)}
                    </p>
                  </div>
                  <div>
                    <p className="text-caption text-text-secondary">Change</p>
                    <p
                      className={`text-body font-semibold tabular-nums ${
                        item.valueDelta >= 0 ? "text-primary-700" : "text-red-700"
                      }`}
                    >
                      {formatSignedCurrency(item.valueDelta)}
                    </p>
                  </div>
                  <div>
                    <p className="text-caption text-text-secondary">After</p>
                    <p className="text-body font-semibold tabular-nums text-text-primary">
                      {formatCurrency(item.valueAfter)}
                    </p>
                  </div>
                </div>
                {item.previousLevelName && item.newLevelName && (
                  <p className="text-caption text-text-secondary">
                    Level changed:{" "}
                    <strong className="text-text-primary">{item.previousLevelName}</strong> →{" "}
                    <strong className="text-text-primary">{item.newLevelName}</strong>
                  </p>
                )}
                {item.reason && (
                  <p className="text-body text-text-secondary [overflow-wrap:anywhere]">
                    {item.reason}
                  </p>
                )}
                <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border-default pt-3 text-caption text-text-secondary">
                  <p>
                    Order{" "}
                    {item.orderId ? (
                      <Link
                        to={APP_PATHS.ORDER_DETAIL.replace(
                          ":orderId",
                          encodeURIComponent(item.orderId),
                        )}
                        className="font-semibold text-primary-700 hover:underline"
                      >
                        {item.orderCode ?? item.orderId}
                      </Link>
                    ) : (
                      (item.orderCode ?? "—")
                    )}
                  </p>
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
            itemLabel="changes"
          />
        </>
      )}
    </section>
  );
}
