import { useState } from "react";
import {
  Activity,
  ArrowRight,
  Ban,
  CheckCircle2,
  ChevronDown,
  Clock3,
  PauseCircle,
  ScrollText,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { ShopStatusHistory as StatusHistoryItem } from "../types/shopManagement.types";

interface ShopStatusHistoryProps {
  history: StatusHistoryItem[];
}
const VISIBLE_COUNT = 8;
const styles: Record<string, { icon: LucideIcon; circle: string; badge: string; border: string }> =
  {
    Active: {
      icon: CheckCircle2,
      circle: "bg-emerald-50 text-emerald-700 ring-emerald-200",
      badge: "bg-emerald-50 text-emerald-800 ring-emerald-200",
      border: "border-emerald-200",
    },
    Paused: {
      icon: PauseCircle,
      circle: "bg-amber-50 text-amber-700 ring-amber-200",
      badge: "bg-amber-50 text-amber-900 ring-amber-200",
      border: "border-amber-200",
    },
    Suspended: {
      icon: Ban,
      circle: "bg-rose-50 text-rose-700 ring-rose-200",
      badge: "bg-rose-50 text-rose-800 ring-rose-200",
      border: "border-rose-200",
    },
  };
const fallback = {
  icon: Activity,
  circle: "bg-neutral-100 text-neutral-600 ring-neutral-200",
  badge: "bg-neutral-100 text-neutral-700 ring-neutral-200",
  border: "border-neutral-200",
};
const dateOf = (value: string) => {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
};
const monthOf = (value: string) =>
  dateOf(value)?.toLocaleDateString("en-US", { month: "long", year: "numeric" }) ?? "Unknown date";

function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={`inline-flex items-center rounded-lg px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${(styles[status] ?? fallback).badge}`}
    >
      {status}
    </span>
  );
}

export function ShopStatusHistory({ history }: ShopStatusHistoryProps) {
  const [expanded, setExpanded] = useState(false);
  const entries = [...history].sort(
    (a, b) => (dateOf(b.createdAt)?.getTime() ?? 0) - (dateOf(a.createdAt)?.getTime() ?? 0),
  );
  const hiddenCount = Math.max(0, entries.length - VISIBLE_COUNT);
  const visible = expanded ? entries : entries.slice(0, VISIBLE_COUNT);
  const groups: { label: string; items: StatusHistoryItem[] }[] = [];
  for (const entry of visible) {
    const label = monthOf(entry.createdAt);
    const last = groups[groups.length - 1];
    if (last?.label === label) last.items.push(entry);
    else groups.push({ label, items: [entry] });
  }

  return (
    <section
      aria-label="Shop status history"
      className="overflow-hidden rounded-3xl bg-white shadow-sm ring-1 ring-gray-200/70"
    >
      <div className="flex flex-wrap items-center justify-between gap-4 border-b border-border-default px-5 py-5 sm:px-7">
        <div className="flex items-center gap-3">
          <span className="flex size-11 items-center justify-center rounded-2xl bg-primary-50 text-primary-700">
            <ScrollText className="size-5" aria-hidden="true" />
          </span>
          <div>
            <h2 className="text-lg font-bold text-text-primary">Status history</h2>
            <p className="mt-0.5 text-sm text-text-secondary">A timeline of moderation decisions</p>
          </div>
        </div>
        <span className="rounded-full bg-neutral-100 px-3 py-1 text-xs font-semibold tabular-nums text-text-secondary">
          {entries.length} {entries.length === 1 ? "change" : "changes"}
        </span>
      </div>
      {entries.length === 0 ? (
        <div className="flex flex-col items-center gap-3 px-6 py-20 text-center">
          <span className="flex size-14 items-center justify-center rounded-2xl bg-neutral-100 text-text-muted">
            <Clock3 className="size-7" />
          </span>
          <p className="font-semibold text-text-primary">No status changes yet</p>
          <p className="max-w-sm text-sm text-text-secondary">
            When an admin changes this shop’s status, the event and its reason will appear here.
          </p>
        </div>
      ) : (
        <div className="space-y-8 px-5 py-6 sm:px-7">
          {groups.map((group) => (
            <section key={group.label} aria-label={group.label}>
              <div className="mb-5 flex items-center gap-3">
                <h3 className="shrink-0 text-xs font-bold uppercase tracking-widest text-text-secondary">
                  {group.label}
                </h3>
                <span className="h-px flex-1 bg-border-default" />
              </div>
              <ol className="space-y-0">
                {group.items.map((item, index) => {
                  const date = dateOf(item.createdAt);
                  const style = styles[item.toStatus] ?? fallback;
                  const Icon = style.icon;
                  return (
                    <li key={item.id} className="relative flex gap-4 pb-5 last:pb-0 sm:gap-5">
                      {index < group.items.length - 1 && (
                        <span
                          aria-hidden="true"
                          className="absolute bottom-0 left-[19px] top-11 w-px bg-neutral-200"
                        />
                      )}
                      <span
                        className={`relative z-10 flex size-10 shrink-0 items-center justify-center rounded-full ring-1 ring-inset ${style.circle}`}
                      >
                        <Icon className="size-5" aria-hidden="true" />
                      </span>
                      <div className="min-w-0 flex-1 rounded-2xl border border-border-default bg-white p-4 transition-colors hover:bg-neutral-50/60 sm:p-5">
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <div className="flex flex-wrap items-center gap-2">
                            <StatusBadge status={item.fromStatus ?? "Unknown"} />
                            <ArrowRight className="size-4 text-text-muted" aria-hidden="true" />
                            <StatusBadge status={item.toStatus} />
                            {item.id === entries[0]?.id && (
                              <span className="rounded-full bg-primary-50 px-2 py-0.5 text-[11px] font-semibold text-primary-700">
                                Latest
                              </span>
                            )}
                          </div>
                          <time
                            dateTime={item.createdAt}
                            className="text-xs tabular-nums text-text-muted"
                          >
                            {date
                              ? date.toLocaleString("en-US", {
                                  dateStyle: "medium",
                                  timeStyle: "short",
                                })
                              : item.createdAt}
                          </time>
                        </div>
                        {item.reason && (
                          <div
                            className={`mt-4 rounded-xl border-l-[3px] ${style.border} bg-neutral-50 px-3.5 py-3`}
                          >
                            <p className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-text-muted">
                              Reason
                            </p>
                            <p className="whitespace-pre-wrap break-words text-sm leading-relaxed text-text-primary">
                              {item.reason}
                            </p>
                          </div>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ol>
            </section>
          ))}
          {hiddenCount > 0 && (
            <div className="border-t border-border-default pt-5 text-center">
              <button
                type="button"
                onClick={() => setExpanded((value) => !value)}
                aria-expanded={expanded}
                className="inline-flex items-center gap-2 rounded-xl border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-text-primary transition hover:bg-neutral-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
              >
                {expanded ? "Show fewer" : `Show ${hiddenCount} older changes`}
                <ChevronDown
                  className={`size-4 transition-transform ${expanded ? "rotate-180" : ""}`}
                />
              </button>
            </div>
          )}
        </div>
      )}
    </section>
  );
}
