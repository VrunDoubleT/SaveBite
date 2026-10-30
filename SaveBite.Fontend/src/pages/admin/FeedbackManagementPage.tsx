import { useCallback, useEffect, useState, type ReactNode } from "react";
import { AlertCircle, ChevronDown, Inbox, RotateCw, SearchX, X } from "lucide-react";
import { feedbackApi } from "@/features/feedback/api/feedbackApi";
import { FeedbackModerationDialog } from "@/features/feedback/components/FeedbackModerationDialog";
import { FeedbackTable } from "@/features/feedback/components/FeedbackTable";
import type { Feedback, FeedbackPage } from "@/features/feedback/types/feedback.types";
import { Pagination } from "@/shared/components/pagination";
import { SearchInput } from "@/shared/components/modal/SearchInput";

const PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 400;

const getStatusCode = (status: Feedback["status"]): number => {
  if (typeof status === "number") {
    return status;
  }

  switch (status) {
    case "Visible":
      return 1;
    case "HiddenByCustomer":
      return 2;
    case "HiddenByAdmin":
      return 3;
    default:
      return 0;
  }
};

const focusRing =
  "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700";

/* ---------- Loading / state components (same pattern as ShopManagementPage) ---------- */

function ListSkeleton() {
  return (
    <div
      role="status"
      aria-busy="true"
      aria-label="Loading feedback"
      className="overflow-hidden rounded-3xl border border-border-default bg-white motion-safe:animate-pulse"
    >
      <span className="sr-only">Loading feedback…</span>
      <div className="h-11 bg-neutral-50" />
      <div className="divide-y divide-border-default">
        {Array.from({ length: 5 }, (_, i) => (
          <div key={i} className="flex items-center gap-4 px-6 py-4">
            <div className="size-10 shrink-0 rounded-full bg-neutral-100" />
            <div className="min-w-0 flex-1 space-y-2">
              <div className="h-4 w-2/5 rounded bg-neutral-100" />
              <div className="h-3 w-1/4 rounded bg-neutral-100" />
            </div>
            <div className="hidden h-4 w-32 rounded bg-neutral-100 md:block" />
            <div className="hidden h-6 w-24 rounded-full bg-neutral-100 sm:block" />
          </div>
        ))}
      </div>
    </div>
  );
}

function StateMessage({
  icon: Icon,
  title,
  description,
  tone = "neutral",
  role,
  actions,
}: {
  icon: typeof AlertCircle;
  title: string;
  description?: string;
  tone?: "neutral" | "error";
  role?: "alert" | "status";
  actions?: ReactNode;
}) {
  const isError = tone === "error";

  return (
    <div
      role={role}
      className={`flex flex-col items-center gap-3 rounded-3xl border bg-white px-6 py-20 text-center ${
        isError ? "border-red-200" : "border-border-default"
      }`}
    >
      <span
        className={`flex size-14 items-center justify-center rounded-2xl ${
          isError ? "bg-red-50 text-red-600" : "bg-neutral-100 text-text-muted"
        }`}
      >
        <Icon className="size-7" aria-hidden="true" />
      </span>
      <p
        className={`text-lg font-semibold tracking-tight ${
          isError ? "text-red-700" : "text-text-primary"
        }`}
      >
        {title}
      </p>
      {description && <p className="max-w-sm text-sm text-text-secondary">{description}</p>}
      {actions && <div className="mt-2">{actions}</div>}
    </div>
  );
}

/* ---------- Page ---------- */

export function FeedbackManagementPage() {
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");

  const [data, setData] = useState<FeedbackPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const [selected, setSelected] = useState<Feedback | null>(null);

  const retry = useCallback(() => setReloadKey((key) => key + 1), []);

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, SEARCH_DEBOUNCE_MS);

    return () => clearTimeout(timer);
  }, [searchInput]);

  // Load data (ignores stale responses)
  useEffect(() => {
    let cancelled = false;

    setLoading(true);
    setError(null);

    feedbackApi
      .list({
        page,
        pageSize: PAGE_SIZE,
        search: search || undefined,
        status: status ? Number(status) : undefined,
      })
      .then((result) => {
        if (!cancelled) setData(result);
      })
      .catch(() => {
        if (!cancelled) setError("Unable to load feedback.");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [page, search, status, reloadKey]);

  const items = data?.items ?? [];
  const totalPages = Math.max(1, data?.totalPages ?? 1);
  const totalItems = data?.totalItems ?? 0;
  const isInitialLoading = loading && !data;
  const hasFilters = search !== "" || status !== "";

  const clearFilters = () => {
    setSearchInput("");
    setSearch("");
    setStatus("");
    setPage(1);
  };

  /* ---------- Moderation ---------- */

  const moderationMode = selected && getStatusCode(selected.status) === 3 ? "restore" : "hide";

  function openModeration(item: Feedback) {
    const itemStatus = getStatusCode(item.status);
    if (itemStatus !== 1 && itemStatus !== 3) return;
    setSelected(item);
  }

  const closeModeration = useCallback(() => setSelected(null), []);

  async function confirmModeration(reason: string) {
    if (!selected) return;

    if (moderationMode === "restore") {
      await feedbackApi.restore(selected.id, reason || undefined);
    } else {
      await feedbackApi.remove(selected.id, reason);
    }

    setSelected(null);
    retry();
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-col justify-between gap-4 border-b border-border-default pb-5 lg:flex-row lg:items-end">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold tracking-tight text-text-primary">
              Feedback Moderation
            </h1>
            {!isInitialLoading && !error && (
              <span className="rounded-full bg-neutral-100 px-2.5 py-0.5 text-sm font-semibold tabular-nums text-text-secondary">
                {totalItems.toLocaleString("vi-VN")}
              </span>
            )}
          </div>
          <p className="text-sm text-text-secondary">
            Review customer feedback, hide content that breaks the rules and restore it when needed.
          </p>
        </div>

        <div className="flex w-full flex-col gap-3 sm:flex-row sm:items-center lg:w-auto">
          <div className="relative">
            <select
              aria-label="Feedback status"
              className={`h-11 w-full appearance-none rounded-full border border-border-default bg-white pl-4 pr-10 text-sm text-text-primary transition-colors hover:border-neutral-300 sm:w-48 ${focusRing}`}
              value={status}
              onChange={(event) => {
                setPage(1);
                setStatus(event.target.value);
              }}
            >
              <option value="">All statuses</option>
              <option value="1">Visible</option>
              <option value="2">Hidden by customer</option>
              <option value="3">Hidden by admin</option>
            </select>
            <ChevronDown
              className="pointer-events-none absolute right-4 top-1/2 size-4 -translate-y-1/2 text-text-muted"
              aria-hidden="true"
            />
          </div>

          <SearchInput
            value={searchInput}
            onChange={setSearchInput}
            placeholder="Search feedback, product or shop..."
            className="w-full sm:w-80"
          />
        </div>
      </header>

      {isInitialLoading ? (
        <ListSkeleton />
      ) : error ? (
        <StateMessage
          role="alert"
          tone="error"
          icon={AlertCircle}
          title={error}
          description="Check your connection, then try again."
          actions={
            <button
              type="button"
              onClick={retry}
              className="inline-flex items-center gap-2 rounded-xl bg-primary-700 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2"
            >
              <RotateCw className="size-4" aria-hidden="true" />
              Try again
            </button>
          }
        />
      ) : items.length === 0 ? (
        hasFilters ? (
          <StateMessage
            icon={SearchX}
            title="No matching feedback"
            description="Try a different search term or status filter."
            actions={
              <button
                type="button"
                onClick={clearFilters}
                className="inline-flex items-center gap-2 rounded-xl border border-border-default bg-white px-4 py-2 text-sm font-semibold text-text-primary transition-colors hover:bg-neutral-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2"
              >
                <X className="size-4" aria-hidden="true" />
                Clear filters
              </button>
            }
          />
        ) : (
          <StateMessage
            icon={Inbox}
            title="No feedback yet"
            description="Customer feedback will appear here for review."
          />
        )
      ) : (
        <div
          className={`space-y-5 transition-opacity ${loading ? "opacity-60" : "opacity-100"}`}
          aria-busy={loading}
        >
          <FeedbackTable items={items} onModerate={openModeration} />

          <Pagination
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            totalItems={totalItems}
            pageSize={PAGE_SIZE}
            itemLabel="feedback"
            showFirstLast
          />
        </div>
      )}

      {selected && (
        <FeedbackModerationDialog
          key={selected.id}
          feedback={selected}
          mode={moderationMode}
          onClose={closeModeration}
          onConfirm={confirmModeration}
        />
      )}
    </div>
  );
}
