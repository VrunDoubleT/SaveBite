import { AlertCircle } from "lucide-react";

export function PanelSkeleton({ rows = 3 }: { rows?: number }) {
  return (
    <div className="space-y-3" aria-busy="true" aria-live="polite">
      {Array.from({ length: rows }, (_, index) => (
        <div key={index} className="h-20 animate-pulse rounded-lg bg-neutral-100" />
      ))}
    </div>
  );
}

interface PanelErrorProps {
  message: string;
  onRetry?: () => void;
}

export function PanelError({ message, onRetry }: PanelErrorProps) {
  return (
    <div
      role="alert"
      className="flex flex-col items-center gap-3 rounded-lg border border-danger/20 bg-danger/5 p-8 text-center"
    >
      <AlertCircle className="size-8 text-danger" />
      <p className="text-sm text-text-primary">{message}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="rounded-md border border-border-default bg-white px-4 py-2 text-sm font-semibold text-text-secondary transition-colors hover:bg-neutral-50"
        >
          Try again
        </button>
      )}
    </div>
  );
}

export function PanelEmpty({ title, description }: { title: string; description?: string }) {
  return (
    <div className="rounded-lg border border-dashed border-border-default p-10 text-center">
      <p className="font-semibold text-text-primary">{title}</p>
      {description && <p className="mt-1 text-sm text-text-secondary">{description}</p>}
    </div>
  );
}
