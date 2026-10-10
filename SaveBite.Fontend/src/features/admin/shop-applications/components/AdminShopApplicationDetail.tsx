import type { ShopApplication } from "../types/adminShopApplication.types";
import { ArrowLeft, CheckCircle2, FileText, ExternalLink, Loader2, XCircle } from "lucide-react";
import {
  formatCoordinate,
  dateTime,
  getStatusMeta,
  focusRing,
  NOTE_MAX_LENGTH,
} from "../schemas/adminShopApplication.schema";
import { Notice, Panel, InfoRow, StatusBadge } from "./AdminShopApplicationShared";

type Props = {
  app: ShopApplication;
  error: string;
  success: string;
  note: string;
  saving: boolean;
  onBack: () => void;
  onNoteChange: (value: string) => void;
  onReview: (decision: "Approved" | "Rejected") => void;
};

export function AdminShopApplicationDetail({
  app,
  error,
  success,
  note,
  saving,
  onBack,
  onNoteChange,
  onReview,
}: Props) {
  const address = [app.addressLine, app.ward, app.district, app.city].filter(Boolean).join(", ");
  const currentDocuments = app.documents.filter((document) => document.isCurrent);
  const logs = [...app.reviewLogs].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );
  const isPending = app.status === "Pending";
  const finalMeta = getStatusMeta(app.status);
  return (
    <div className="mx-auto max-w-6xl space-y-5 p-4 sm:p-6">
      <button
        onClick={() => {
          onBack();
        }}

        className={`inline-flex items-center gap-2 rounded-md text-sm font-medium text-slate-600 hover:text-slate-900 ${focusRing}`}
      >
        <ArrowLeft className="size-4" aria-hidden="true" />
        Back to applications
      </button>

      {/* Identity header */}
      <header className="overflow-hidden rounded-xl border border-slate-200 bg-white">
        {app.coverImageUrl ? (
          <img
            src={app.coverImageUrl}
            alt={`${app.name} cover`}
            className="h-32 w-full object-cover sm:h-44"
          />
        ) : (
          <div className="h-16 w-full bg-slate-100 sm:h-20" aria-hidden="true" />
        )}

        <div className="flex flex-wrap items-end justify-between gap-4 px-5 pb-5">
          <div className="-mt-8 flex min-w-0 items-end gap-4">
            {app.logoUrl ? (
              <img
                src={app.logoUrl}

                alt={`${app.name} logo`}

                className="size-16 shrink-0 rounded-xl border-2 border-white bg-white object-cover shadow-sm sm:size-20"
              />
            ) : (
              <span className="flex size-16 shrink-0 items-center justify-center rounded-xl border-2 border-white bg-slate-200 text-xl font-semibold text-slate-600 shadow-sm sm:size-20">
                {app.name.slice(0, 1).toUpperCase()}
              </span>
            )}

            <div className="min-w-0 pb-0.5">
              <h1 className="truncate text-xl font-semibold text-slate-900 sm:text-2xl">
                {app.name}
              </h1>

              <p className="mt-0.5 text-sm text-slate-500">
                Revision {app.revisionNumber}, submitted {dateTime(app.createdAt)}
              </p>
            </div>
          </div>

          <StatusBadge status={app.status} />
        </div>
      </header>

      {error && <Notice tone="error">{error}</Notice>}

      {success && <Notice tone="success">{success}</Notice>}

      <div className="grid gap-5 lg:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
        {/* Left: application content */}
        <div className="space-y-5">
          <Panel title="Application details">
            {app.description && (
              <p className="mb-2 whitespace-pre-wrap border-b border-slate-100 pb-4 text-sm leading-relaxed text-slate-600">
                {app.description}
              </p>
            )}

            <dl className="divide-y divide-slate-100">
              <InfoRow label="Business license number">{app.businessLicenseNo || "—"}</InfoRow>

              <InfoRow label="Address">{address || "—"}</InfoRow>

              <InfoRow label="Coordinates">
                {formatCoordinate(app.latitude)}, {formatCoordinate(app.longitude)}
              </InfoRow>

              <InfoRow label="Opening hours">
                {app.openingTime?.slice(0, 5) ?? "—"} – {app.closingTime?.slice(0, 5) ?? "—"}
              </InfoRow>
            </dl>
          </Panel>

          <Panel title="Payment">
            <dl className="divide-y divide-slate-100">
              <InfoRow label="Bank">{app.bankName || "—"}</InfoRow>

              <InfoRow label="Account holder">{app.bankAccountHolder || "—"}</InfoRow>

              <InfoRow label="Account number">{app.bankAccountNumber || "—"}</InfoRow>

              <InfoRow label="PayOS configuration">
                <span
                  className={`inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-semibold ring-1 ring-inset ${
                    app.hasPayosConfiguration
                      ? "bg-emerald-50 text-emerald-800 ring-emerald-200"
                      : "bg-slate-100 text-slate-600 ring-slate-200"
                  }`}
                >
                  <span
                    className={`size-1.5 rounded-full ${app.hasPayosConfiguration ? "bg-emerald-500" : "bg-slate-400"}`}

                    aria-hidden="true"
                  />

                  {app.hasPayosConfiguration ? "Provided" : "Not configured"}
                </span>
              </InfoRow>
            </dl>
          </Panel>

          <Panel
            title="Supporting documents"

            aside={
              <span className="text-xs text-slate-500">{currentDocuments.length} current</span>
            }
          >
            {currentDocuments.length === 0 ? (
              <p className="text-sm text-slate-500">No current documents attached.</p>
            ) : (
              <ul className="grid gap-2.5 sm:grid-cols-2">
                {currentDocuments.map((doc) => (
                  <li key={doc.id}>
                    <a
                      href={doc.fileUrl}

                      target="_blank"

                      rel="noreferrer"

                      className="group flex items-center gap-3 rounded-lg border border-slate-200 p-3 hover:border-emerald-400 hover:bg-emerald-50/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600"
                    >
                      <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-500 group-hover:bg-white group-hover:text-emerald-700">
                        <FileText className="size-[18px]" aria-hidden="true" />
                      </span>

                      <span className="min-w-0 flex-1">
                        <span className="block text-sm font-medium text-slate-900">
                          {doc.documentType}
                        </span>

                        <span className="block truncate text-xs text-slate-500">
                          {doc.originalFileName}
                        </span>
                      </span>

                      <ExternalLink
                        className="size-4 shrink-0 text-slate-300 group-hover:text-emerald-700"
                        aria-label="Open in new tab"
                      />
                    </a>
                  </li>
                ))}
              </ul>
            )}
          </Panel>
        </div>

        {/* Right: decision + history */}
        <div className="space-y-5">
          <Panel title="Review decision">
            {isPending ? (
              <>
                <label htmlFor="review-note" className="block text-sm font-medium text-slate-900">
                  Review note
                  <span className="ml-1.5 font-normal text-slate-500">Required to reject</span>
                </label>

                <textarea
                  id="review-note"

                  value={note}

                  onChange={(e) => onNoteChange(e.target.value)}

                  rows={5}

                  maxLength={NOTE_MAX_LENGTH}

                  placeholder="Explain why this application is rejected, or leave a note for the approval."

                  className="mt-2 w-full resize-y rounded-lg border border-slate-300 p-3 text-sm text-slate-900 outline-none placeholder:text-slate-400 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100"
                />

                <p className="mt-1 text-right text-xs tabular-nums text-slate-400">
                  {note.length} / {NOTE_MAX_LENGTH}
                </p>

                <div className="mt-3 grid grid-cols-2 gap-3">
                  <button
                    disabled={saving}

                    onClick={() => onReview("Approved")}

                    className={`inline-flex items-center justify-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50 ${focusRing}`}
                  >
                    {saving ? (
                      <Loader2 className="size-4 animate-spin" aria-hidden="true" />
                    ) : (
                      <CheckCircle2 className="size-4" aria-hidden="true" />
                    )}
                    Approve
                  </button>

                  <button
                    disabled={saving || !note.trim()}

                    onClick={() => onReview("Rejected")}

                    className="inline-flex items-center justify-center gap-2 rounded-lg bg-white px-4 py-2.5 text-sm font-semibold text-red-700 ring-1 ring-inset ring-red-300 hover:bg-red-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-600 disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:bg-white"
                  >
                    <XCircle className="size-4" aria-hidden="true" />
                    Reject
                  </button>
                </div>
              </>
            ) : (
              <div className="flex items-start gap-3">
                <span
                  className={`flex size-9 shrink-0 items-center justify-center rounded-full ring-1 ring-inset ${finalMeta.dot}`}
                >
                  <finalMeta.icon className="size-[18px]" aria-hidden="true" />
                </span>

                <div>
                  <p className="text-sm font-medium text-slate-900">Already reviewed</p>

                  <p className="mt-0.5 text-sm text-slate-500">
                    This application is {finalMeta.label.toLowerCase()}. The decision cannot be
                    changed here.
                  </p>
                </div>
              </div>
            )}
          </Panel>

          <Panel title="Review history">
            {logs.length === 0 ? (
              <p className="text-sm text-slate-500">No review activity yet.</p>
            ) : (
              <ol>
                {logs.map((log, i) => {
                  const meta = getStatusMeta(log.toStatus);

                  const isLast = i === logs.length - 1;

                  return (
                    <li key={`${log.revisionNumber}-${log.createdAt}-${i}`} className="flex gap-3">
                      <div className="flex flex-col items-center">
                        <span
                          className={`z-10 flex size-8 shrink-0 items-center justify-center rounded-full ring-1 ring-inset ${meta.dot}`}
                        >
                          <meta.icon className="size-4" aria-hidden="true" />
                        </span>

                        {!isLast && (
                          <span className="mt-1 w-px flex-1 bg-slate-200" aria-hidden="true" />
                        )}
                      </div>

                      <div className={`min-w-0 flex-1 ${isLast ? "" : "pb-5"}`}>
                        <div className="flex flex-wrap items-center gap-1.5">
                          {log.fromStatus ? (
                            <p className="text-sm font-semibold text-slate-900">
                              {getStatusMeta(log.fromStatus).label}

                              <span className="mx-1.5 font-normal text-slate-400" aria-label="to">
                                →
                              </span>

                              {meta.label}
                            </p>
                          ) : (
                            <p className="text-sm font-semibold text-slate-900">
                              Submitted as {meta.label}
                            </p>
                          )}
                        </div>

                        <p className="mt-0.5 text-xs text-slate-500">
                          Revision {log.revisionNumber}, {dateTime(log.createdAt)}
                        </p>

                        {log.note && (
                          <p
                            className={`mt-2 whitespace-pre-wrap rounded-lg px-3 py-2.5 text-sm ${
                              log.toStatus === "Rejected"
                                ? "border-l-4 border-red-500 bg-red-50 text-red-900"
                                : "bg-slate-50 text-slate-700"
                            }`}
                          >
                            {log.note}
                          </p>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ol>
            )}
          </Panel>
        </div>
      </div>
    </div>
  );
}
