import { useCallback, useEffect, useState } from "react";
import { AlertCircle, RotateCw, ShieldCheck, TriangleAlert } from "lucide-react";
import { warningApi } from "../api/warningApi";
import type { Warning } from "../types/warning.types";
import { Pagination } from "@/shared/components/pagination";
import { WarningItems } from "../components/WarningItems";

const PAGE_SIZE = 5;

const solidPill =
  "inline-flex items-center justify-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-emerald-700 active:scale-[0.98]";

function ListSkeleton() {
  return (
    <div role="status" aria-live="polite" className="space-y-6 motion-safe:animate-pulse">
      <span className="sr-only">Loading warnings…</span>
      {Array.from({ length: 2 }).map((_, g) => (
        <div key={g} className="space-y-3">
          <div className="h-4 w-28 rounded bg-neutral-100" />
          <div className="space-y-3">
            {Array.from({ length: 2 }).map((__, i) => (
              <div
                key={i}
                className="flex items-center gap-4 rounded-xl border border-neutral-200 bg-white p-4 shadow-2xs"
              >
                <div className="size-11 rounded-lg bg-neutral-100" />
                <div className="flex-1 space-y-2">
                  <div className="h-4 w-2/5 rounded bg-neutral-100" />
                  <div className="h-3 w-1/4 rounded bg-neutral-100" />
                </div>
                <div className="h-7 w-16 rounded-full bg-neutral-100" />
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

export function WarningHistoryPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("all");
  const [reloadKey, setReloadKey] = useState(0);
  const requestKey = `${status}:${page}:${reloadKey}`;
  const [result, setResult] = useState<{
    key: string;
    items: Warning[];
    totalItems: number;
    totalPages: number;
    error: string | null;
  }>({ key: "", items: [], totalItems: 0, totalPages: 1, error: null });
  const loading = result.key !== requestKey;
  const { items, totalItems, totalPages } = loading
    ? { items: [], totalItems: 0, totalPages: 1 }
    : result;
  const error = loading ? null : result.error;

  useEffect(() => {
    const controller = new AbortController();
    void warningApi
      .history(page, PAGE_SIZE, controller.signal, status)
      .then((data) => {
        if (!controller.signal.aborted)
          setResult({
            key: requestKey,
            items: data.items ?? [],
            totalItems: data.totalItems ?? 0,
            totalPages: Math.max(1, data.totalPages ?? 1),
            error: null,
          });
      })
      .catch(() => {
        if (!controller.signal.aborted)
          setResult({
            key: requestKey,
            items: [],
            totalItems: 0,
            totalPages: 1,
            error: "We couldn't load your warnings. Check your connection and try again.",
          });
      });
    return () => controller.abort();
  }, [page, requestKey, status]);

  const retry = useCallback(() => setReloadKey((k) => k + 1), []);

  const needsActionCount = items.filter((w) => !w.resolvedAt && !w.firstEditedAt).length;
  const submittedCount = items.filter((w) => !w.resolvedAt && Boolean(w.firstEditedAt)).length;
  const resolvedCount = items.filter((w) => Boolean(w.resolvedAt)).length;

  return (
    <div className="w-full space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">
            Warning History
          </h1>
          <p className="mt-1 text-sm text-neutral-500">
            Track warnings that need action, corrections you have submitted, and warnings approved by the administrator.
          </p>
        </div>
        <button
          type="button"
          disabled={loading}
          onClick={retry}
          className="inline-flex items-center gap-2 self-start rounded-lg border border-neutral-200 bg-white px-4 py-2.5 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 disabled:opacity-50"
        >
          <RotateCw className={`size-3.5 ${loading ? "animate-spin" : ""}`} />
          Refresh
        </button>
      </div>

      {/* 4 Stats Cards */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 sm:gap-4">
        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <TriangleAlert className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Total warnings</p>
              <p className="text-xl font-bold text-neutral-900">{totalItems}</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-amber-50 text-amber-600">
              <AlertCircle className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Needs action</p>
              <p className="text-xl font-bold text-amber-600">{needsActionCount}</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-blue-50 text-blue-600">
              <RotateCw className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Submitted</p>
              <p className="text-xl font-bold text-blue-600">{submittedCount}</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-neutral-100 bg-white p-4 shadow-2xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
              <ShieldCheck className="size-5" />
            </span>
            <div>
              <p className="text-xs font-medium text-neutral-500">Resolved</p>
              <p className="text-xl font-bold text-emerald-600">{resolvedCount}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-2">
        {[
          ["all", "All warnings"],
          ["new", "Needs your action"],
          ["submitted", "Correction submitted"],
          ["resolved", "Resolved"],
        ].map(([value, label]) => (
          <button
            key={value}
            type="button"
            aria-pressed={status === value}
            onClick={() => {
              setPage(1);
              setStatus(value);
            }}
            className={`rounded-lg px-3.5 py-2 text-xs font-semibold transition shadow-2xs ${
              status === value
                ? "bg-emerald-600 text-white shadow-sm"
                : "border border-neutral-200 bg-white text-neutral-700 hover:bg-neutral-50"
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      <div aria-busy={loading}>
        {loading ? (
          <ListSkeleton />
        ) : error ? (
          <div
            role="alert"
            className="flex flex-col items-center gap-3 rounded-xl border border-red-200 bg-white px-6 py-16 text-center shadow-2xs"
          >
            <span className="flex size-14 items-center justify-center rounded-full bg-red-50 text-red-600">
              <AlertCircle className="size-7" aria-hidden="true" />
            </span>
            <p className="max-w-md text-lg font-semibold text-red-700">{error}</p>
            <button type="button" onClick={retry} className={`mt-2 ${solidPill}`}>
              <RotateCw className="size-4" aria-hidden="true" />
              Try again
            </button>
          </div>
        ) : items.length === 0 ? (
          <div className="flex flex-col items-center gap-3 rounded-xl border border-dashed border-neutral-200 bg-white px-6 py-16 text-center shadow-2xs">
            <span className="flex size-16 items-center justify-center rounded-full bg-emerald-50 text-emerald-600 ring-8 ring-emerald-50/50">
              <ShieldCheck className="size-8" aria-hidden="true" />
            </span>
            <p className="mt-2 text-lg font-semibold text-neutral-900">
              {status === "all" ? "No warnings yet" : "No warnings in this status"}
            </p>
            <p className="max-w-sm text-sm leading-6 text-neutral-500">
              {status === "all"
                ? "Warnings from the administrator will appear here."
                : "Choose another status to view your other warnings."}
            </p>
          </div>
        ) : (
          <WarningItems items={items} />
        )}
      </div>

      {!loading && !error && items.length > 0 && (
        <Pagination
          currentPage={page}
          totalPages={totalPages}
          onPageChange={setPage}
          totalItems={totalItems}
          pageSize={PAGE_SIZE}
          itemLabel="warnings"
          showFirstLast
        />
      )}
    </div>
  );
}

export default WarningHistoryPage;
