import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { TONE, type Tone } from "./tones";

export interface Kpi {
  label: string;
  value: ReactNode;
  hint?: ReactNode;
  icon: LucideIcon;
  tone?: Tone;
}

// Columns follow the width of the panel the grid sits in (container queries), not the browser window,
// so a half-width panel gets fewer columns instead of squeezed cards.
const COLUMNS = {
  2: "grid-cols-1 @sm:grid-cols-2",
  3: "grid-cols-1 @sm:grid-cols-2 @3xl:grid-cols-3",
  4: "grid-cols-1 @sm:grid-cols-2 @4xl:grid-cols-4",
} as const;

/**
 * Icon and label share a fixed-height top row (room for two lines of label), so the numbers of all cards
 * in a row start on the same line whatever the length of their labels.
 */
export function KpiCard({ label, value, hint, icon: Icon, tone = "neutral" }: Kpi) {
  return (
    <div className="flex h-full min-w-0 flex-col gap-3 rounded-md border border-neutral-100 bg-neutral-50/70 p-4">
      <div className="flex min-h-10 items-center gap-3">
        <span className={`flex size-10 shrink-0 items-center justify-center rounded-md ${TONE[tone].icon}`}>
          <Icon className="size-5" />
        </span>
        <p className="min-w-0 text-sm leading-5 text-neutral-500">{label}</p>
      </div>
      <div className="min-w-0">
        <p
          className="text-2xl font-semibold leading-tight tabular-nums tracking-tight text-neutral-900 [overflow-wrap:anywhere]"
          title={typeof value === "string" ? value : undefined}
        >
          {value}
        </p>
        {hint && <p className="mt-1 text-xs leading-snug text-neutral-500">{hint}</p>}
      </div>
    </div>
  );
}

interface KpiGridProps {
  items: Kpi[];
  columns?: keyof typeof COLUMNS;
}

export function KpiGrid({ items, columns = 4 }: KpiGridProps) {
  return (
    <div className="@container">
      <div className={`grid gap-3 ${COLUMNS[columns]}`}>
        {items.map((item) => (
          <KpiCard key={item.label} {...item} />
        ))}
      </div>
    </div>
  );
}
