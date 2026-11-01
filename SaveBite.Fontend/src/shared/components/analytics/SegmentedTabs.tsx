import type { LucideIcon } from "lucide-react";
import { Link, type To } from "react-router-dom";

export interface TabItem<T extends string> {
  id: T;
  label: string;
  icon: LucideIcon;
}

interface SegmentedTabsProps<T extends string> {
  items: TabItem<T>[];
  value: T;
  onChange?: (value: T) => void;
  label: string;
  /** When given, every tab is a real link (its own URL) instead of a button. */
  getTo?: (value: T) => To;
}

/** Pill tabs with icons. Scrolls sideways on narrow screens instead of wrapping. */
export function SegmentedTabs<T extends string>({ items, value, onChange, label, getTo }: SegmentedTabsProps<T>) {
  return (
    <div
      role="tablist"
      aria-label={label}
      className="flex max-w-full gap-1 overflow-x-auto rounded-lg border border-neutral-200 bg-white p-1 shadow-sm"
    >
      {items.map(({ id, label: text, icon: Icon }) => {
        const active = value === id;
        const className = `inline-flex shrink-0 items-center gap-2 rounded-md px-3.5 py-2 text-sm font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500 ${
          active
            ? "bg-primary-600 text-white shadow-sm"
            : "text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900"
        }`;
        const content = (
          <>
            <Icon className="size-4" />
            {text}
          </>
        );

        return getTo ? (
          <Link key={id} to={getTo(id)} role="tab" aria-selected={active} className={className}>
            {content}
          </Link>
        ) : (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={active}
            onClick={() => onChange?.(id)}
            className={className}
          >
            {content}
          </button>
        );
      })}
    </div>
  );
}
