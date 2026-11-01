import { CalendarDays } from "lucide-react";
import type { ReactNode } from "react";

interface DashboardHeaderProps {
  title: string;
  subtitle: string;
  periodLabel?: string;
  actions: ReactNode;
}

export function DashboardHeader({ title, subtitle, periodLabel, actions }: DashboardHeaderProps) {
  return (
    <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
      <div className="min-w-0">
        <h1 className="text-3xl font-semibold tracking-tight text-neutral-900">{title}</h1>
        <p className="mt-1 text-sm text-neutral-500">{subtitle}</p>
        {periodLabel && (
          <p className="mt-3 inline-flex items-center gap-2 rounded-full border border-neutral-200 bg-white px-3 py-1 text-sm text-neutral-700 shadow-sm">
            <CalendarDays className="size-4 text-primary-600" />
            <span className="tabular-nums">{periodLabel}</span>
          </p>
        )}
      </div>
      {actions}
    </header>
  );
}
