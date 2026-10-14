export function ProductListSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div role="status" aria-label="Loading products" className="overflow-x-auto">
      <span className="sr-only">Loading products…</span>
      <div aria-hidden="true" className="min-w-[760px] motion-safe:animate-pulse">
        <div className="grid grid-cols-[2fr_1fr_1fr_1fr_1fr_48px] gap-5 border-b border-neutral-100 px-5 py-4">
          {Array.from({ length: 6 }, (_, i) => (
            <div key={i} className="h-3 w-16 rounded bg-neutral-100" />
          ))}
        </div>
        {Array.from({ length: rows }, (_, i) => (
          <div
            key={i}
            className="grid grid-cols-[2fr_1fr_1fr_1fr_1fr_48px] items-center gap-5 border-b border-neutral-50 px-5 py-5"
          >
            <div className="flex items-center gap-3">
              <div className="size-12 shrink-0 rounded-lg bg-neutral-100" />
              <div className="flex-1 space-y-2">
                <div className="h-3 w-4/5 rounded bg-neutral-100" />
                <div className="h-3 w-1/2 rounded bg-neutral-100" />
              </div>
            </div>
            {Array.from({ length: 4 }, (_, j) => (
              <div key={j} className="h-5 w-3/4 rounded-full bg-neutral-100" />
            ))}
            <div className="size-9 rounded-lg bg-neutral-100" />
          </div>
        ))}
      </div>
    </div>
  );
}
