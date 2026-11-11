interface ProgressBarProps {
  percent: number;
  label: string;
  /** Text on the right, e.g. "35 / 80". */
  valueText?: string;
  complete?: boolean;
}

export function ProgressBar({ percent, label, valueText, complete }: ProgressBarProps) {
  const safePercent = Number.isFinite(percent) ? Math.min(Math.max(percent, 0), 100) : 0;
  const isComplete = complete ?? safePercent >= 100;

  return (
    <div>
      <div className="mb-2 flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1 text-body">
        <span className="font-medium text-text-primary">{label}</span>
        {valueText && (
          <span className="text-text-secondary tabular-nums [overflow-wrap:anywhere]">
            {valueText}
          </span>
        )}
      </div>
      <div
        role="progressbar"
        aria-label={label}
        aria-valuetext={valueText}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(safePercent)}
        className="h-2.5 overflow-hidden rounded-full bg-neutral-100"
      >
        <div
          className={`h-full rounded-full transition-[width] duration-500 motion-reduce:transition-none ${
            isComplete ? "bg-primary-600" : "bg-primary-500"
          }`}
          style={{ width: `${safePercent}%` }}
        />
      </div>
    </div>
  );
}
