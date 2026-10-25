import { useCallback } from "react";
import { Navigate } from "react-router-dom";
import { Pagination } from "@/shared/components/pagination";
import { trustApi } from "../api/trustApi";
import { useApiResource } from "../hooks/useApiResource";
import { TRUST_PAGE_SIZE, useListParams } from "../hooks/useListParams";
import type { TrustScoreEventType } from "../types/trust.types";
import { formatDateTime, formatSignedPoints } from "../utils/format";
import { ListFilter } from "./ListFilter";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";

const EVENT_LABELS: Record<TrustScoreEventType, string> = {
  OrderCompleted: "Order completed",
  NoShow: "No-show",
};

const EVENT_FILTERS = ["OrderCompleted", "NoShow"] as const;
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
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-base font-bold text-text-primary">Score history</h3>
        <ListFilter
          label="Event"
          allLabel="All events"
          value={eventType}
          options={EVENT_OPTIONS}
          onChange={setFilter}
        />
      </div>

      {error && !data ? (
        <PanelError message={error} onRetry={reload} />
      ) : !data ? (
        <PanelSkeleton rows={4} />
      ) : data.items.length === 0 ? (
        <PanelEmpty
          title="No score changes yet"
          description="Your score changes will appear here after you complete an order."
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
                  <th className="px-5 py-3 font-semibold">Event</th>
                  <th className="px-5 py-3 font-semibold">Order</th>
                  <th className="px-5 py-3 text-right font-semibold">Change</th>
                  <th className="px-5 py-3 text-right font-semibold">Score after</th>
                  <th className="px-5 py-3 font-semibold">Note</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-default text-text-primary">
                {data.items.map((item) => (
                  <tr key={item.id}>
                    <td className="whitespace-nowrap px-5 py-3 text-text-secondary">
                      {formatDateTime(item.createdAt)}
                    </td>
                    <td className="px-5 py-3 font-medium">
                      {EVENT_LABELS[item.eventType] ?? item.eventType}
                    </td>
                    <td className="px-5 py-3 text-text-secondary">{item.orderCode ?? "—"}</td>
                    <td
                      className={`px-5 py-3 text-right font-semibold tabular-nums ${
                        item.scoreDelta >= 0 ? "text-primary-700" : "text-danger"
                      }`}
                    >
                      {formatSignedPoints(item.scoreDelta)}
                    </td>
                    <td className="px-5 py-3 text-right tabular-nums">
                      <span className="text-text-muted">{item.scoreBefore} → </span>
                      {item.scoreAfter}
                    </td>
                    <td className="max-w-[260px] px-5 py-3 text-text-secondary">
                      {item.reason ?? "—"}
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
            itemLabel="changes"
            hideOnSinglePage
          />
        </>
      )}
    </section>
  );
}
