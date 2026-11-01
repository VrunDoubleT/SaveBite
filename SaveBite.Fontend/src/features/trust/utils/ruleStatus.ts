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
  replaced: "Expired / Replaced",
};

/** `now` is passed in (epoch ms) so render stays pure. */
export function getRuleStatus(rule: RuleLike, now: number): RuleStatus {
  const fromMs = new Date(rule.effectiveFrom).getTime();
  const toMs = rule.effectiveTo ? new Date(rule.effectiveTo).getTime() : null;

  if (toMs !== null && toMs <= now) return "replaced";
  if (rule.isActive === false) return "disabled";
  if (fromMs > now) return "scheduled";
  if (rule.isCurrent || (fromMs <= now && (toMs === null || toMs > now))) return "current";
  return "replaced";
}

/** True when the rule starts in the future and can be deleted. */
export function isFutureRule(rule: { effectiveFrom: string }, now: number = Date.now()): boolean {
  return new Date(rule.effectiveFrom).getTime() > now;
}

/** Only the latest version of a score rule (no end date) can be edited. */
export function isEditableVersion(rule: { effectiveTo?: string | null }): boolean {
  // The API omits null fields from the JSON, so a missing value means "no end date".
  return !rule.effectiveTo;
}