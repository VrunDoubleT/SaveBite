import type { ReactNode } from "react";

/** Vertical timeline container (the left border line). */
export function Timeline({ children }: { children: ReactNode }) {
  return (
    <div className="relative pl-3 sm:pl-4 border-l-2 border-neutral-200 space-y-6 ml-2">
      {children}
    </div>
  );
}

/** One timeline entry: a dot on the line + a card with the content. */
export function TimelineItem({ children }: { children: ReactNode }) {
  return (
    <div className="relative">
      <div className="absolute -left-5.25 sm:-left-6.25 top-1.5 size-3.5 rounded-full bg-white border-[3px] border-primary-500 shadow-sm" />
      <div className="bg-white border border-border-default rounded-lg p-4 shadow-sm ml-2 hover:border-primary-200 transition-colors">
        {children}
      </div>
    </div>
  );
}
