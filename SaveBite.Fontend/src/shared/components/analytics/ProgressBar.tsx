import { TONE, type Tone } from "./tones";

interface ProgressBarProps {
  percent: number;
  tone?: Tone;
  label: string;
}

export function ProgressBar({ percent, tone = "primary", label }: ProgressBarProps) {
  const clamped = Math.max(0, Math.min(100, percent));
  return (
    <div
      className="h-2 w-full overflow-hidden rounded-full bg-neutral-100"
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={Math.round(clamped)}
    >
      <div className={`h-full rounded-full ${TONE[tone].bar}`} style={{ width: `${clamped}%` }} />
    </div>
  );
}
