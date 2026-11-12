import { Skeleton } from "./Content";

interface LoadingSkeletonProps {
  variant?: "list" | "cards" | "detail" | "form" | "stats" | "profile" | "notifications" | "levels";
  rows?: number;
  label?: string;
  className?: string;
}

/** Match the shape of the incoming content while keeping one accessible loading announcement. */
export function LoadingSkeleton({
  variant = "list",
  rows = 3,
  label = "Loading content",
  className = "",
}: LoadingSkeletonProps) {
  const items = Array.from({ length: rows }, (_, index) => index);
  return (
    <div role="status" aria-label={label} className={`min-w-0 space-y-4 ${className}`}>
      <span className="sr-only">{label}</span>
      <div aria-hidden="true" className="space-y-4">
        {(variant === "detail" || variant === "profile") && (
          <div className="flex items-center gap-4 rounded-lg border border-border-default bg-bg-surface p-4">
            {variant === "profile" && <Skeleton className="size-16 shrink-0 rounded-full" />}
            <div className="min-w-0 flex-1 space-y-3">
              <Skeleton className="h-6 w-2/3" />
              <Skeleton className="h-4 w-1/2" />
            </div>
            <Skeleton className="h-7 w-20" />
          </div>
        )}
        {variant === "stats" ? (
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {items.map((index) => (
              <div
                key={index}
                className="space-y-3 rounded-lg border border-border-default bg-bg-surface p-4"
              >
                <Skeleton className="h-4 w-1/2" />
                <Skeleton className="h-8 w-2/3" />
                <Skeleton className="h-3 w-3/4" />
              </div>
            ))}
          </div>
        ) : variant === "form" ? (
          <div className="space-y-5 rounded-lg border border-border-default bg-bg-surface p-4">
            <Skeleton className="h-6 w-1/3" />
            <div className="grid gap-5 sm:grid-cols-2">
              {items.map((index) => (
                <div key={index} className="space-y-2">
                  <Skeleton className="h-4 w-1/3" />
                  <Skeleton className="h-11 w-full" />
                </div>
              ))}
            </div>
            <Skeleton className="h-24 w-full" />
            <Skeleton className="ml-auto h-10 w-28" />
          </div>
        ) : (
          <div
            className={
              variant === "cards"
                ? "grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
                : variant === "levels"
                  ? "grid gap-4 md:grid-cols-2"
                  : "space-y-3"
            }
          >
            {items.map((index) => (
              <div
                key={index}
                className="min-w-0 rounded-lg border border-border-default bg-bg-surface p-4"
              >
                {variant === "cards" && <Skeleton className="mb-4 aspect-video w-full" />}
                <div className="flex items-start gap-3">
                  {(variant === "notifications" || variant === "list") && (
                    <Skeleton className="size-10 shrink-0 rounded-full" />
                  )}
                  <div className="min-w-0 flex-1 space-y-3">
                    <Skeleton className="h-4 w-2/3" />
                    <Skeleton className="h-3 w-full" />
                    <Skeleton className="h-3 w-1/2" />
                  </div>
                </div>
                {(variant === "detail" || variant === "profile" || variant === "levels") && (
                  <div className="mt-4 grid gap-3 sm:grid-cols-2">
                    <Skeleton className="h-12" />
                    <Skeleton className="h-12" />
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

/** Render inside tbody: the number of cells mirrors the real table, without nesting another table. */
export function TableSkeletonRows({
  columns,
  rows = 4,
  label = "Loading records",
}: {
  columns: number;
  rows?: number;
  label?: string;
}) {
  return (
    <>
      {Array.from({ length: rows }, (_, row) => (
        <tr key={row} aria-hidden={row > 0 ? true : undefined}>
          {Array.from({ length: columns }, (_, column) => (
            <td key={column} className="px-4 py-4">
              {row === 0 && column === 0 && (
                <span role="status" className="sr-only">
                  {label}
                </span>
              )}
              <Skeleton
                aria-hidden="true"
                className={`h-4 min-w-8 ${column === 0 ? "w-4/5" : "w-2/3"}`}
              />
            </td>
          ))}
        </tr>
      ))}
    </>
  );
}

export function InlineSkeleton({
  label = "Loading value",
  className = "w-24",
}: {
  label?: string;
  className?: string;
}) {
  return (
    <span
      role="status"
      aria-label={label}
      className={`inline-block h-[1em] max-w-full animate-pulse rounded bg-neutral-200 align-middle motion-reduce:animate-none ${className}`}
    >
      <span className="sr-only">{label}</span>
    </span>
  );
}

export function TableLoadingSkeleton({
  headers,
  label = "Loading records",
}: {
  headers: string[];
  label?: string;
}) {
  return (
    <div className="min-w-0 overflow-x-auto rounded-lg border border-border-default bg-bg-surface">
      <table className="w-full text-left text-sm">
        <thead className="bg-neutral-50">
          <tr>
            {headers.map((header) => (
              <th
                key={header}
                className="whitespace-nowrap px-4 py-3 text-xs font-semibold text-text-secondary"
              >
                {header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-border-default">
          <TableSkeletonRows columns={headers.length} label={label} />
        </tbody>
      </table>
    </div>
  );
}
