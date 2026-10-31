export function DashboardSkeleton({ label = "Loading analytics..." }: { label?: string }) {
  return (
    <div className="space-y-6" role="status" aria-live="polite" aria-label={label}>
      <div className="space-y-2">
        <div className="h-8 w-72 animate-pulse rounded bg-neutral-200" />
        <div className="h-4 w-96 max-w-full animate-pulse rounded bg-neutral-100" />
      </div>
      <div className="h-80 animate-pulse rounded-lg bg-primary-800/15" />
      <div className="grid gap-6 lg:grid-cols-2">
        {[0, 1].map((panel) => (
          <div key={panel} className="h-72 animate-pulse rounded-lg border border-neutral-200 bg-white" />
        ))}
      </div>
      <span className="sr-only">{label}</span>
    </div>
  );
}
