import { TONE, toneForStatus, type Tone } from "./tones";

interface StatusBadgeProps {
  status: string;
  tone?: Tone;
}

export function StatusBadge({ status, tone }: StatusBadgeProps) {
  const resolved = tone ?? toneForStatus(status);
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${TONE[resolved].soft}`}
    >
      <span className={`size-1.5 rounded-full ${TONE[resolved].dot}`} aria-hidden="true" />
      {status}
    </span>
  );
}
