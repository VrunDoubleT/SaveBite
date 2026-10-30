import { useCallback, useEffect, useState } from "react";
import { AlertCircle, RotateCw, ShieldCheck, TriangleAlert } from "lucide-react";
import { warningApi } from "../api/warningApi";
import type { Warning } from "../types/warning.types";
import { Pagination } from "@/shared/components/pagination";
import { WarningItems } from "../components/WarningItems";

const PAGE_SIZE = 5;

const solidPill =
  "inline-flex items-center justify-center gap-2 rounded-full bg-primary-700 px-5 py-2.5 text-[13px] font-medium text-white shadow-sm transition-all hover:bg-primary-700/90 hover:shadow-md active:scale-[0.98] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700";

function ListSkeleton() {
  return (
    <div role="status" aria-live="polite" className="space-y-8 motion-safe:animate-pulse">
      <span className="sr-only">Loading warnings…</span>
      {Array.from({ length: 2 }).map((_, g) => (
        <div key={g} className="space-y-3">
          <div className="h-4 w-28 rounded bg-neutral-100" />
          <div className="space-y-3 pl-6">
            {Array.from({ length: 2 }).map((__, i) => (
              <div
                key={i}
                className="flex items-center gap-4 rounded-3xl border border-border-default bg-white p-4"
              >
                <div className="size-11 rounded-2xl bg-neutral-100" />
                <div className="flex-1 space-y-2">
                  <div className="h-4 w-2/5 rounded bg-neutral-100" />
                  <div className="h-3 w-1/4 rounded bg-neutral-100" />
                </div>
                <div className="h-7 w-16 rounded-full bg-neutral-100" />
                <div className="size-8 rounded-full bg-neutral-100" />
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
  const [items, setItems] = useState<Warning[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    void warningApi
      .history(page, PAGE_SIZE, controller.signal)
      .then((data) => {
        if (controller.signal.aborted) return;
        setItems(data?.items ?? []);
        setTotalItems(data?.totalItems ?? 0);
        setTotalPages(Math.max(1, data?.totalPages ?? 1));
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setError("We couldn't load your warnings. Check your connection and try again.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [page, reloadKey]);

  const retry = useCallback(() => setReloadKey((k) => k + 1), []);
  const hasWarnings = !loading && !error && totalItems > 0;

  return (
    <div className="w-full space-y-8 antialiased">
      <header className="relative overflow-hidden rounded-3xl border border-border-default bg-white p-6 shadow-[0_1px_2px_rgba(16,24,40,0.04)] sm:p-7">
        <span aria-hidden="true" className="absolute inset-y-0 left-0 w-1.5 bg-amber-400" />
        <div className="flex flex-wrap items-center justify-between gap-5 pl-2">
          <div className="flex min-w-0 items-start gap-4">
            <span className="flex size-12 shrink-0 items-center justify-center rounded-2xl bg-amber-50 text-amber-600 ring-1 ring-inset ring-amber-100">
              <TriangleAlert className="size-6" aria-hidden="true" />
            </span>
            <div className="min-w-0 space-y-1">
              <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
                Warning History
              </h1>
              <p className="max-w-xl text-sm leading-6 text-text-secondary">
                Products in your shop that received a warning. Open one to see why and how to fix
                it.
              </p>
            </div>
          </div>

          {hasWarnings && (
            <div className="rounded-2xl bg-amber-50 px-5 py-3 text-center ring-1 ring-inset ring-amber-100">
              <p className="text-2xl font-semibold tabular-nums leading-none text-amber-800">
                {totalItems}
              </p>
              <p className="mt-1 text-xs font-medium text-amber-700">
                {totalItems === 1 ? "warning" : "warnings"} in total
              </p>
            </div>
          )}
        </div>
      </header>

      <div aria-busy={loading}>
        {loading ? (
          <ListSkeleton />
        ) : error ? (
          <div
            role="alert"
            className="flex flex-col items-center gap-3 rounded-3xl border border-red-200 bg-white px-6 py-16 text-center"
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
          <div className="flex flex-col items-center gap-3 rounded-3xl border border-dashed border-border-default bg-white px-6 py-20 text-center">
            <span className="flex size-16 items-center justify-center rounded-full bg-green-50 text-green-600 ring-8 ring-green-50/50">
              <ShieldCheck className="size-8" aria-hidden="true" />
            </span>
            <p className="mt-2 text-lg font-semibold text-text-primary">No warnings yet</p>
            <p className="max-w-sm text-sm leading-6 text-text-secondary">
              Your shop is in good standing. When a warning is issued, it will appear here.
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
