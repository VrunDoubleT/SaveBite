
import { useCallback, useEffect, useState, type ReactNode } from "react";
import { useSearchParams } from "react-router-dom";
import {
  AlertCircle,
  ChevronDown,
  Inbox,
  RotateCw,
  SearchX,
  X,
  MessageSquare,
  Eye,
  EyeOff,
  ShieldAlert,
} from "lucide-react";

import { feedbackApi } from "@/features/feedback/api/feedbackApi";
import { FeedbackDetailsDialog } from "@/features/feedback/components/FeedbackDetailsDialog";
import { FeedbackModerationDialog } from "@/features/feedback/components/FeedbackModerationDialog";
import { FeedbackTable } from "@/features/feedback/components/FeedbackTable";
import type {
  Feedback,
  FeedbackPage,
  FeedbackStatus,
} from "@/features/feedback/types/feedback.types";
import { Pagination } from "@/shared/components/pagination";
import { SearchInput } from "@/shared/components/modal/SearchInput";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api";
import { emitFeedbackSync } from "@/features/feedbacks/utils/feedbackSync";
import { useDebounce } from "@/shared/hooks/useDebounce";

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
          isError
            ? "bg-red-50 text-red-600"
            : "bg-neutral-100 text-text-muted"
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

      {description && (
        <p className="max-w-sm text-sm text-text-secondary">
          {description}
        </p>
      )}

      {actions && <div className="mt-2">{actions}</div>}
    </div>
  );
}

export function FeedbackManagementPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  // URL is the source of truth for pagination and filters.
  const requestedPage = Number(searchParams.get("page") ?? 1);

  const page =
    Number.isInteger(requestedPage) && requestedPage > 0
      ? requestedPage
      : 1;

  const search = searchParams.get("search") ?? "";

  const requestedStatus = searchParams.get("status") ?? "";

  const status = ["1", "2", "3"].includes(requestedStatus)
    ? requestedStatus
    : "";

  const [searchInput, setSearchInput] = useState(search);

  const debouncedSearch = useDebounce(
    searchInput,
    SEARCH_DEBOUNCE_MS,
  );

  const [data, setData] = useState<FeedbackPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const [selected, setSelected] = useState<Feedback | null>(null);
  const [detailsFeedback, setDetailsFeedback] =
    useState<Feedback | null>(null);

  // Summary counts state.
  const [counts, setCounts] = useState({
    total: 0,
    visible: 0,
    hiddenByCustomer: 0,
    hiddenByAdmin: 0,
  });

  const [loadingStats, setLoadingStats] = useState(true);

  // Sync search input when URL changes.
  useEffect(() => {
    setSearchInput(search);
  }, [search]);

  // Debounce search and synchronize it with URL.
  useEffect(() => {
    const value = debouncedSearch.trim();

    if (value === search) return;

    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current);

        if (value) {
          next.set("search", value);
        } else {
          next.delete("search");
        }

        next.delete("page");

        return next;
      },
      { preventScrollReset: true },
    );
  }, [debouncedSearch, search, setSearchParams]);

  // Retry handler.
  const retry = useCallback(() => {
    setLoading(true);
    setError(null);
    setLoadingStats(true);
    setReloadKey((key) => key + 1);
  }, []);

  // Load summary statistics.
  useEffect(() => {
    let cancelled = false;

    Promise.all([
      feedbackApi.list({ page: 1, pageSize: 1 }),
      feedbackApi.list({ page: 1, pageSize: 1, status: 1 }),
      feedbackApi.list({ page: 1, pageSize: 1, status: 2 }),
      feedbackApi.list({ page: 1, pageSize: 1, status: 3 }),
    ])
      .then(([allRes, visibleRes, customerHiddenRes, adminHiddenRes]) => {
        if (!cancelled) {
          setCounts({
            total: allRes?.totalItems ?? 0,
            visible: visibleRes?.totalItems ?? 0,
            hiddenByCustomer: customerHiddenRes?.totalItems ?? 0,
            hiddenByAdmin: adminHiddenRes?.totalItems ?? 0,
          });

          setLoadingStats(false);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setLoadingStats(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  // Load feedback data.
  useEffect(() => {
    let cancelled = false;

    feedbackApi
      .list({
        page,
        pageSize: PAGE_SIZE,
        search: search.trim() || undefined,
        status: status ? Number(status) : undefined,
      })
      .then((result) => {
        if (!cancelled) {
          setData(result);
          setLoading(false);
          setError(null);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError("Unable to load feedback.");
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [page, search, status, reloadKey]);

  const items = data?.items ?? [];
  const totalPages = Math.max(1, data?.totalPages ?? 1);
  const totalItems = data?.totalItems ?? 0;

  const isInitialLoading = loading && !data;

  const hasFilters =
    search.trim() !== "" || status !== "";

  // Correct page if it exceeds the available page count.
  useEffect(() => {
    if (!loading && data && page > totalPages) {
      setSearchParams(
        (current) => {
          const next = new URLSearchParams(current);

          if (totalPages <= 1) {
            next.delete("page");
          } else {
            next.set("page", String(totalPages));
          }

          return next;
        },
        { replace: true, preventScrollReset: true },
      );
    }
  }, [data, loading, page, totalPages, setSearchParams]);

  // Change status filter.
  const handleStatusChange = (nextStatus: string) => {
    setLoading(true);

    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current);

        if (nextStatus) {
          next.set("status", nextStatus);
        } else {
          next.delete("status");
        }

        next.delete("page");

        return next;
      },
      { preventScrollReset: true },
    );
  };

  // Change pagination.
  const handlePageChange = (nextPage: number) => {
    setLoading(true);

    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current);
        next.set("page", String(nextPage));

        return next;
      },
      { preventScrollReset: true },
    );
  };

  // Clear all filters.
  const clearFilters = () => {
    setLoading(true);
    setSearchInput("");

    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current);

        next.delete("search");
        next.delete("status");
        next.delete("page");

        return next;
      },
      { preventScrollReset: true },
    );
  };

  const moderationMode =
    selected && getStatusCode(selected.status) === 3
      ? "restore"
      : "hide";

  function openModeration(item: Feedback) {
    const itemStatus = getStatusCode(item.status);

    if (itemStatus !== 1 && itemStatus !== 3) return;

    setSelected(item);
  }

  const closeModeration = useCallback(() => {
    setSelected(null);
  }, []);

  async function confirmModeration(reason: string) {
    if (!selected) return;

    const targetFeedback = selected;

    const isRestoring = moderationMode === "restore";

    const nextStatus: FeedbackStatus = isRestoring
      ? "Visible"
      : "HiddenByAdmin";

    try {
      if (isRestoring) {
        await feedbackApi.restore(
          targetFeedback.id,
          reason || undefined,
        );

        toast.success("Feedback restored successfully.");
      } else {
        await feedbackApi.remove(targetFeedback.id, reason);

        toast.success("Feedback removed successfully.");
      }

      // Optimistically update status.
      setData((prev) => {
        if (!prev) return prev;

        return {
          ...prev,
          items: prev.items.map((item) =>
            item.id === targetFeedback.id
              ? {
                  ...item,
                  status: nextStatus,
                  currentReason: !isRestoring
                    ? reason
                    : item.currentReason,
                }
              : item,
          ),
        };
      });

      emitFeedbackSync(
        isRestoring ? "restore" : "hide",
        targetFeedback.id,
        {
          shopId: targetFeedback.shopId,
          productId: targetFeedback.productId,
          rating: targetFeedback.rating,
        },
      );

      setSelected(null);

      retry();
    } catch (error) {
      toast.error(
        getApiErrorMessage(error) ||
          "Failed to update feedback.",
      );
    }
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight text-neutral-900">
              Feedback Moderation
            </h1>
          </div>

          <p className="text-sm text-text-secondary">
            Review customer feedback, hide content that breaks the
            rules and restore it when needed.
          </p>
        </div>

        <div className="flex w-full flex-col gap-3 sm:flex-row sm:items-center lg:w-auto">
          <div className="relative">
            <select
              aria-label="Feedback status"
              className={`h-10 w-full appearance-none rounded-lg border border-neutral-200 bg-white pl-4 pr-10 text-sm text-neutral-900 outline-none transition focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20 sm:w-48 ${focusRing}`}
              value={status}
              onChange={(event) =>
                handleStatusChange(event.target.value)
              }
            >
              <option value="">All statuses</option>
              <option value="1">Visible</option>
              <option value="2">Hidden by customer</option>
              <option value="3">Hidden by admin</option>
            </select>

            <ChevronDown
              className="pointer-events-none absolute right-4 top-1/2 size-4 -translate-y-1/2 text-neutral-400"
              aria-hidden="true"
            />
          </div>

          <SearchInput
            value={searchInput}
            onChange={(val) => {
              setSearchInput(val);

              if (val.trim() !== search.trim()) {
                setLoading(true);
              }
            }}
            placeholder="Search feedback, product or shop..."
            className="w-full sm:w-80"
          />
        </div>
      </header>

      {/* Overview Statistics Cards */}
      <div
        className="grid grid-cols-2 gap-3 sm:grid-cols-4 sm:gap-4"
        role="region"
        aria-label="Feedback summary metrics"
      >
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-neutral-100 text-neutral-600">
              <MessageSquare className="size-5" />
            </span>

            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">
                All Feedback
              </p>

              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.total}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
              <Eye className="size-5" />
            </span>

            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">
                Visible
              </p>

              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.visible}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-500">
              <EyeOff className="size-5" />
            </span>

            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">
                Hidden by Customer
              </p>

              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.hiddenByCustomer}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
          <div className="flex items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-rose-50 text-rose-600">
              <ShieldAlert className="size-5" />
            </span>

            <div className="min-w-0">
              <p className="text-xs font-medium text-slate-500 truncate">
                Hidden by Admin
              </p>

              <p className="mt-0.5 text-xl font-bold tabular-nums text-slate-900">
                {loadingStats ? "–" : counts.hiddenByAdmin}
              </p>
            </div>
          </div>
        </div>
      </div>

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
              <RotateCw
                className="size-4"
                aria-hidden="true"
              />
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
          className={`overflow-hidden rounded-lg border border-border-default bg-bg-surface shadow-sm transition-opacity ${
            loading ? "opacity-60" : "opacity-100"
          }`}
          aria-busy={loading}
        >
          <FeedbackTable
            items={items}
            onViewDetails={setDetailsFeedback}
          />

          {totalItems > 0 && (
            <div className="flex w-full items-center justify-around border-t border-border-default bg-neutral-50/30 px-6 py-5">
              <Pagination
                currentPage={page}
                totalPages={totalPages}
                onPageChange={handlePageChange}
                totalItems={totalItems}
                pageSize={PAGE_SIZE}
                itemLabel="feedback"
                showTotalItems={true}
                showFirstLast={true}
                className="w-full"
              />
            </div>
          )}
        </div>
      )}

      <FeedbackDetailsDialog
        feedback={detailsFeedback}
        onClose={() => setDetailsFeedback(null)}
        onModerate={(feedback) => {
          setDetailsFeedback(null);
          openModeration(feedback);
        }}
      />

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