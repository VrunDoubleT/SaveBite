import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { EmptyState, ErrorState } from "@/shared/components/ui";
export function PanelSkeleton({ rows = 3 }: { rows?: number }) {
  return <LoadingSkeleton rows={rows} label="Loading trust information" />;
}
export function PanelError({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <ErrorState title="Couldn't load trust information" description={message} onRetry={onRetry} />
  );
}
export function PanelEmpty({ title, description }: { title: string; description?: string }) {
  return <EmptyState title={title} description={description} />;
}
