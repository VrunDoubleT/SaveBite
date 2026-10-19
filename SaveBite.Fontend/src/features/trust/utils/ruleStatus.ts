export type RuleStatus = "current" | "scheduled" | "disabled" | "replaced";

interface RuleLike {
  isActive?: boolean;
  isCurrent: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
}

export const RULE_STATUS_LABELS: Record<RuleStatus, string> = {
  current: "In effect",
  scheduled: "Scheduled",
  disabled: "Disabled",
  replaced: "Replaced",
};

/** `now` is passed in (epoch ms) so render stays pure. */
export function getRuleStatus(rule: RuleLike, now: number): RuleStatus {
  if (rule.isCurrent) return "current";
  if (rule.isActive === false && !rule.effectiveTo) return "disabled";
  if (new Date(rule.effectiveFrom).getTime() > now) return "scheduled";
  return "replaced";
}

/** Only the latest version of a rule (no end date) can be edited. */
export function isEditableVersion(rule: { effectiveTo?: string | null }): boolean {
  // The API omits null fields from the JSON, so a missing value means "no end date".
  return !rule.effectiveTo;
}
