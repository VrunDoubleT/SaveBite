import type { MouseEvent } from "react";
import { toast } from "@/shared/stores/toastStore";

interface RecordIdProps {
  id: string;
  variant?: "compact" | "full";
  className?: string;
}

export function RecordId({ id, variant = "compact", className = "" }: RecordIdProps) {
  const copyId = async () => {
    try {
      await navigator.clipboard.writeText(id);
      toast.success("ID copied to clipboard.");
    } catch {
      toast.error("Unable to copy ID. Check clipboard permissions and try again.");
    }
  };

  const stopPropagation = (event: MouseEvent<HTMLButtonElement>) => {
    event.stopPropagation();
    void copyId();
  };

  if (variant === "full") {
    return (
      <div className={`flex min-w-0 max-w-full items-center gap-2 overflow-x-auto whitespace-nowrap ${className}`}>
        <button
          type="button"
          onClick={stopPropagation}
          title="Click to copy full ID"
          aria-label={`Copy ID: ${id}`}
          className="inline-flex shrink-0 items-center rounded-lg border border-primary-200 bg-primary-50 px-3 py-1.5 text-left font-mono text-xs font-medium tracking-wide text-primary-800 shadow-sm transition-colors hover:border-primary-300 hover:bg-primary-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-primary-600"
        >
          <code className="whitespace-nowrap" title={id}>
            {id}
          </code>
        </button>
      </div>
    );
  }

  const displayId = id.length > 8 ? `${id.slice(0, 4)}...${id.slice(-4)}` : id;

  return (
    <button
      type="button"
      onClick={stopPropagation}
      title="Click to copy full ID"
      aria-label={`Copy ID: ${id}`}
      className={`inline-flex max-w-full cursor-copy items-center rounded-lg border border-primary-100 bg-primary-50/70 px-2.5 py-1.5 text-left font-mono text-[11px] font-semibold tracking-wide text-primary-700 shadow-sm transition-all hover:border-primary-300 hover:bg-primary-100 hover:text-primary-800 hover:shadow focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-primary-600 ${className}`}
    >
      <span className="truncate" title={id}>
        {displayId}
      </span>
    </button>
  );
}
