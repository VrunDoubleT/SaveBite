import { useCallback, useState } from "react";
import { Pagination } from "@/shared/components/pagination";
import { trustApi } from "../api/trustApi";
import { useApiResource } from "../hooks/useApiResource";
import type { TrustScoreEventType } from "../types/trust.types";
import { formatDateTime, formatSignedPoints } from "../utils/format";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";

const PAGE_SIZE = 10;

const EVENT_LABELS: Record<TrustScoreEventType, string> = {
  OrderCompleted: "Order completed",
  NoShow: "No-show",
};

type EventFilter = TrustScoreEventType | "all";

export function TrustHistoryPanel() {
  const [page, setPage] = useState(1);
  const [eventType, setEventType] = useState<EventFilter>("all");

  const fetchHistory = useCallback(
    () =>
      trustApi.getHistory({
        page,
        pageSize: PAGE_SIZE,
        eventType: eventType === "all" ? undefined : eventType,
      }),
    [page, eventType],
  );
  const { data, error, isLoading, reload } = useApiResource(
    `trust:history:${page}:${eventType}`,
    fetchHistory,
  );

  const handleFilterChange = (value: EventFilter) => {
    setEventType(value);
    setPage(1);
  };

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-base font-bold text-text-primary">Score history</h3>
        <label className="flex items-center gap-2 text-sm text-text-secondary">
          Event
          <select
            value={eventType}
            onChange={(event) => handleFilterChange(event.target.value as EventFilter)}
            className="rounded-md border border-border-default bg-white px-3 py-2 text-sm text-text-primary focus:border-border-focus focus:outline-none focus:ring-1 focus:ring-border-focus"
          >
            <option value="all">All events</option>
            <option value="OrderCompleted">Order completed</option>
            <option value="NoShow">No-show</option>
          </select>
        </label>
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
            pageSize={PAGE_SIZE}
            itemLabel="changes"
            hideOnSinglePage
          />
        </>
      )}
    </section>
  );
}
