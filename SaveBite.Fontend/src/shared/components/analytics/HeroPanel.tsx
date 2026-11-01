import type { ReactNode } from "react";

export interface HeroFormulaItem {
  label: string;
  value: string;
  /** minus: deducted from the previous figure. */
  op?: "minus";
}

export interface HeroStat {
  label: string;
  value: string;
  hint?: string;
}

interface HeroPanelProps {
  title: string;
  description?: string;
  headlineLabel: string;
  headline: string;
  /** How the headline is made up, read left to right. */
  formula?: HeroFormulaItem[];
  stats: HeroStat[];
  /** Normally an <AreaTrend onDark />. */
  children: ReactNode;
}

/**
 * The one high-emphasis block of a dashboard: the number the page exists to show, how it is made up,
 * and its trend. Everything else on the page stays light so this is what the eye lands on first.
 */
export function HeroPanel({
  title,
  description,
  headlineLabel,
  headline,
  formula,
  stats,
  children,
}: HeroPanelProps) {
  return (
    <section className="relative overflow-hidden rounded-lg bg-gradient-to-br from-primary-900 via-primary-800 to-primary-700 text-white shadow-lg">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -right-24 -top-24 size-80 rounded-full bg-primary-400/20 blur-3xl"
      />
      <div className="relative grid gap-8 p-6 sm:p-8 lg:grid-cols-5">
        <div className="flex min-w-0 flex-col justify-between gap-6 lg:col-span-2">
          <div>
          <h2 className="text-sm font-medium text-primary-100">{title}</h2>
          {description && <p className="mt-1 max-w-sm text-sm leading-relaxed text-white/60">{description}</p>}

          <p className="mt-6 text-sm text-primary-100">{headlineLabel}</p>
          <p className="mt-1 break-words text-4xl font-semibold tabular-nums tracking-tight sm:text-5xl">
            {headline}
          </p>
          </div>

          {formula && formula.length > 0 && (
            <ul className="flex flex-wrap items-stretch gap-2">
              {formula.map((item, index) => (
                <li
                  key={`${item.label}-${index}`}
                  className="flex items-center gap-2 rounded-md bg-white/10 px-3 py-2 ring-1 ring-inset ring-white/15"
                >
                  {index > 0 && (
                    <span className="text-xl font-bold leading-none text-accent-300" aria-hidden="true">
                      {item.op === "minus" ? "−" : "+"}
                    </span>
                  )}
                  <span>
                    <span className="block text-xs text-primary-100">{item.label}</span>
                    <span className="block text-sm font-semibold tabular-nums">{item.value}</span>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="min-w-0 lg:col-span-3">{children}</div>
      </div>

      <dl className="relative grid grid-cols-2 border-t border-white/15 bg-black/10 sm:grid-cols-4">
        {stats.map((stat) => (
          <div key={stat.label} className="min-w-0 px-6 py-4 sm:px-8">
            <dt className="text-xs text-primary-100">{stat.label}</dt>
            <dd className="mt-0.5 truncate text-lg font-semibold tabular-nums">{stat.value}</dd>
            {stat.hint && <p className="text-xs text-white/50">{stat.hint}</p>}
          </div>
        ))}
      </dl>
    </section>
  );
}
