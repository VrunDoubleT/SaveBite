import { attributeValueChanges } from "../utils/attributeValueChanges";
import type { AttributeValueContent } from "../types/product.types";

export function AttributeValueReview({
  before,
  after,
  showUnchanged,
}: {
  before: AttributeValueContent[] | null;
  after: AttributeValueContent[] | null;
  showUnchanged: boolean;
}) {
  const rows = attributeValueChanges(before, after).filter((row) => showUnchanged || row.changed);
  if (before !== null && rows.length === 0) return null;
  return (
    <article className="overflow-hidden rounded-2xl border border-border-default">
      <div className="border-b border-border-default bg-neutral-50 px-4 py-3">
        <h3 className="text-sm font-semibold">Attribute values & images</h3>
        <p className="mt-1 text-xs text-text-secondary">
          Compare each value and its associated image.
        </p>
      </div>
      {before === null && (
        <p className="border-b border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          This older warning has no attribute snapshot. Previous images cannot be compared.
        </p>
      )}
      <div className="divide-y divide-border-default">
        {rows.map((row) => {
          const value = row.after ?? row.before!;
          const status =
            before === null
              ? "Current content"
              : !row.before
                ? "Added"
                : !row.after
                  ? "Removed"
                  : row.changed
                    ? "Modified"
                    : "Unchanged";
          return (
            <section key={row.id}>
              <div className="flex items-center justify-between gap-3 px-4 py-3">
                <h4 className="text-sm font-semibold">
                  {value.attributeName}: {value.value}
                </h4>
                <span className="text-xs font-medium text-violet-700">{status}</span>
              </div>
              <div className="grid md:grid-cols-2">
                {(["before", "after"] as const).map((side) => {
                  const item = row[side];
                  return (
                    <div
                      key={side}
                      className={
                        side === "before"
                          ? "border-b border-border-default md:border-b-0 md:border-r"
                          : ""
                      }
                    >
                      <p
                        className={`px-4 py-2 text-xs font-semibold ${side === "before" ? "bg-red-50 text-red-800" : "bg-emerald-50 text-emerald-800"}`}
                      >
                        {side === "before" ? "− Before · at warning" : "+ After · latest update"}
                      </p>
                      <div className="flex flex-wrap items-start gap-4 p-4">
                        {item ? (
                          <>
                            <div className="space-y-1 text-sm">
                              <p className="font-medium">{item.attributeName}</p>
                              <p>{item.value}</p>
                            </div>
                            {item.imageUrl ? (
                              <a
                                href={item.imageUrl}
                                target="_blank"
                                rel="noreferrer"
                                className="rounded-xl border border-border-default p-1"
                              >
                                <img
                                  src={item.imageUrl}
                                  alt={`${item.attributeName}: ${item.value} (${side})`}
                                  className="size-32 rounded-lg object-contain"
                                />
                              </a>
                            ) : (
                              <p className="rounded-xl bg-neutral-50 px-4 py-8 text-sm text-text-muted">
                                No image
                              </p>
                            )}
                          </>
                        ) : (
                          <p className="text-sm text-text-muted">
                            {before === null && side === "before"
                              ? "Previous content unavailable"
                              : "Value not present"}
                          </p>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </section>
          );
        })}
      </div>
    </article>
  );
}
