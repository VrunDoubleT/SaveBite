import type { ReactNode } from "react";
import {
  ClipboardList,
  Eye,
  Inbox,
  Loader2,
  MapPin,
  PencilLine,
} from "lucide-react";

import type { ShopApplication } from "../types/adminShopApplication.types";
import { RecordId } from "@/shared/components/RecordId";
import {
  FILTERS,
  focusRing,
  getStatusMeta,
  shortDate,
} from "../schemas/adminShopApplication.schema";
import { Notice, StatusBadge } from "./AdminShopApplicationShared";

type Props = {
  visibleItems: ShopApplication[];
  loading: boolean;
  filter: string;
  setFilter: (value: string) => void;
  counts: Record<string, number>;
  error: string;
  load: () => void;
  openDetail: (item: ShopApplication) => void;
  openingId: ShopApplication["id"] | null;
  pagination?: ReactNode;
};

/* helpers */
const locationOf = (item: ShopApplication) =>
  [item.district, item.city].filter(Boolean).join(", ") || item.addressLine || "";

const actionLabel = (status: string) => (status.toLowerCase() === "pending" ? "Review" : "View");
const SUMMARY_ICON_STYLE: Record<string, string> = {
All: "bg-neutral-100 text-neutral-600",
Pending: "bg-amber-50 text-amber-600",
NeedsRevision: "bg-orange-50 text-orange-600",
Approved: "bg-emerald-50 text-emerald-600",
Rejected: "bg-rose-50 text-rose-600",
Cancelled: "bg-slate-100 text-slate-500",
};

function Avatar({ item }: { item: ShopApplication }) {
  if (item.logoUrl) {
    return (
      <img
        src={item.logoUrl}
        alt=""
        className="size-11 shrink-0 rounded-xl border border-slate-200 bg-white object-cover"
      />
    );
  }

  return (
    <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-slate-100 to-slate-200 text-base font-semibold text-slate-600">
      {item.name.slice(0, 1).toUpperCase()}
    </span>
  );
}

/* component */
export function AdminShopApplicationsList({
  visibleItems: rows,
  loading,
  filter,
  setFilter,
  counts,
  error,
  load,
  openDetail,
  openingId,
  pagination,
}: Props) {
  const busy = openingId !== null;

  return (
    <div className="space-y-6">
      {/* Header */}
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight text-neutral-900">
              Shop Applications
            </h1>
          </div>
          <p className="text-sm text-text-secondary">
            Review submitted shop registrations and look back at earlier decisions.
          </p>
        </div>

        <select
          aria-label="Filter applications by status"
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
          className={`min-h-10 w-full rounded-lg border border-border-default bg-white px-3 text-sm font-medium text-text-primary shadow-sm transition-colors hover:border-primary-300 focus:outline-none focus:ring-2 focus:ring-primary-500 sm:w-56 ${focusRing}`}
        >
          {FILTERS.map((status) => (
            <option key={status} value={status}>
              {status === "All" ? "All statuses" : getStatusMeta(status).label}
            </option>
          ))}
        </select>
      </header>

      {error && (
        <Notice tone="error">
          <span className="flex flex-wrap items-center justify-between gap-3">
            <span>{error}</span>
            <button
              type="button"
              onClick={() => load()}
              disabled={loading}
              className={`rounded-md px-2 py-1 text-sm font-semibold underline underline-offset-2 hover:no-underline disabled:opacity-60 ${focusRing}`}
            >
              Try again
            </button>
          </span>
        </Notice>
      )}

      {/* Status totals are an overview only; filtering is controlled by the dropdown above. */}
      <div
        className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6 sm:gap-4"
        role="region"
        aria-label="Application totals by status"
      >
        {FILTERS.map((status) => {
          const SummaryIcon =
            status === "All"
              ? ClipboardList
              : status === "NeedsRevision"
                ? PencilLine
                : getStatusMeta(status).icon;
          const iconStyle = SUMMARY_ICON_STYLE[status] ?? SUMMARY_ICON_STYLE.All;

          return (
            <div
              key={status}
              className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs"
            >
              <div className="flex items-center gap-3">
                <span
                  className={`flex size-10 shrink-0 items-center justify-center rounded-lg ${iconStyle}`}
                  aria-hidden="true"
                >
                  <SummaryIcon className="size-5" />
                </span>
                <div className="min-w-0">
                  <p className="truncate text-xs font-medium text-slate-500">
                    {status === "All" ? "All applications" : getStatusMeta(status).label}
                  </p>
                  <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                    {loading ? "–" : (counts[status] ?? 0)}
                  </p>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* List */}
      <div className="overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm">
        {loading ? (
          <div
            className="divide-y divide-slate-100"
            role="status"
            aria-label="Loading applications"
          >
            {[0, 1, 2, 3, 4].map((row) => (
              <div key={row} className="flex items-center gap-4 px-5 py-4">
                <div className="size-11 animate-pulse rounded-xl bg-slate-100 motion-reduce:animate-none" />
                <div className="flex-1 space-y-2">
                  <div className="h-4 w-1/3 animate-pulse rounded bg-slate-100 motion-reduce:animate-none" />
                  <div className="h-3 w-1/4 animate-pulse rounded bg-slate-100 motion-reduce:animate-none" />
                </div>
                <div className="hidden h-4 w-24 animate-pulse rounded bg-slate-100 motion-reduce:animate-none sm:block" />
                <div className="h-6 w-20 animate-pulse rounded-full bg-slate-100 motion-reduce:animate-none" />
              </div>
            ))}
          </div>
        ) : rows.length === 0 ? (
          <div className="flex flex-col items-center px-6 py-16 text-center">
            <span className="flex size-14 items-center justify-center rounded-full bg-slate-100 text-slate-500">
              <Inbox className="size-7" aria-hidden="true" />
            </span>

            <p className="mt-4 font-semibold text-slate-900">No applications found</p>

            <p className="mt-1 max-w-sm text-sm text-slate-500">
              {filter === "All"
                ? "No store has applied yet. New registrations will show up here."
                : `No applications are ${getStatusMeta(filter).label.toLowerCase()} right now.`}
            </p>

            {filter !== "All" && (
              <button
                type="button"
                onClick={() => setFilter("All")}
                className={`mt-5 rounded-lg bg-emerald-600 px-3.5 py-2 text-sm font-medium text-white hover:bg-emerald-700 ${focusRing}`}
              >
                Show all applications
              </button>
            )}
          </div>
        ) : (
          <>
            {/* Desktop table */}
            <div className="hidden overflow-x-auto md:block">
              <table className="w-full min-w-[1100px] table-fixed text-left text-sm">
                <colgroup>
                  <col className="w-[14%]" />
                  <col className="w-[24%]" />
                  <col className="w-[19%]" />
                  <col className="w-[13%]" />
                  <col className="w-[12%]" />
                  <col className="w-[8%]" />
                  <col className="w-[10%]" />
                </colgroup>
                <thead className="border-b border-border-default bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-text-secondary">
                  <tr>
                    <th scope="col" className="px-5 py-4">
                      ID
                    </th>
                    <th scope="col" className="px-5 py-4">
                      Store
                    </th>
                    <th scope="col" className="px-5 py-4">
                      Location
                    </th>
                    <th scope="col" className="px-5 py-4">
                      Submitted
                    </th>
                    <th scope="col" className="px-5 py-4">
                      Status
                    </th>
                    <th scope="col" className="px-5 py-4">
                      Revision
                    </th>
                    <th scope="col" className="px-5 py-4 text-right">
                      Actions
                    </th>
                  </tr>
                </thead>

                <tbody className="divide-y divide-border-default text-text-primary">
                  {rows.map((item) => {
                    const opening = openingId === item.id;
                    const place = locationOf(item);

                    return (
                      <tr
                        key={item.id}
                        onClick={() => !busy && openDetail(item)}
                        className={`transition-colors hover:bg-neutral-50/70 ${
                          busy ? "cursor-wait" : "cursor-pointer"
                        } ${opening ? "bg-primary-50/50" : ""}`}
                      >
                        <td className="max-w-40 px-5 py-4">
                          <RecordId id={item.id} />
                        </td>
                        <td className="min-w-0 px-5 py-4">
                          <div className="flex min-w-0 items-center gap-3">
                            <Avatar item={item} />
                            <div className="min-w-0 flex-1">
                              <p className="truncate font-semibold text-text-primary" title={item.name}>
                                {item.name}
                              </p>
                              <p className="mt-0.5 truncate text-xs text-text-muted">
                                {item.businessLicenseNo || "No license number"}
                              </p>
                            </div>
                          </div>
                        </td>

                        <td className="px-5 py-4 text-text-secondary">
                          {place ? (
                            <span className="flex items-start gap-1.5" title={place}>
                              <MapPin
                                className="mt-0.5 size-3.5 shrink-0 text-text-muted"
                                aria-hidden="true"
                              />
                              <span className="line-clamp-2 min-w-0 break-words">{place}</span>
                            </span>
                          ) : (
                            <span className="text-text-muted">—</span>
                          )}
                        </td>

                        <td className="whitespace-nowrap px-5 py-4 text-text-secondary">
                          {shortDate(item.createdAt)}
                        </td>

                        <td className="px-5 py-4">
                          <StatusBadge status={item.status} />
                        </td>

                        <td className="px-5 py-4">
                          <span className="inline-flex rounded-md bg-neutral-100 px-2 py-1 text-xs font-medium tabular-nums text-neutral-700">
                            #{item.revisionNumber}
                          </span>
                        </td>

                        <td className="px-5 py-4 text-right">
                          <button
                            type="button"
                            onClick={(event) => {
                              event.stopPropagation();
                              openDetail(item);
                            }}
                            disabled={busy}
                            aria-label={`${actionLabel(item.status)} application from ${item.name}`}
                            title={actionLabel(item.status)}
                            className={`inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-600 transition hover:bg-neutral-50 hover:text-neutral-900 disabled:cursor-not-allowed disabled:opacity-50 ${focusRing}`}
                          >
                            {opening ? (
                              <Loader2 className="size-3.5 animate-spin motion-reduce:animate-none" aria-hidden="true" />
                            ) : (
                              <Eye className="size-3.5" aria-hidden="true" />
                            )}
                            {!opening && <span>{actionLabel(item.status)}</span>}
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {/* Mobile cards */}
            <ul className="divide-y divide-border-default md:hidden">
              {rows.map((item) => {
                const opening = openingId === item.id;
                const place = locationOf(item);

                return (
                  <li key={item.id} className={opening ? "bg-primary-50/50" : ""}>
                    <div className="flex items-start gap-3 px-4 py-4">
                      <div className="shrink-0">
                        <RecordId id={item.id} />
                      </div>
                      <Avatar item={item} />

                      <div className="min-w-0 flex-1">
                        <div className="flex items-start justify-between gap-2">
                          <p className="min-w-0 truncate font-semibold text-text-primary" title={item.name}>
                            {item.name}
                          </p>
                          <span className="shrink-0">
                            <StatusBadge status={item.status} />
                          </span>
                        </div>

                        <p className="mt-1 truncate text-xs text-text-muted">
                          {item.businessLicenseNo || "No license number"}
                        </p>
                        <p className="mt-1 flex items-center gap-1 text-xs text-text-secondary" title={place}>
                          <MapPin className="size-3 shrink-0 text-text-muted" aria-hidden="true" />
                          <span className="truncate">{place || "—"}</span>
                        </p>

                        <div className="mt-3 flex items-center justify-between gap-3 border-t border-border-default pt-3">
                          <span className="text-xs text-text-muted">
                            Revision {item.revisionNumber} · {shortDate(item.createdAt)}
                          </span>
                          <button
                            type="button"
                            onClick={() => openDetail(item)}
                            disabled={busy}
                            aria-label={`${actionLabel(item.status)} application from ${item.name}`}
                            className={`inline-flex shrink-0 items-center gap-1.5 rounded-md border border-border-default bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-600 transition hover:bg-neutral-50 hover:text-neutral-900 disabled:cursor-not-allowed disabled:opacity-50 ${focusRing}`}
                          >
                            {opening ? (
                              <Loader2 className="size-3.5 animate-spin motion-reduce:animate-none" aria-hidden="true" />
                            ) : (
                              <>
                                <Eye className="size-3.5" aria-hidden="true" />
                                {actionLabel(item.status)}
                              </>
                            )}
                          </button>
                        </div>
                      </div>
                    </div>
                  </li>
                );
              })}
            </ul>
          </>
        )}
      </div>
      {pagination}
    </div>
  );
}
