import { useMemo, useState } from "react";
import {
  ArrowRight,
  ArrowUpDown,
  ChevronRight,
  Inbox,
  Loader2,
  MapPin,
  RefreshCw,
} from "lucide-react";

import type { ShopApplication } from "../types/adminShopApplication.types";
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
};

type SortOrder = "newest" | "oldest";

/* helpers */
const locationOf = (item: ShopApplication) =>
  [item.district, item.city].filter(Boolean).join(", ") || item.addressLine || "";

const DOT_CLASS: Record<string, string> = {
  pending: "bg-amber-500",
  approved: "bg-emerald-500",
  rejected: "bg-rose-500",
  cancelled: "bg-slate-400",
};

const dotClass = (status: string) => DOT_CLASS[status.toLowerCase()] ?? "bg-slate-400";

const actionLabel = (status: string) => (status.toLowerCase() === "pending" ? "Review" : "View");

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
  visibleItems,
  loading,
  filter,
  setFilter,
  counts,
  error,
  load,
  openDetail,
  openingId,
}: Props) {
  const [sort, setSort] = useState<SortOrder>("newest");

  const busy = openingId !== null;

  const rows = useMemo(
    () =>
      [...visibleItems].sort((a, b) => {
        const diff = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
        return sort === "newest" ? -diff : diff;
      }),
    [visibleItems, sort],
  );

  return (
    <div className="mx-auto max-w-7xl space-y-5 p-4 sm:p-6">
      {/* Header */}
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">
            Store applications
          </h1>
          <p className="mt-1 max-w-xl text-sm text-slate-500">
            Review submitted store registrations and look back at earlier decisions.
          </p>
        </div>

        <button
          type="button"
          onClick={() => load()}
          disabled={loading}
          className={`inline-flex items-center gap-2 rounded-lg border border-slate-300 bg-white px-3.5 py-2 text-sm font-medium text-slate-700 shadow-sm hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60 ${focusRing}`}
        >
          <RefreshCw
            className={`size-4 ${loading ? "animate-spin motion-reduce:animate-none" : ""}`}
            aria-hidden="true"
          />
          Refresh
        </button>
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

      {/* Status tabs: counts act as the overview */}
      <div
        className="grid grid-cols-2 gap-2 sm:grid-cols-[repeat(auto-fit,minmax(6.5rem,1fr))] [&>*:last-child:nth-child(odd)]:col-span-2 sm:[&>*:last-child:nth-child(odd)]:col-span-1"
        role="group"
        aria-label="Filter by status"
      >
        {FILTERS.map((status) => {
          const active = filter === status;

          return (
            <button
              key={status}
              type="button"
              onClick={() => setFilter(status)}
              aria-pressed={active}
              className={`group rounded-xl border px-3 py-3 text-left transition-colors sm:px-4 motion-reduce:transition-none ${focusRing} ${
                active
                  ? "border-emerald-600 bg-emerald-50 text-emerald-900 ring-1 ring-emerald-600"
                  : "border-slate-200 bg-white text-slate-700 hover:border-emerald-300 hover:bg-emerald-50/40"
              }`}
            >
              <span className="flex items-center gap-2 text-sm font-medium">
                {status !== "All" && (
                  <span className={`size-2 rounded-full ${dotClass(status)}`} aria-hidden="true" />
                )}
                {status === "All" ? status : getStatusMeta(status).label}
              </span>
              <span
                className={`mt-1 block text-2xl font-semibold tabular-nums ${
                  active ? "text-emerald-700" : "text-slate-900"
                }`}
              >
                {loading ? "–" : (counts[status] ?? 0)}
              </span>
            </button>
          );
        })}
      </div>

      {/* Result count + sort */}
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm text-slate-500" aria-live="polite">
          {!loading && rows.length > 0 && (
            <>
              Showing <span className="font-medium text-slate-700">{rows.length}</span>
              {rows.length !== counts.All && (
                <>
                  {" "}
                  of <span className="font-medium text-slate-700">{counts.All ?? 0}</span>
                </>
              )}{" "}
              {rows.length === 1 ? "application" : "applications"}
            </>
          )}
        </p>

        <button
          type="button"
          onClick={() => setSort((s) => (s === "newest" ? "oldest" : "newest"))}
          className={`inline-flex items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-3.5 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 ${focusRing}`}
        >
          <ArrowUpDown className="size-4 text-slate-400" aria-hidden="true" />
          {sort === "newest" ? "Newest first" : "Oldest first"}
        </button>
      </div>

      {/* List */}
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
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
            <table className="hidden w-full text-left text-sm md:table">
              <thead className="border-b border-slate-200 bg-slate-50/80 text-xs text-slate-500">
                <tr>
                  <th scope="col" className="px-5 py-3 font-medium">
                    Store
                  </th>
                  <th scope="col" className="px-5 py-3 font-medium">
                    Location
                  </th>
                  <th scope="col" className="px-5 py-3 font-medium">
                    Submitted
                  </th>
                  <th scope="col" className="px-5 py-3 font-medium">
                    Status
                  </th>
                  <th scope="col" className="px-5 py-3 font-medium">
                    Revision
                  </th>
                  <th scope="col" className="px-5 py-3">
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
                      <td className="px-5 py-3.5">
                        <div className="flex items-center gap-3">
                          <Avatar item={item} />
                          <div className="min-w-0">
                            <p className="truncate font-semibold text-slate-900">{item.name}</p>
                            <p className="mt-0.5 max-w-56 truncate text-xs text-slate-500">
                              {item.businessLicenseNo || "No license number"}
                            </p>
                          </div>
                        </div>
                      </td>

                      <td className="max-w-56 px-5 py-3.5 text-slate-600">
                        {place ? (
                          <span className="flex items-start gap-1.5">
                            <MapPin
                              className="mt-0.5 size-3.5 shrink-0 text-slate-400"
                              aria-hidden="true"
                            />
                            <span className="line-clamp-2">{place}</span>
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
                          className={`inline-flex items-center gap-2 rounded-full border py-1.5 pl-3.5 pr-1.5 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50 motion-reduce:transition-none ${
                            opening
                              ? "border-emerald-600 bg-emerald-600 text-white"
                              : "border-emerald-200 bg-emerald-50 text-emerald-800 group-hover:border-emerald-600 group-hover:bg-emerald-600 group-hover:text-white"
                          } ${focusRing}`}
                        >
                          {opening ? "Opening…" : actionLabel(item.status)}
                          <span
                            className={`flex size-6 items-center justify-center rounded-full transition-colors motion-reduce:transition-none ${
                              opening
                                ? "bg-white/20"
                                : "bg-white text-emerald-700 group-hover:bg-white/20 group-hover:text-white"
                            }`}
                          >
                            {opening ? (
                              <Loader2
                                className="size-3.5 animate-spin motion-reduce:animate-none"
                                aria-hidden="true"
                              />
                            ) : (
                              <ArrowRight
                                className="size-3.5 transition-transform group-hover:translate-x-0.5 motion-reduce:transition-none"
                                aria-hidden="true"
                              />
                            )}
                          </span>
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
                    <button
                      type="button"
                      onClick={() => openDetail(item)}
                      disabled={busy}
                      className={`flex w-full items-start gap-3 px-4 py-4 text-left hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-emerald-600 disabled:opacity-60 ${
                        opening ? "bg-emerald-50/60" : ""
                      }`}
                    >
                      <Avatar item={item} />

                      <span className="min-w-0 flex-1">
                        <span className="flex items-start justify-between gap-2">
                          <span className="truncate font-semibold text-slate-900">{item.name}</span>
                          <StatusBadge status={item.status} />
                        </span>

                        <span className="mt-1 flex items-center gap-1 text-xs text-slate-500">
                          <MapPin className="size-3 shrink-0 text-slate-400" aria-hidden="true" />
                          <span className="truncate">{place || "—"}</span>
                        </span>

                        <span className="mt-1 block text-xs text-slate-500">
                          Revision {item.revisionNumber}, {shortDate(item.createdAt)}
                        </span>
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
                    </button>
                  </li>
                );
              })}
            </ul>
          </>
        )}
      </div>
    </div>
  );
}
