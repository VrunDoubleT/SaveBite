import { EmptyState, ErrorState, Skeleton } from "@/shared/components/ui";
export function PanelSkeleton({ rows = 3 }: { rows?: number }) {
  return (
    <div role="status" aria-label="Loading trust information" className="space-y-3">
      {Array.from({ length: rows }, (_, index) => (
        <Skeleton key={index} className="h-24" />
      ))}
    </div>
  );
}
export function PanelError({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <ErrorState title="Couldn't load trust information" description={message} onRetry={onRetry} />
  );
}
export function PanelEmpty({ title, description }: { title: string; description?: string }) {
  return <EmptyState title={title} description={description} />;
}
