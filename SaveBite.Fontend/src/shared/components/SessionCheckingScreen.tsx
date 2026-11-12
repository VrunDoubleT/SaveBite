import { Skeleton } from "@/shared/components/ui/Content";
import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";

interface SessionCheckingScreenProps {
  message?: string;
  subMessage?: string;
  minHeight?: string;
}

export function SessionCheckingScreen({
  message = "Checking your session",
  subMessage = "Preparing your SaveBite experience",
  minHeight = "min-h-screen",
}: SessionCheckingScreenProps) {
  return (
    <div
      role="status"
      aria-label={message}
      className={`${minHeight} w-full bg-neutral-50 p-4 sm:p-6`}
    >
      <span className="sr-only">
        {message}. {subMessage}
      </span>
      <div aria-hidden="true" className="mx-auto max-w-7xl space-y-6">
        <div className="flex items-center justify-between gap-4 rounded-lg border border-border-default bg-bg-surface p-4">
          <Skeleton className="h-8 w-32" />
          <Skeleton className="size-10 rounded-full" />
        </div>
        <div className="space-y-3 py-4">
          <Skeleton className="h-8 w-64 max-w-full" />
          <Skeleton className="h-4 w-96 max-w-full" />
        </div>
        <LoadingSkeleton variant="stats" rows={3} />
        <LoadingSkeleton variant="detail" rows={2} />
      </div>
    </div>
  );
}
