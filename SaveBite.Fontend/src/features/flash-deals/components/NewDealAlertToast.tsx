import { useEffect, useState } from "react";
import { ArrowUp, Flame, Sparkles, X } from "lucide-react";

interface NewDealAlertToastProps {
  newDealsCount: number;
  onViewNewDeals: () => void;
  onDismiss?: () => void;
}

export function NewDealAlertToast({
  newDealsCount,
  onViewNewDeals,
  onDismiss,
}: NewDealAlertToastProps) {
  const [isDismissed, setIsDismissed] = useState(false);

  useEffect(() => {
    if (newDealsCount > 0) {
      setIsDismissed(false);
    }
  }, [newDealsCount]);

  if (newDealsCount <= 0 || isDismissed) {
    return null;
  }

  const handleDismiss = (e: React.MouseEvent) => {
    e.stopPropagation();
    setIsDismissed(true);
    onDismiss?.();
  };

  return (
    <aside
      aria-label="New flash deal notification"
      className="fixed bottom-8 left-1/2 z-50 flex max-w-[calc(100vw-2rem)] -translate-x-1/2 items-center gap-3 rounded-2xl border border-emerald-500/30 bg-white/95 p-3 shadow-[0_20px_50px_rgba(16,185,129,0.18),0_4px_16px_rgba(0,0,0,0.08)] backdrop-blur-xl transition-all duration-300 hover:scale-[1.02] hover:shadow-[0_25px_60px_rgba(16,185,129,0.25)] dark:bg-slate-900/95 sm:px-4 sm:py-3.5 animate-in fade-in slide-in-from-bottom-8 duration-300"
    >
      {/* Icon Badge with Ambient Glow and Live Indicator */}
      <div className="relative shrink-0">
        <div className="flex size-11 items-center justify-center rounded-xl bg-gradient-to-tr from-amber-500 via-orange-500 to-emerald-500 shadow-md shadow-orange-500/25 ring-2 ring-white/60 dark:ring-slate-800">
          <Flame className="size-6 text-white fill-amber-200 animate-pulse" aria-hidden="true" />
        </div>
        {/* Live ping dot */}
        <span className="absolute -right-1 -top-1 flex size-3">
          <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75" />
          <span className="relative inline-flex size-3 rounded-full border-2 border-white bg-emerald-500 dark:border-slate-900" />
        </span>
      </div>

      {/* Content */}
      <button
        type="button"
        onClick={onViewNewDeals}
        className="flex min-w-0 flex-1 cursor-pointer flex-col text-left focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 rounded-lg pr-1"
      >
        <div className="flex items-center gap-1.5">
          <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-extrabold uppercase tracking-wider text-emerald-700 ring-1 ring-inset ring-emerald-600/20 dark:bg-emerald-950/60 dark:text-emerald-400">
            <Sparkles className="size-3 shrink-0" aria-hidden="true" />
            New Deals Just In
          </span>
        </div>
        <p className="mt-0.5 truncate text-xs font-semibold text-slate-900 dark:text-white sm:text-sm">
          <strong className="font-extrabold text-emerald-600 dark:text-emerald-400">+{newDealsCount}</strong> new deal{newDealsCount > 1 ? "s" : ""} just dropped!
        </p>
        <p className="hidden text-[11px] text-slate-500 dark:text-slate-400 sm:block">
          Click to discover the latest dishes
        </p>
      </button>

      {/* Action CTA Button */}
      <button
        type="button"
        onClick={onViewNewDeals}
        className="inline-flex shrink-0 items-center gap-1.5 rounded-xl bg-gradient-to-r from-emerald-600 to-primary-700 px-3.5 py-2 text-xs font-bold text-white shadow-md shadow-emerald-600/30 transition-all duration-200 hover:from-emerald-700 hover:to-primary-800 hover:scale-105 active:scale-95"
      >
        <span>View now</span>
        <ArrowUp className="size-3.5" aria-hidden="true" />
      </button>

      {/* Dismiss Button */}
      <button
        type="button"
        onClick={handleDismiss}
        aria-label="Dismiss notification"
        className="shrink-0 rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
      >
        <X className="size-4" aria-hidden="true" />
      </button>
    </aside>
  );
}
