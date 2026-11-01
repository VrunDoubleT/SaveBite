import type { LucideIcon } from "lucide-react";

interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description?: string;
}

export function EmptyState({ icon: Icon, title, description }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center rounded-md border border-dashed border-neutral-200 bg-neutral-50/50 px-4 py-10 text-center">
      <span className="flex size-12 items-center justify-center rounded-full bg-white text-neutral-400 shadow-sm ring-1 ring-neutral-200">
        <Icon className="size-6" />
      </span>
      <p className="mt-3 text-sm font-semibold text-neutral-800">{title}</p>
      {description && <p className="mt-1 max-w-sm text-sm text-neutral-500">{description}</p>}
    </div>
  );
}
