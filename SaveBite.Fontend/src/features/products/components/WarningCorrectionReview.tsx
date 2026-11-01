import { AttributeValueReview } from "./AttributeValueReview";
import { attributeValueChanges } from "../utils/attributeValueChanges";
import { useState } from "react";
import { CheckCircle2, Clock3, FilePenLine } from "lucide-react";
import type { ProductContentManagement } from "../types/product.types";

type Comparison = NonNullable<ProductContentManagement["warningComparison"]>;
const date = (value: string) => new Date(value).toLocaleString("vi-VN");

function TextChange({
  before,
  after,
  side,
}: {
  before: string;
  after: string;
  side: "before" | "after";
}) {
  if (before === after) return <>{before || "—"}</>;
  let start = 0;
  while (start < before.length && start < after.length && before[start] === after[start]) start++;
  let end = 0;
  while (
    end < before.length - start &&
    end < after.length - start &&
    before[before.length - 1 - end] === after[after.length - 1 - end]
  )
    end++;
  const value = side === "before" ? before : after;
  const changed = value.slice(start, value.length - end);
  return (
    <>
      {value.slice(0, start)}
      {changed &&
        (side === "before" ? (
          <del className="rounded bg-red-200/70 px-0.5 text-red-950 decoration-red-500">
            {changed}
          </del>
        ) : (
          <ins className="rounded bg-emerald-200/70 px-0.5 text-emerald-950 no-underline">
            {changed}
          </ins>
        ))}
      {end > 0 && value.slice(-end)}
      {!value && <span className="italic text-text-muted">Empty</span>}
    </>
  );
}

export function WarningCorrectionReview({ comparison: c }: { comparison: Comparison }) {
  const [showUnchanged, setShowUnchanged] = useState(false);
  const fields = [
    { key: "name", label: "Product name" },
    { key: "description", label: "Description" },
    { key: "categoryName", label: "Category" },
  ] as const;
  const isChanged = (key: (typeof fields)[number]["key"]) =>
    key === "categoryName"
      ? c.before.categoryId !== c.after.categoryId
      : (c.before[key] ?? "") !== (c.after[key] ?? "");
  const galleryChanged = JSON.stringify(c.before.images) !== JSON.stringify(c.after.images);
  const attributeChanges =
    c.before.attributeValues == null
      ? 0
      : attributeValueChanges(c.before.attributeValues, c.after.attributeValues).filter(
          (row) => row.changed,
        ).length;
  const count =
    fields.filter((f) => isChanged(f.key)).length + Number(galleryChanged) + attributeChanges;
  const submitted = !!c.firstEditedAt;
  const StatusIcon = submitted ? CheckCircle2 : Clock3;
  const heading = (side: "before" | "after") => (
    <p
      className={`px-4 py-2 text-xs font-semibold ${side === "before" ? "bg-red-50 text-red-800" : "bg-emerald-50 text-emerald-800"}`}
    >
      {side === "before" ? "− Before · at warning" : "+ After · latest update"}
    </p>
  );
  return (
    <section
      className="overflow-hidden rounded-3xl border border-border-default bg-white shadow-sm"
      aria-label="Warning correction review"
    >
      <header className="border-b border-border-default bg-neutral-50/70 p-5 sm:p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="flex items-start gap-3">
            <span className="rounded-xl bg-violet-100 p-2.5 text-violet-700">
              <FilePenLine className="size-5" />
            </span>
            <div>
              <h2 className="text-lg font-semibold text-text-primary">
                Review warning corrections
              </h2>
              <p className="mt-1 text-sm text-text-secondary">
                Compare the flagged content with the latest shop update.
              </p>
            </div>
          </div>
          <span
            className={`inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-xs font-semibold ${submitted ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"}`}
          >
            <StatusIcon className="size-4" />
            {submitted ? "Correction submitted · awaiting review" : "Waiting for shop correction"}
          </span>
        </div>
        <div className="mt-5 grid gap-4 rounded-2xl border border-amber-200 bg-amber-50 p-4 sm:grid-cols-2">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-amber-800">
              Warning reason
            </p>
            <p className="mt-1 whitespace-pre-wrap text-sm text-amber-950">{c.reason}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-amber-800">
              Requested correction
            </p>
            <p className="mt-1 whitespace-pre-wrap text-sm text-amber-950">
              {c.requiredCorrection}
            </p>
          </div>
        </div>
        <div className="mt-4 flex flex-wrap gap-x-6 gap-y-2 text-xs text-text-secondary">
          <span>Edit deadline: {date(c.dueAt)}</span>
          {c.firstEditedAt && <span>First correction submitted: {date(c.firstEditedAt)}</span>}
          {c.dealBlocked && (
            <span className="font-semibold text-red-700">Opening deals blocked until approval</span>
          )}
        </div>
      </header>
      <div className="p-5 sm:p-6">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm font-semibold text-text-primary">
            {count} {count === 1 ? "field changed" : "fields changed"}
          </p>
          <label className="flex cursor-pointer items-center gap-2 text-sm text-text-secondary">
            <input
              type="checkbox"
              checked={showUnchanged}
              onChange={(e) => setShowUnchanged(e.target.checked)}
              className="size-4 accent-violet-600"
            />
            Show unchanged fields
          </label>
        </div>
        <div className="space-y-4">
          {fields
            .filter((f) => showUnchanged || isChanged(f.key))
            .map((field) => {
              const changed = isChanged(field.key);
              return (
                <article
                  key={field.key}
                  className="overflow-hidden rounded-2xl border border-border-default"
                >
                  <div className="flex items-center justify-between border-b border-border-default bg-neutral-50 px-4 py-2.5">
                    <h3 className="text-sm font-semibold">{field.label}</h3>
                    <span
                      className={`text-xs font-medium ${changed ? "text-violet-700" : "text-text-muted"}`}
                    >
                      {changed ? "Modified" : "Unchanged"}
                    </span>
                  </div>
                  <div className="grid min-w-0 md:grid-cols-2">
                    {(["before", "after"] as const).map((side) => (
                      <div
                        key={side}
                        className={`min-w-0 ${side === "before" ? "border-b border-border-default md:border-b-0 md:border-r" : ""}`}
                      >
                        {heading(side)}
                        <div className="whitespace-pre-wrap break-words p-4 text-sm leading-7 text-text-primary [overflow-wrap:anywhere]">
                          <TextChange
                            before={c.before[field.key] ?? ""}
                            after={c.after[field.key] ?? ""}
                            side={side}
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                </article>
              );
            })}
          {(galleryChanged || showUnchanged) && (
            <article className="overflow-hidden rounded-2xl border border-border-default">
              <div className="flex items-center justify-between border-b border-border-default bg-neutral-50 px-4 py-2.5">
                <h3 className="text-sm font-semibold">Product gallery</h3>
                <span className="text-xs text-text-secondary">
                  {galleryChanged ? "Modified · image order shown below" : "Unchanged"}
                </span>
              </div>
              <div className="grid md:grid-cols-2">
                {(["before", "after"] as const).map((side) => (
                  <div
                    key={side}
                    className={
                      side === "before"
                        ? "border-b border-border-default md:border-b-0 md:border-r"
                        : ""
                    }
                  >
                    {heading(side)}
                    <div className="flex flex-wrap gap-3 p-4">
                      {c[side].images.length === 0 && (
                        <p className="text-sm text-text-muted">No images</p>
                      )}
                      {c[side].images.map((url, i) => {
                        const other = side === "before" ? "after" : "before";
                        const otherIndex = c[other].images.indexOf(url);
                        const label =
                          otherIndex < 0
                            ? side === "before"
                              ? "Removed"
                              : "Added"
                            : otherIndex !== i
                              ? "Reordered"
                              : "Unchanged";
                        return (
                          <figure
                            key={`${url}-${i}`}
                            className={`w-28 overflow-hidden rounded-xl border ${otherIndex < 0 ? (side === "before" ? "border-red-300 bg-red-50" : "border-emerald-300 bg-emerald-50") : "border-border-default"}`}
                          >
                            <a href={url} target="_blank" rel="noreferrer">
                              <img
                                src={url}
                                alt={`${side} gallery image ${i + 1}`}
                                className="aspect-square w-full object-cover"
                              />
                            </a>
                            <figcaption className="px-2 py-1.5 text-xs">
                              {i + 1}. {label}
                            </figcaption>
                          </figure>
                        );
                      })}
                    </div>
                  </div>
                ))}
              </div>
            </article>
          )}
          <AttributeValueReview
            before={c.before.attributeValues ?? null}
            after={c.after.attributeValues ?? null}
            showUnchanged={showUnchanged}
          />
          {count === 0 && !showUnchanged && (
            <div className="rounded-2xl border border-dashed border-border-default bg-neutral-50 px-5 py-8 text-center">
              <p className="font-medium text-text-primary">
                No differences from the flagged content
              </p>
              <p className="mt-1 text-sm text-text-secondary">
                {submitted
                  ? "The shop submitted an earlier edit, but the latest content matches the original warning."
                  : "The shop has not submitted content changes yet."}
              </p>
            </div>
          )}
        </div>
      </div>
    </section>
  );
}
