import type { ReactNode } from "react";
import { Skeleton } from "./Content";

function LoadingFrame({
  label,
  children,
  className = "space-y-6",
}: {
  label: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div role="status" aria-label={label} aria-busy="true" className={`min-w-0 ${className}`}>
      <span className="sr-only">{label}</span>
      <div aria-hidden="true" className={className}>
        {children}
      </div>
    </div>
  );
}

function Heading({ back = false }: { back?: boolean }) {
  return (
    <>
      {back && <Skeleton className="h-4 w-36 max-w-full" />}
      <div className="space-y-2">
        <Skeleton className="h-8 w-64 max-w-full" />
        <Skeleton className="h-5 w-96 max-w-full" />
      </div>
    </>
  );
}

function Fields({
  count = 4,
  className = "grid gap-4 sm:grid-cols-2",
}: {
  count?: number;
  className?: string;
}) {
  return (
    <div className={className}>
      {Array.from({ length: count }, (_, index) => (
        <div key={index} className="min-w-0 space-y-2">
          <Skeleton className="h-4 w-24 max-w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      ))}
    </div>
  );
}

function Panel({ children }: { children: ReactNode }) {
  return (
    <div className="min-w-0 space-y-4 rounded-xl border border-neutral-200 bg-white p-5">
      {children}
    </div>
  );
}

/** Uses the same summary grid, schedule grid and product columns as both deal detail pages. */
export function FlashDealDetailSkeleton({ staff = false }: { staff?: boolean }) {
  return (
    <LoadingFrame label="Loading flash deal">
      <Heading back />
      <Panel>
        <div className="flex items-center gap-4">
          <Skeleton className="size-20 shrink-0 rounded-lg" />
          <div className="min-w-0 flex-1 space-y-3">
            <Skeleton className="h-6 w-2/3" />
            <Skeleton className="h-4 w-1/3" />
          </div>
        </div>
      </Panel>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        {Array.from({ length: staff ? 5 : 6 }, (_, index) => (
          <div
            key={index}
            className={`rounded-xl border border-neutral-100 bg-white p-4 ${staff && index === 4 ? "col-span-2" : ""}`}
          >
            <div className="flex items-center gap-3">
              <Skeleton className="size-10 shrink-0 rounded-lg" />
              <div className="min-w-0 flex-1 space-y-2">
                <Skeleton className="h-3 w-full" />
                <Skeleton className="h-7 w-2/3" />
              </div>
            </div>
          </div>
        ))}
      </div>
      <Panel>
        <Skeleton className="h-5 w-36" />
        <Fields className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4" />
      </Panel>
      <Panel>
        <Skeleton className="h-5 w-40" />
        <Skeleton className="h-40 w-full" />
      </Panel>
    </LoadingFrame>
  );
}

/** Full forms have several sections; a single generic form card caused the page to jump. */
export function FormPageSkeleton({ label, sections = 3 }: { label: string; sections?: number }) {
  return (
    <LoadingFrame label={label}>
      <Heading back />
      {Array.from({ length: sections }, (_, index) => (
        <Panel key={index}>
          <Skeleton className="h-5 w-40 max-w-full" />
          <Fields count={index === 0 ? 2 : 4} />
        </Panel>
      ))}
    </LoadingFrame>
  );
}

export function ShopProfileSkeleton() {
  return (
    <LoadingFrame label="Loading shop profile">
      <Heading />
      <div className="overflow-hidden rounded-xl border border-neutral-200 bg-white">
        <Skeleton className="h-48 w-full rounded-none sm:h-64" />
        <div className="space-y-5 p-5 sm:p-7">
          <div className="flex items-center gap-4">
            <Skeleton className="size-20 shrink-0 rounded-xl" />
            <div className="min-w-0 flex-1 space-y-3">
              <Skeleton className="h-7 w-1/2" />
              <Skeleton className="h-4 w-2/3" />
            </div>
          </div>
          <Fields count={3} className="grid gap-4 sm:grid-cols-3" />
        </div>
      </div>
      <div className="grid min-w-0 items-start gap-5 xl:grid-cols-[minmax(0,1.65fr)_minmax(19rem,1fr)]">
        <Panel>
          <Skeleton className="h-5 w-36" />
          <Fields count={6} className="space-y-5" />
        </Panel>
        <Panel>
          <Skeleton className="h-5 w-36" />
          <Skeleton className="h-64 w-full" />
        </Panel>
      </div>
    </LoadingFrame>
  );
}

export function OrderDetailSkeleton() {
  return (
    <LoadingFrame label="Loading order" className="space-y-4">
      <Panel>
        <Fields count={2} />
        <Skeleton className="h-16 w-full" />
      </Panel>
      <div className="grid items-start gap-4 xl:grid-cols-[minmax(0,1fr)_320px]">
        <div className="min-w-0 space-y-4">
          <Panel>
            <Skeleton className="h-5 w-40" />
            <Skeleton className="h-20 w-full" />
            <Skeleton className="h-20 w-full" />
          </Panel>
          <Panel>
            <Fields />
          </Panel>
        </div>
        <Panel>
          <Skeleton className="h-5 w-32" />
          <Fields count={3} className="space-y-4" />
        </Panel>
      </div>
    </LoadingFrame>
  );
}

export function StatementDetailSkeleton({ admin = false }: { admin?: boolean }) {
  return (
    <LoadingFrame label="Loading statement details">
      {!admin && (
        <>
          <Skeleton className="h-4 w-40" />
          <Panel>
            <Skeleton className="h-28 w-full" />
            <Fields className="grid grid-cols-2 gap-4 sm:grid-cols-4" />
          </Panel>
        </>
      )}
      {admin && <Fields className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4" />}
      <Panel>
        <Skeleton className="h-5 w-48" />
        <Skeleton className="h-60 w-full" />
      </Panel>
    </LoadingFrame>
  );
}

export function StaffDetailSkeleton() {
  return (
    <LoadingFrame label="Loading staff details">
      <Skeleton className="h-5 w-48 max-w-full" />
      <div className="rounded-xl border border-neutral-200 bg-white p-6">
        <div className="flex items-center gap-4">
          <Skeleton className="size-14 shrink-0 rounded-full" />
          <div className="min-w-0 flex-1 space-y-2">
            <Skeleton className="h-7 w-48" />
            <Skeleton className="h-5 w-56" />
          </div>
        </div>
        <div className="mt-6 border-t border-neutral-100 pt-6">
          <Fields count={3} className="grid gap-4 sm:grid-cols-3" />
        </div>
      </div>
      <Panel>
        <Skeleton className="h-6 w-48" />
        <Fields count={2} />
        <Skeleton className="h-48 w-full" />
      </Panel>
    </LoadingFrame>
  );
}

export function StaffShopDetailSkeleton() {
  return (
    <LoadingFrame label="Loading store information">
      <Heading />
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 sm:gap-4">
        {[0, 1, 2, 3].map((index) => (
          <Panel key={index}>
            <Skeleton className="h-4 w-24" />
            <Skeleton className="h-7 w-full" />
          </Panel>
        ))}
      </div>
      <div className="overflow-hidden rounded-xl border border-neutral-200 bg-white">
        <Skeleton className="h-52 w-full rounded-none" />
        <div className="space-y-5 p-6">
          <Skeleton className="h-7 w-56" />
          <Fields className="grid gap-4 md:grid-cols-2" />
        </div>
      </div>
      <Panel>
        <Skeleton className="h-6 w-48" />
        <Fields count={3} className="grid gap-4 md:grid-cols-3" />
      </Panel>
    </LoadingFrame>
  );
}
