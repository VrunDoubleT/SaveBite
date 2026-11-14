import { Skeleton } from "@/shared/components/ui/Content";
import { TableSkeletonRows } from "@/shared/components/ui/LoadingSkeleton";

function ChartPlaceholder() {
  return (
    <div className="flex h-[230px] items-end gap-3 border-b border-l border-neutral-200 px-3 pt-4">
      {[40, 65, 50, 80, 60, 90].map((height, index) => (
        <Skeleton key={index} className="min-w-0 flex-1" style={{ height: `${height}%` }} />
      ))}
    </div>
  );
}

/** Matches the admin Overview: comparison, both tab rows, revenue, orders, fees and platform statistics. */
export function PlatformOverviewSkeleton() {
  return (
    <div
      role="status"
      aria-label="Loading platform analytics..."
      aria-busy="true"
      className="min-w-0 space-y-6"
    >
      <span className="sr-only">Loading platform analytics...</span>
      <div aria-hidden="true" className="min-w-0 space-y-6">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div className="min-w-0 space-y-2">
            <Skeleton className="h-8 w-64" />
            <Skeleton className="h-5 w-96" />
            <Skeleton className="h-7 w-48 rounded-full" />
          </div>
          <div className="flex max-w-full flex-wrap gap-2">
            {[0, 1, 2].map((index) => (
              <Skeleton key={index} className="h-10 w-24" />
            ))}
          </div>
        </div>

        <section
          data-loading-section="comparison"
          className="rounded-lg border border-neutral-200 bg-white shadow-sm"
        >
          <div className="space-y-2 px-5 pt-5 sm:px-6 sm:pt-6">
            <Skeleton className="h-7 w-36" />
            <Skeleton className="h-5 w-96" />
          </div>
          <div className="grid gap-6 px-5 pb-5 pt-5 sm:px-6 sm:pb-6 lg:grid-cols-2">
            {[0, 1].map((index) => (
              <div
                key={index}
                className="min-w-0 overflow-hidden rounded-md border border-neutral-200"
              >
                <table className="w-full table-fixed text-left text-sm">
                  <thead className="bg-neutral-50">
                    <tr>
                      {[0, 1, 2].map((column) => (
                        <th key={column} className="px-4 py-3">
                          <Skeleton className="h-4 w-full" />
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-neutral-100">
                    <TableSkeletonRows columns={3} rows={5} cellClassName="px-4 py-3.5" />
                  </tbody>
                </table>
              </div>
            ))}
          </div>
        </section>

        <div className="flex flex-wrap items-center gap-3">
          <Skeleton className="h-5 w-28" />
          <Skeleton className="h-10 w-56" />
        </div>

        <div
          data-loading-section="section-tabs"
          className="flex gap-1 overflow-x-auto border-b border-neutral-200 pb-3"
        >
          {Array.from({ length: 7 }, (_, index) => (
            <Skeleton key={index} className="h-10 w-28 shrink-0" />
          ))}
        </div>

        <section
          data-loading-section="revenue"
          className="overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-sm"
        >
          <div className="grid gap-8 p-6 sm:p-8 lg:grid-cols-5">
            <div className="flex min-w-0 flex-col justify-between gap-6 lg:col-span-2">
              <div className="space-y-3">
                <Skeleton className="h-5 w-36" />
                <Skeleton className="h-16 w-full" />
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-12 w-56" />
              </div>
              <div className="flex flex-wrap gap-2">
                {[0, 1, 2].map((index) => (
                  <Skeleton key={index} className="h-14 w-28" />
                ))}
              </div>
            </div>
            <div className="min-w-0 space-y-2 lg:col-span-3">
              <Skeleton className="h-5 w-40" />
              <ChartPlaceholder />
            </div>
          </div>
          <div className="grid grid-cols-2 border-t border-neutral-200 sm:grid-cols-4">
            {[0, 1, 2, 3].map((index) => (
              <div key={index} className="min-w-0 space-y-2 px-6 py-4 sm:px-8">
                <Skeleton className="h-3 w-full" />
                <Skeleton className="h-6 w-full" />
                <Skeleton className="h-3 w-2/3" />
              </div>
            ))}
          </div>
        </section>

        <section
          data-loading-section="orders"
          className="min-w-0 space-y-5 rounded-lg border border-neutral-200 bg-white p-5 sm:p-6"
        >
          <Skeleton className="h-7 w-48" />
          <div className="grid gap-6 lg:grid-cols-2">
            <ChartPlaceholder />
            <ChartPlaceholder />
          </div>
        </section>
        <section
          data-loading-section="fees"
          className="min-w-0 space-y-5 rounded-lg border border-neutral-200 bg-white p-5 sm:p-6"
        >
          <Skeleton className="h-7 w-48" />
          <div className="grid grid-cols-2 gap-4 xl:grid-cols-4">
            {[0, 1, 2, 3].map((index) => (
              <Skeleton key={index} className="h-24 w-full" />
            ))}
          </div>
          <ChartPlaceholder />
        </section>
        <div data-loading-section="users-shops" className="grid grid-cols-1 gap-6 2xl:grid-cols-2">
          {[0, 1].map((index) => (
            <section
              key={index}
              className="min-w-0 space-y-5 rounded-lg border border-neutral-200 bg-white p-5 sm:p-6"
            >
              <Skeleton className="h-7 w-48" />
              <ChartPlaceholder />
            </section>
          ))}
        </div>
        {["deals", "payments-refunds"].map((section) => (
          <section
            key={section}
            data-loading-section={section}
            className="min-w-0 space-y-5 rounded-lg border border-neutral-200 bg-white p-5 sm:p-6"
          >
            <Skeleton className="h-7 w-48" />
            <Skeleton className="h-40 w-full" />
          </section>
        ))}
      </div>
    </div>
  );
}
