interface ProgressBarProps {
  percent: number;
  label: string;
  /** Text on the right, e.g. "35 / 80". */
  valueText?: string;
  complete?: boolean;
}

export function ProgressBar({ percent, label, valueText, complete }: ProgressBarProps) {
  const safePercent = Math.min(Math.max(percent, 0), 100);
  const isComplete = complete ?? safePercent >= 100;

  return (
    <div>
      <div className="mb-1.5 flex items-baseline justify-between gap-3 text-sm">
        <span className="font-medium text-text-primary">{label}</span>
        {valueText && <span className="text-text-secondary tabular-nums">{valueText}</span>}
      </div>
      <div
        role="progressbar"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(safePercent)}
        className="h-2 overflow-hidden rounded-full bg-neutral-100"
      >
        <div
          className={`h-full rounded-full transition-[width] duration-500 ${
            isComplete ? "bg-primary-500" : "bg-accent-500"
          }`}
          style={{ width: `${safePercent}%` }}
        />
      </div>
    </div>
  );
}
