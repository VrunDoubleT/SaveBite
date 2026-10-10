import { useEffect, useMemo, useState } from "react";
import {
  ArrowRight,
  Ban,
  CheckCircle2,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Clock,
  ExternalLink,
  FileText,
  History,
  MessageSquareWarning,
  XCircle,
  type LucideIcon,
} from "lucide-react";

import { shopApplicationApi } from "../api/shopApplicationApi";
import type { ShopApplication, ShopApplicationReviewLog, ShopApplicationStatus } from "../types/shopApplication.types";

const PAGE_SIZE = 5;

type ApplicationDocument = ShopApplication["documents"][number];

const STATUS_LABELS: Record<ShopApplicationStatus, string> = {
  Pending: "Pending",
  Approved: "Approved",
  Rejected: "Rejected",
  Cancelled: "Canceled",
};

const STATUS_META: Record<
  ShopApplicationStatus,
  { icon: LucideIcon; badge: string; dot: string }
> = {
  Pending: {
    icon: Clock,
    badge: "bg-amber-50 text-amber-800 ring-amber-200",
    dot: "bg-amber-100 text-amber-700 ring-amber-200",
  },
  Approved: {
    icon: CheckCircle2,
    badge: "bg-emerald-50 text-emerald-800 ring-emerald-200",
    dot: "bg-emerald-100 text-emerald-700 ring-emerald-200",
  },
  Rejected: {
    icon: XCircle,
    badge: "bg-red-50 text-red-800 ring-red-200",
    dot: "bg-red-100 text-red-700 ring-red-200",
  },
  Cancelled: {
    icon: Ban,
    badge: "bg-slate-100 text-slate-700 ring-slate-200",
    dot: "bg-slate-100 text-slate-600 ring-slate-200",
  },
};

type HistoryEntry = {
  application: ShopApplication;
  log: ShopApplicationReviewLog;
};

function dateTime(value: string) {
  return new Date(value).toLocaleString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

function formatCoordinate(value?: number | string | null) {
  if (value === null || value === undefined || value === "") return "—";
  const number = Number(value);
  return Number.isFinite(number) ? number.toFixed(4) : String(value);
}

function valueOrDash(value?: string | number | null) {
  return value === null || value === undefined || value === "" ? "—" : String(value);
}

/**
 * Documents that were effective for a given revision. Documents that were not
 * re-uploaded are inherited from the most recent earlier revision; a replaced
 * document stays available for older history entries.
 */
function getRevisionDocuments(application: ShopApplication, revisionNumber: number): ApplicationDocument[] {
  const latestByType = new Map<ApplicationDocument["documentType"], ApplicationDocument>();

  application.documents
    .filter((document) => document.revisionNumber <= revisionNumber)
    .forEach((document) => {
      const existing = latestByType.get(document.documentType);
      if (
        !existing ||
        document.revisionNumber > existing.revisionNumber ||
        (document.revisionNumber === existing.revisionNumber &&
          application.documents.indexOf(document) > application.documents.indexOf(existing))
      ) {
        latestByType.set(document.documentType, document);
      }
    });

  return Array.from(latestByType.values()).sort((a, b) => a.documentType.localeCompare(b.documentType));
}

function StatusBadge({ status, size = "md" }: { status: ShopApplicationStatus; size?: "sm" | "md" }) {
  const { icon: Icon, badge } = STATUS_META[status];
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full font-semibold ring-1 ring-inset ${badge} ${
        size === "sm" ? "px-2 py-0.5 text-xs" : "px-2.5 py-1 text-xs"
      }`}
    >
      <Icon className="h-3.5 w-3.5" aria-hidden="true" />
      {STATUS_LABELS[status]}
    </span>
  );
}

function InfoRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5 py-2 sm:flex-row sm:items-baseline sm:justify-between sm:gap-4">
      <dt className="shrink-0 text-xs text-slate-500">{label}</dt>
      <dd className="min-w-0 break-words text-sm font-medium text-slate-900 sm:text-right">{children}</dd>
    </div>
  );
}

function InfoCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-lg bg-white px-4 py-3 ring-1 ring-inset ring-slate-200">
      <h5 className="text-sm font-semibold text-slate-900">{title}</h5>
      <dl className="mt-1 divide-y divide-slate-100">{children}</dl>
    </section>
  );
}

function ApplicationDetails({ application, revisionNumber }: { application: ShopApplication; revisionNumber: number }) {
  const address = [application.addressLine, application.ward, application.district, application.city]
    .filter(Boolean)
    .join(", ");
  const documents = getRevisionDocuments(application, revisionNumber);

  return (
    <div className="mt-4 overflow-hidden rounded-xl bg-slate-50 ring-1 ring-inset ring-slate-200">
      {application.coverImageUrl && (
        <img src={application.coverImageUrl} alt="Shop cover" className="h-24 w-full object-cover sm:h-28" />
      )}

      <div className="space-y-3 p-3.5 sm:p-4">
        {/* Identity */}
        <div className="flex items-start gap-3">
          {application.logoUrl && (
            <img
              src={application.logoUrl}
              alt="Shop logo"
              className="h-12 w-12 shrink-0 rounded-lg border border-slate-200 bg-white object-cover"
            />
          )}
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-semibold text-slate-900">{application.name}</p>
            <p className="mt-0.5 text-xs text-slate-500">
              License {valueOrDash(application.businessLicenseNo)}
            </p>
          </div>
          <span className="shrink-0 rounded-md bg-white px-2 py-0.5 text-xs font-medium text-slate-600 ring-1 ring-inset ring-slate-200">
            Revision {revisionNumber}
          </span>
        </div>

        {application.description && (
          <p className="whitespace-pre-wrap text-sm leading-relaxed text-slate-600">{application.description}</p>
        )}

        {/* Info cards */}
        <div className="grid gap-3 md:grid-cols-2">
          <InfoCard title="Location and hours">
            <InfoRow label="Address">{valueOrDash(address)}</InfoRow>
            <InfoRow label="Coordinates">
              {formatCoordinate(application.latitude)}, {formatCoordinate(application.longitude)}
            </InfoRow>
            <InfoRow label="Opening hours">
              {application.openingTime?.slice(0, 5) ?? "—"} – {application.closingTime?.slice(0, 5) ?? "—"}
            </InfoRow>
          </InfoCard>

          <InfoCard title="Payment">
            <InfoRow label="Bank">{valueOrDash(application.bankName)}</InfoRow>
            <InfoRow label="Account holder">{valueOrDash(application.bankAccountHolder)}</InfoRow>
            <InfoRow label="Account number">{valueOrDash(application.bankAccountNumber)}</InfoRow>
            <InfoRow label="PayOS">
              <span
                className={`inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-semibold ring-1 ring-inset ${
                  application.hasPayosConfiguration
                    ? "bg-emerald-50 text-emerald-800 ring-emerald-200"
                    : "bg-slate-100 text-slate-600 ring-slate-200"
                }`}
              >
                <span
                  className={`h-1.5 w-1.5 rounded-full ${application.hasPayosConfiguration ? "bg-emerald-500" : "bg-slate-400"}`}
                  aria-hidden="true"
                />
                {application.hasPayosConfiguration ? "Configured" : "Not configured"}
              </span>
            </InfoRow>
          </InfoCard>
        </div>

        {/* Documents */}
        <section>
          <h5 className="text-sm font-semibold text-slate-900">
            Documents <span className="font-normal text-slate-500">({documents.length})</span>
          </h5>
          {documents.length === 0 ? (
            <p className="mt-1.5 text-sm text-slate-500">No documents were attached to this revision.</p>
          ) : (
            <ul className="mt-2 grid gap-2 sm:grid-cols-2">
              {documents.map((document) => (
                <li key={document.id}>
                  <a
                    href={document.fileUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="group flex items-center gap-2.5 rounded-lg bg-white px-3 py-2 ring-1 ring-inset ring-slate-200 hover:ring-emerald-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600"
                  >
                    <FileText className="h-4 w-4 shrink-0 text-slate-400 group-hover:text-emerald-600" aria-hidden="true" />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-medium text-slate-900">{document.originalFileName}</span>
                      <span className="block text-xs text-slate-500">{document.documentType}</span>
                    </span>
                    <ExternalLink className="h-3.5 w-3.5 shrink-0 text-slate-300 group-hover:text-emerald-600" aria-hidden="true" />
                  </a>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  );
}

function HistorySkeleton() {
  return (
    <div className="space-y-4" role="status" aria-label="Loading status history">
      {[0, 1, 2].map((item) => (
        <div key={item} className="flex gap-4">
          <div className="h-9 w-9 shrink-0 animate-pulse rounded-full bg-slate-100" />
          <div className="flex-1 space-y-2.5 rounded-xl border border-slate-100 p-4">
            <div className="h-4 w-1/3 animate-pulse rounded bg-slate-100" />
            <div className="h-3 w-1/2 animate-pulse rounded bg-slate-100" />
            <div className="h-3 w-1/4 animate-pulse rounded bg-slate-100" />
          </div>
        </div>
      ))}
    </div>
  );
}

export default function ShopApplicationHistory({ refreshKey = 0 }: { refreshKey?: number }) {
  const [applications, setApplications] = useState<ShopApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [expandedEntry, setExpandedEntry] = useState<string | null>(null);
  const [retryKey, setRetryKey] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    shopApplicationApi
      .getHistory()
      .then((result) => {
        if (active) setApplications(result);
      })
      .catch((err: any) => {
        if (active) setError(err?.response?.data?.message || err?.message || "Unable to load status history.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [refreshKey, retryKey]);

  const entries = useMemo<HistoryEntry[]>(
    () =>
      applications
        .flatMap((application) => application.reviewLogs.map((log) => ({ application, log })))
        .sort((a, b) => new Date(b.log.createdAt).getTime() - new Date(a.log.createdAt).getTime()),
    [applications],
  );
  const pageCount = Math.max(1, Math.ceil(entries.length / PAGE_SIZE));
  const pageEntries = entries.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const goToPage = (next: number) => {
    setPage(next);
    setExpandedEntry(null);
  };

  return (
    <section
      className="mt-8 overflow-hidden rounded-2xl border border-slate-200 bg-white"
      aria-labelledby="application-history-title"
    >
      <header className="flex items-start gap-3.5 border-b border-slate-200 px-5 py-5 sm:px-7">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-emerald-600 text-white">
          <History className="h-5 w-5" aria-hidden="true" />
        </span>
        <div className="min-w-0 flex-1">
          <h2 id="application-history-title" className="text-lg font-semibold text-slate-900">
            Application status history
          </h2>
          <p className="mt-0.5 max-w-prose text-sm text-slate-500">
            Every status change on your shop applications, from the first submission to the latest review, with the
            admin's notes.
          </p>
        </div>
        {!loading && !error && entries.length > 0 && (
          <span className="hidden shrink-0 rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-600 sm:inline-block">
            {entries.length} {entries.length === 1 ? "change" : "changes"}
          </span>
        )}
      </header>

      <div className="px-5 py-6 sm:px-7">
        {loading && <HistorySkeleton />}

        {!loading && error && (
          <div role="alert" className="flex flex-col gap-3 rounded-xl border border-red-200 bg-red-50 p-4 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-red-900">{error}</p>
            <button
              type="button"
              onClick={() => setRetryKey((current) => current + 1)}
              className="self-start rounded-lg bg-white px-3 py-1.5 text-sm font-medium text-red-800 ring-1 ring-inset ring-red-200 hover:bg-red-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-600 sm:self-auto"
            >
              Try again
            </button>
          </div>
        )}

        {!loading && !error && entries.length === 0 && (
          <div className="flex flex-col items-center rounded-xl border border-dashed border-slate-300 px-6 py-10 text-center">
            <span className="flex h-11 w-11 items-center justify-center rounded-full bg-slate-100 text-slate-500">
              <History className="h-5 w-5" aria-hidden="true" />
            </span>
            <p className="mt-3 text-sm font-medium text-slate-900">No status changes yet</p>
            <p className="mt-1 max-w-sm text-sm text-slate-500">
              Once you submit a shop application, each review update will show up here.
            </p>
          </div>
        )}

        {!loading && !error && pageEntries.length > 0 && (
          <>
            <ol className="relative">
              {pageEntries.map(({ application, log }, index) => {
                const key = `${application.id}-${log.revisionNumber}-${log.createdAt}-${log.toStatus}`;
                const expanded = expandedEntry === key;
                const isLast = index === pageEntries.length - 1;
                const { icon: StatusIcon, dot } = STATUS_META[log.toStatus];
                const panelId = `details-${key}`;

                return (
                  <li key={key} className="relative flex gap-3.5 sm:gap-4">
                    {/* Timeline rail */}
                    <div className="flex flex-col items-center">
                      <span
                        className={`z-10 flex h-9 w-9 shrink-0 items-center justify-center rounded-full ring-1 ring-inset ${dot}`}
                      >
                        <StatusIcon className="h-[18px] w-[18px]" aria-hidden="true" />
                      </span>
                      {!isLast && <span className="mt-1 w-px flex-1 bg-slate-200" aria-hidden="true" />}
                    </div>

                    <div className={`min-w-0 flex-1 ${isLast ? "" : "pb-6"}`}>
                      <div className="rounded-xl border border-slate-200 p-4 sm:p-5">
                        <div className="flex flex-col gap-1 sm:flex-row sm:items-baseline sm:justify-between sm:gap-4">
                          <p className="min-w-0 truncate text-base font-semibold text-slate-900">{application.name}</p>
                          <time dateTime={log.createdAt} className="shrink-0 text-xs text-slate-500">
                            {dateTime(log.createdAt)}
                          </time>
                        </div>

                        <div className="mt-2.5 flex flex-wrap items-center gap-2">
                          {log.fromStatus ? (
                            <>
                              <StatusBadge status={log.fromStatus} size="sm" />
                              <ArrowRight className="h-3.5 w-3.5 text-slate-400" aria-label="changed to" />
                              <StatusBadge status={log.toStatus} />
                            </>
                          ) : (
                            <>
                              <span className="text-sm text-slate-600">Initial submission</span>
                              <ArrowRight className="h-3.5 w-3.5 text-slate-400" aria-hidden="true" />
                              <StatusBadge status={log.toStatus} />
                            </>
                          )}
                          <span className="ml-auto rounded-md bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
                            Revision {log.revisionNumber}
                          </span>
                        </div>

                        {log.toStatus === "Rejected" && log.note && (
                          <div className="mt-4 flex gap-3 rounded-lg border-l-4 border-red-500 bg-red-50 px-3.5 py-3">
                            <MessageSquareWarning className="mt-0.5 h-4 w-4 shrink-0 text-red-600" aria-hidden="true" />
                            <div className="min-w-0">
                              <p className="text-sm font-semibold text-red-900">Note from admin</p>
                              <p className="mt-1 whitespace-pre-wrap text-sm text-red-900/90">{log.note}</p>
                            </div>
                          </div>
                        )}
                        {log.note && log.toStatus !== "Rejected" && (
                          <p className="mt-3 whitespace-pre-wrap rounded-lg bg-slate-50 px-3.5 py-2.5 text-sm text-slate-700">
                            {log.note}
                          </p>
                        )}

                        <button
                          type="button"
                          aria-expanded={expanded}
                          aria-controls={panelId}
                          onClick={() => setExpandedEntry(expanded ? null : key)}
                          className="mt-4 inline-flex items-center gap-1.5 rounded-md text-sm font-medium text-emerald-700 hover:text-emerald-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 focus-visible:ring-offset-2"
                        >
                          {expanded ? "Hide application details" : "View application details"}
                          <ChevronDown
                            className={`h-4 w-4 transition-transform motion-reduce:transition-none ${expanded ? "rotate-180" : ""}`}
                            aria-hidden="true"
                          />
                        </button>

                        {expanded && (
                          <div id={panelId}>
                            <ApplicationDetails application={application} revisionNumber={log.revisionNumber} />
                          </div>
                        )}
                      </div>
                    </div>
                  </li>
                );
              })}
            </ol>

            <nav
              aria-label="Status history pages"
              className="mt-6 flex flex-col gap-3 border-t border-slate-200 pt-4 sm:flex-row sm:items-center sm:justify-between"
            >
              <p className="text-sm text-slate-500">
                Showing {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, entries.length)} of {entries.length}{" "}
                status changes
              </p>
              <div className="flex items-center justify-between gap-2 sm:justify-end">
                <button
                  type="button"
                  disabled={page <= 1}
                  onClick={() => goToPage(page - 1)}
                  className="inline-flex items-center gap-1 rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-transparent"
                >
                  <ChevronLeft className="h-4 w-4" aria-hidden="true" />
                  Previous
                </button>
                <span className="min-w-16 text-center text-sm tabular-nums text-slate-600" aria-live="polite">
                  {page} / {pageCount}
                </span>
                <button
                  type="button"
                  disabled={page >= pageCount}
                  onClick={() => goToPage(page + 1)}
                  className="inline-flex items-center gap-1 rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-transparent"
                >
                  Next
                  <ChevronRight className="h-4 w-4" aria-hidden="true" />
                </button>
              </div>
            </nav>
          </>
        )}
      </div>
    </section>
  );
}