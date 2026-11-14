import { Skeleton } from "@/shared/components/ui/Content";

export function DashboardSkeleton({ label = "Loading analytics..." }: { label?: string }) {
  return (
    <div className="space-y-6" role="status" aria-label={label}>
      <span className="sr-only">{label}</span>
      <div aria-hidden="true" className="space-y-6">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div className="min-w-0 space-y-2">
            <Skeleton className="h-8 w-72 max-w-full" />
            <Skeleton className="h-4 w-96 max-w-full" />
            <Skeleton className="h-7 w-48 max-w-full rounded-full" />
          </div>
          <div className="flex flex-wrap gap-2 pt-2">
            {[0, 1, 2, 3].map((item) => (
              <Skeleton key={item} className="h-10 w-24" />
            ))}
          </div>
        </div>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {[0, 1, 2, 3].map((item) => (
            <div
              key={item}
              className="space-y-3 rounded-lg border border-border-default bg-bg-surface p-4"
            >
              <Skeleton className="h-4 w-2/3" />
              <Skeleton className="h-8 w-1/2" />
              <Skeleton className="h-3 w-3/4" />
            </div>
          ))}
        </div>
        <div className="flex flex-wrap gap-2">
          {[0, 1, 2].map((item) => (
            <Skeleton key={item} className="h-10 w-28" />
          ))}
        </div>
        <div className="grid gap-6 lg:grid-cols-2">
          {[0, 1].map((panel) => (
            <div
              key={panel}
              className="space-y-4 rounded-lg border border-border-default bg-bg-surface p-5"
            >
              <Skeleton className="h-5 w-1/2" />
              <div className="flex h-52 items-end gap-3 border-b border-l border-border-default px-3">
                {[40, 65, 50, 80, 60, 90].map((height, index) => (
                  <Skeleton key={index} className="flex-1" style={{ height: `${height}%` }} />
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
