import { RULE_STATUS_LABELS, type RuleStatus } from "../utils/ruleStatus";

const STYLES: Record<RuleStatus, string> = {
  current: "border-primary-100 bg-primary-50 text-primary-700",
  scheduled: "border-info/20 bg-info/10 text-info",
  disabled: "border-neutral-200 bg-neutral-50 text-neutral-600",
  replaced: "border-neutral-200 bg-neutral-50 text-text-muted",
};

export function RuleStatusBadge({ status }: { status: RuleStatus }) {
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-1 text-[11px] font-semibold ${STYLES[status]}`}
    >
      {RULE_STATUS_LABELS[status]}
    </span>
  );
}
