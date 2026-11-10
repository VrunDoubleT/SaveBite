import type { ReactNode } from "react";
import {
  ChevronRight,
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
const SUMMARY_STYLE: Record<string, { icon: string; count: string }> = {
  All: { icon: "bg-slate-100 text-slate-600", count: "text-slate-900" },
  Pending: { icon: "bg-amber-50 text-amber-600", count: "text-amber-600" },
  NeedsRevision: { icon: "bg-orange-50 text-orange-600", count: "text-orange-600" },
  Approved: { icon: "bg-emerald-50 text-emerald-600", count: "text-emerald-600" },
  Rejected: { icon: "bg-rose-50 text-rose-600", count: "text-rose-600" },
  Cancelled: { icon: "bg-slate-100 text-slate-500", count: "text-slate-600" },
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
      <header className="flex flex-wrap items-end justify-between gap-3">
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
          className={`min-w-40 rounded-lg border border-border-default bg-white px-3 py-2 text-sm font-semibold text-text-primary shadow-sm transition-colors hover:border-primary-300 focus:outline-none focus:ring-2 focus:ring-primary-500 ${focusRing}`}
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
        className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-6"
        aria-label="Application totals by status"
      >
        {FILTERS.map((status) => {
          const SummaryIcon =
            status === "All"
              ? ClipboardList
              : status === "NeedsRevision"
                ? PencilLine
                : getStatusMeta(status).icon;
          const style = SUMMARY_STYLE[status] ?? SUMMARY_STYLE.All;

          return (
            <div
              key={status}
              className="flex min-h-20 items-center gap-3 rounded-2xl border border-slate-100 bg-white px-4 py-3 shadow-sm"
            >
              <span
                className={`flex size-10 shrink-0 items-center justify-center rounded-full ${style.icon}`}
                aria-hidden="true"
              >
                <SummaryIcon className="size-4" />
              </span>
              <div className="min-w-0">
                <span className="block whitespace-normal break-words text-xs font-medium leading-tight text-slate-500">
                  {status === "All" ? "Total applications" : getStatusMeta(status).label}
                </span>
                <span className={`mt-0.5 block text-xl font-bold tabular-nums ${style.count}`}>
                  {loading ? "–" : (counts[status] ?? 0)}
                </span>
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
            <table className="hidden w-full table-fixed text-left text-sm md:table">
              <thead className="border-b border-slate-200 bg-slate-50/80 text-xs text-slate-500">
                <tr>
                  <th scope="col" className="w-[18%] px-5 py-3 font-medium">
                    ID
                  </th>
                  <th scope="col" className="w-[26%] px-5 py-3 font-medium">
                    Store
                  </th>
                  <th scope="col" className="w-[18%] px-5 py-3 font-medium">
                    Location
                  </th>
                  <th scope="col" className="w-28 px-5 py-3 font-medium">
                    Submitted
                  </th>
                  <th scope="col" className="w-32 px-5 py-3 font-medium">
                    Status
                  </th>
                  <th scope="col" className="w-24 px-5 py-3 font-medium">
                    Revision
                  </th>
                  <th scope="col" className="w-36 px-5 py-3">
                    <span className="sr-only">Action</span>
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100">
                {rows.map((item) => {
                  const opening = openingId === item.id;
                  const place = locationOf(item);

                  return (
                    <tr
                      key={item.id}
                      onClick={() => !busy && openDetail(item)}
                      className={`group transition-colors hover:bg-emerald-50/40 ${
                        busy ? "cursor-wait" : "cursor-pointer"
                      } ${opening ? "bg-emerald-50/60" : ""}`}
                    >
                      <td className="max-w-40 px-5 py-3.5">
                        <RecordId id={item.id} />
                      </td>
                      <td className="px-5 py-3.5">
                        <div className="flex items-center gap-3">
                          <Avatar item={item} />
                          <div className="min-w-0 flex-1">
                            <p className="truncate font-semibold text-slate-900" title={item.name}>
                              {item.name}
                            </p>
                            <p className="mt-0.5 truncate text-xs text-slate-500">
                              {item.businessLicenseNo || "No license number"}
                            </p>
                          </div>
                        </div>
                      </td>

                      <td className="px-5 py-3.5 text-slate-600">
                        {place ? (
                          <span className="flex items-start gap-1.5" title={place}>
                            <MapPin
                              className="mt-0.5 size-3.5 shrink-0 text-slate-400"
                              aria-hidden="true"
                            />
                            <span className="line-clamp-2 min-w-0 break-words">{place}</span>
                          </span>
                        ) : (
                          <span className="text-slate-400">—</span>
                        )}
                      </td>

                      <td className="whitespace-nowrap px-5 py-3.5 text-slate-600">
                        {shortDate(item.createdAt)}
                      </td>

                      <td className="px-5 py-3.5">
                        <StatusBadge status={item.status} />
                      </td>

                      <td className="px-5 py-3.5">
                        <span className="rounded-md bg-slate-100 px-2 py-0.5 text-xs font-medium tabular-nums text-slate-600">
                          #{item.revisionNumber}
                        </span>
                      </td>

                      <td className="px-5 py-3.5 text-right">
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            openDetail(item);
                          }}
                          disabled={busy}
                          aria-label={`${actionLabel(item.status)} application from ${item.name}`}
                          className={`inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-3 py-1.5 text-xs font-semibold text-text-secondary shadow-sm transition-colors hover:border-primary-300 hover:bg-primary-50 hover:text-primary-700 disabled:cursor-not-allowed disabled:opacity-50 ${focusRing}`}
                        >
                          {opening ? "Opening…" : actionLabel(item.status)}
                          {opening ? (
                            <Loader2 className="size-3.5 animate-spin motion-reduce:animate-none" aria-hidden="true" />
                          ) : (
                            <Eye className="size-3.5" aria-hidden="true" />
                          )}
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>

            {/* Mobile cards */}
            <ul className="divide-y divide-slate-100 md:hidden">
              {rows.map((item) => {
                const opening = openingId === item.id;
                const place = locationOf(item);

                return (
                  <li key={item.id}>
                    <div className={`flex items-start gap-3 px-4 py-4 ${opening ? "bg-emerald-50/60" : ""}`}>
                      <div className="mt-1 shrink-0">
                        <RecordId id={item.id} />
                      </div>
                      <Avatar item={item} />

                      <span className="min-w-0 flex-1">
                        <button
                          type="button"
                          onClick={() => openDetail(item)}
                          disabled={busy}
                          className="w-full text-left hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 disabled:opacity-60"
                        >
                          <span className="flex items-start justify-between gap-2">
                            <span
                              className="min-w-0 truncate font-semibold text-slate-900"
                              title={item.name}
                            >
                              {item.name}
                            </span>
                            <span className="shrink-0">
                              <StatusBadge status={item.status} />
                            </span>
                          </span>

                          <span className="mt-1 flex items-center gap-1 text-xs text-slate-500">
                            <MapPin className="size-3 shrink-0 text-slate-400" aria-hidden="true" />
                            <span className="truncate">{place || "—"}</span>
                          </span>

                          <span className="mt-1 block text-xs text-slate-500">
                            Revision {item.revisionNumber}, {shortDate(item.createdAt)}
                          </span>
                        </button>
                      </span>

                      {opening ? (
                        <Loader2
                          className="mt-3 size-4 shrink-0 animate-spin text-slate-400 motion-reduce:animate-none"
                          aria-hidden="true"
                        />
                      ) : (
                        <ChevronRight
                          className="mt-3 size-4 shrink-0 text-slate-300"
                          aria-hidden="true"
                        />
                      )}
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
