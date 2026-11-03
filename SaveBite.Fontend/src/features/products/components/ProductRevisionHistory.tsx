import { useState } from "react";
import { ChevronDown, History } from "lucide-react";
import type {
  ProductRevisionHistory as Revision,
  RevisionContentSnapshot,
} from "../types/product.types";
import { AttributeValueReview } from "./AttributeValueReview";
import { attributeValueChanges } from "../utils/attributeValueChanges";

const date = (value: string) => new Date(value).toLocaleString("vi-VN");
const states: Record<number, string> = {
  1: "Pending review",
  2: "Approved",
  3: "Changes requested",
  4: "Suspended",
};
function changes(before: RevisionContentSnapshot | null, after: RevisionContentSnapshot) {
  if (!before) return ["Initial content"];
  const result: string[] = [];
  if (before.name !== after.name) result.push("Name");
  if ((before.description ?? "") !== (after.description ?? "")) result.push("Description");
  if (before.categoryId !== after.categoryId) result.push("Category");
  if (JSON.stringify(before.images) !== JSON.stringify(after.images)) result.push("Gallery");
  if (
    before.attributeValues !== null &&
    after.attributeValues !== null &&
    attributeValueChanges(before.attributeValues, after.attributeValues).some((row) => row.changed)
  )
    result.push("Attribute values / images");
  return result;
}

function RevisionDetails({ revision: r }: { revision: Revision }) {
  const snapshots = r.before
    ? [
        { label: "Before · previous revision", snapshot: r.before },
        { label: "After · this revision", snapshot: r.after },
      ]
    : [{ label: "Initial product content", snapshot: r.after }];
  return (
    <div className="space-y-4 border-t border-border-default p-4 sm:p-5">
      {r.warningId && (
        <div className="rounded-xl border border-violet-200 bg-violet-50 p-4 text-sm text-violet-950">
          <p className="font-semibold">Correction for administrator warning</p>
          {r.warningReason && <p className="mt-2">Reason: {r.warningReason}</p>}
          {r.requiredCorrection && (
            <p className="mt-1 whitespace-pre-wrap">Requested correction: {r.requiredCorrection}</p>
          )}
          {r.warningDueAt && (
            <p className="mt-3 rounded-lg bg-white/70 px-3 py-2 font-semibold">
              Correction deadline: <time dateTime={r.warningDueAt}>{date(r.warningDueAt)}</time>
            </p>
          )}
          <p className="mt-2 break-all text-xs text-violet-700">Warning ID: {r.warningId}</p>
        </div>
      )}
      {r.reviewedAt && (
        <div className="rounded-xl bg-neutral-50 p-3 text-sm">
          <p>
            Reviewed by {r.reviewedByName || r.reviewedByUserId || "Administrator"} ·{" "}
            {date(r.reviewedAt)}
          </p>
          {r.reviewNote && <p className="mt-2 whitespace-pre-wrap">Review note: {r.reviewNote}</p>}
        </div>
      )}
      <div
        className={`grid overflow-hidden rounded-xl border border-border-default ${r.before ? "md:grid-cols-2" : ""}`}
      >
        {snapshots.map(({ label, snapshot: s }, index) => (
          <div
            key={label}
            className={
              r.before && index === 0
                ? "min-w-0 border-b border-border-default md:border-b-0 md:border-r"
                : "min-w-0"
            }
          >
            <h4
              className={`px-4 py-2 text-xs font-semibold ${r.before ? (index === 0 ? "bg-red-50 text-red-800" : "bg-emerald-50 text-emerald-800") : "bg-neutral-50 text-text-secondary"}`}
            >
              {label}
            </h4>
            <dl className="space-y-4 p-4 text-sm">
              {(
                [
                  ["Product name", s.name, r.before?.name !== r.after.name],
                  [
                    "Description",
                    s.description || "—",
                    (r.before?.description ?? "") !== (r.after.description ?? ""),
                  ],
                  ["Category", s.categoryName, r.before?.categoryId !== r.after.categoryId],
                ] as const
              ).map(([name, value, changed]) => (
                <div
                  key={name}
                  className={
                    r.before && changed
                      ? "rounded-lg border-l-2 border-violet-300 bg-violet-50/50 p-3"
                      : ""
                  }
                >
                  <dt className="text-xs font-semibold text-text-secondary">
                    {name}
                    {r.before && changed && <span className="ml-2 text-violet-700">Modified</span>}
                  </dt>
                  <dd className="mt-1 whitespace-pre-wrap break-words leading-6 [overflow-wrap:anywhere]">
                    {value}
                  </dd>
                </div>
              ))}
              <div>
                <dt className="mb-2 text-xs font-semibold text-text-secondary">
                  Gallery · {s.images.length} images
                </dt>
                <dd className="flex flex-wrap gap-2">
                  {s.images.map((url, i) => (
                    <a key={`${url}-${i}`} href={url} target="_blank" rel="noreferrer">
                      <img
                        src={url}
                        alt={`Gallery image ${i + 1}`}
                        className="size-24 rounded-lg border border-border-default object-contain"
                      />
                    </a>
                  ))}
                  {!s.images.length && <span className="text-text-muted">No images</span>}
                </dd>
              </div>
            </dl>
          </div>
        ))}
      </div>
      {r.before ? (
        r.after.attributeValues === null ? (
          <p className="text-sm text-text-muted">
            Attribute snapshot unavailable for this revision.
          </p>
        ) : (
          <AttributeValueReview
            before={r.before.attributeValues}
            after={r.after.attributeValues}
            showUnchanged
          />
        )
      ) : (
        <div>
          <h4 className="mb-3 text-sm font-semibold">Initial attribute values</h4>
          {r.after.attributeValues === null ? (
            <p className="text-sm text-text-muted">
              No attribute snapshot was recorded for this older revision.
            </p>
          ) : (
            <div className="flex flex-wrap gap-3">
              {r.after.attributeValues.map((v) => (
                <figure key={v.valueId} className="rounded-xl border border-border-default p-3">
                  <figcaption className="mb-2 text-sm">
                    {v.attributeName}: {v.value}
                  </figcaption>
                  {v.imageUrl ? (
                    <a href={v.imageUrl} target="_blank" rel="noreferrer">
                      <img src={v.imageUrl} alt={v.value} className="size-24 object-contain" />
                    </a>
                  ) : (
                    <p className="text-xs text-text-muted">No image</p>
                  )}
                </figure>
              ))}
              {!r.after.attributeValues.length && (
                <p className="text-sm text-text-muted">No attribute values</p>
              )}
            </div>
          )}
        </div>
      )}
      {r.before && (r.before.attributeValues === null || r.after.attributeValues === null) && (
        <p className="text-xs text-amber-800">
          Attribute image history is incomplete for older revisions. Missing snapshots are not
          treated as additions or deletions.
        </p>
      )}
    </div>
  );
}

export function ProductRevisionHistory({ revisions }: { revisions: Revision[] }) {
  const [page, setPage] = useState(1);
  const totalPages = Math.max(1, Math.ceil(revisions.length / 5));
  const currentPage = Math.min(page, totalPages);
  const items = revisions.slice((currentPage - 1) * 5, currentPage * 5);
  return (
    <section
      className="overflow-hidden rounded-3xl border border-border-default bg-white shadow-sm"
      aria-label="Product revision history"
    >
      <header className="flex flex-wrap items-center justify-between gap-3 border-b border-border-default p-5 sm:p-6">
        <div>
          <h2 className="flex items-center gap-2 text-lg font-semibold">
            <History className="size-5 text-primary-700" />
            Shop revision history
          </h2>
          <p className="mt-1 text-sm text-text-secondary">
            Every recorded content update, newest first. Open a revision to compare it with the
            previous version.
          </p>
        </div>
      </header>
      <div className="space-y-3 p-4 sm:p-6">
        {items.length === 0 ? (
          <p className="py-6 text-center text-sm text-text-secondary">No revisions recorded.</p>
        ) : (
          items.map((r) => {
            const fields = changes(r.before, r.after);
            return (
              <details
                key={r.id}
                className="group overflow-hidden rounded-2xl border border-border-default"
              >
                <summary className="flex cursor-pointer list-none items-start justify-between gap-3 p-4 focus-visible:outline-2 focus-visible:outline-primary-500 sm:p-5">
                  <div className="min-w-0 space-y-2">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold">Revision #{r.revisionNumber}</span>
                      {r.isLatest && (
                        <span className="rounded-full bg-primary-50 px-2 py-1 text-xs font-semibold text-primary-700">
                          Latest
                        </span>
                      )}
                      <span className="rounded-full bg-neutral-100 px-2 py-1 text-xs font-medium">
                        {states[r.status] ?? "Unknown status"}
                      </span>
                      <span
                        className={`rounded-full px-2 py-1 text-xs font-medium ${r.warningId ? "bg-violet-50 text-violet-700" : "bg-blue-50 text-blue-700"}`}
                      >
                        {r.warningId
                          ? "Warning correction"
                          : !r.before
                            ? "Product created"
                            : "Shop update"}
                      </span>
                    </div>
                    <p className="text-xs text-text-secondary">
                      {r.submittedByName || r.submittedByUserId} · {date(r.submittedAt)}
                    </p>
                    {r.warningDueAt && (
                      <p className="text-xs font-medium text-violet-700">
                        Warning deadline:{" "}
                        <time dateTime={r.warningDueAt}>{date(r.warningDueAt)}</time>
                      </p>
                    )}
                    <p className="text-sm text-text-secondary">
                      {fields.length
                        ? `Changed: ${fields.join(", ")}`
                        : "No differences in captured fields"}
                    </p>
                  </div>
                  <ChevronDown className="mt-1 size-5 shrink-0 text-text-muted transition-transform group-open:rotate-180" />
                </summary>
                <RevisionDetails revision={r} />
              </details>
            );
          })
        )}
        {revisions.length > 0 && (
          <div className="flex flex-wrap items-center justify-between gap-3 pt-3 text-sm">
            <span className="text-text-secondary">
              {revisions.length} revisions · Page {currentPage} / {totalPages}
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={currentPage <= 1}
                onClick={() => setPage(currentPage - 1)}
                className="rounded-lg border border-border-default px-3 py-2 disabled:opacity-40"
              >
                Newer
              </button>
              <button
                type="button"
                disabled={currentPage >= totalPages}
                onClick={() => setPage(currentPage + 1)}
                className="rounded-lg border border-border-default px-3 py-2 disabled:opacity-40"
              >
                Older
              </button>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
