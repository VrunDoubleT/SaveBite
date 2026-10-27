import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

interface PanelProps {
  title: string;
  description?: string;
  icon?: LucideIcon;
  action?: ReactNode;
  children: ReactNode;
  className?: string;
}

export function Panel({ title, description, icon: Icon, action, children, className = "" }: PanelProps) {
  return (
    <section className={`rounded-lg border border-neutral-200 bg-white shadow-sm ${className}`}>
      <header className="flex flex-wrap items-start justify-between gap-x-6 gap-y-3 px-5 pt-5 sm:px-6 sm:pt-6">
        <div className="flex min-w-0 items-start gap-3">
          {Icon && (
            <span className="flex size-10 shrink-0 items-center justify-center rounded-md bg-primary-50 text-primary-700">
              <Icon className="size-5" />
            </span>
          )}
          <div className="min-w-0">
            <h2 className="text-lg font-semibold tracking-tight text-neutral-900">{title}</h2>
            {description && (
              <p className="mt-0.5 max-w-2xl text-sm leading-relaxed text-neutral-500">{description}</p>
            )}
          </div>
        </div>
        {action && <div className="shrink-0">{action}</div>}
      </header>
      <div className="space-y-6 px-5 pb-5 pt-5 sm:px-6 sm:pb-6">{children}</div>
    </section>
  );
}
