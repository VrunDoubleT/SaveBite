import { z } from "zod";

const MAX_COUNT = 1_000_000;
const MAX_VALUE = 9_999_999_999;
const MAX_POINTS = 1_000;

const wholeNumber = (label: string) =>
  z.number({ error: `${label} is required` }).int(`${label} must be a whole number`);

export const levelBenefitsSchema = z.object({
  description: z.string().max(2000, "Keep the benefits under 2000 characters"),
});
export type LevelBenefitsFormData = z.infer<typeof levelBenefitsSchema>;

export const levelRequirementSchema = z.object({
  minScore: wholeNumber("Minimum score").min(0, "Cannot be negative").max(MAX_COUNT, "Too large"),
  minCompletedOrders: wholeNumber("Minimum completed orders")
    .min(0, "Cannot be negative")
    .max(MAX_COUNT, "Too large"),
  minTotalOrderValue: z
    .number({ error: "Minimum order value is required" })
    .min(0, "Cannot be negative")
    .max(MAX_VALUE, "Too large"),
});
export type LevelRequirementFormData = z.infer<typeof levelRequirementSchema>;

export const penaltyRuleSchema = z.object({
  minimumOccurrence: wholeNumber("Occurrence")
    .min(1, "Must be at least 1")
    .max(100, "Must be at most 100"),
  scoreDeduction: wholeNumber("Score deduction")
    .min(1, "Must be at least 1")
    .max(MAX_POINTS, `Must be at most ${MAX_POINTS}`),
  requiredOnlineOrders: wholeNumber("Required online orders")
    .min(0, "Cannot be negative")
    .max(MAX_COUNT, "Too large"),
  requiredOnlineOrderValue: z
    .number({ error: "Required order value is required" })
    .min(0, "Cannot be negative")
    .max(MAX_VALUE, "Too large"),
  permanentOfflineLock: z.boolean(),
  isActive: z.boolean(),
  /** datetime-local value; empty means "start now". */
  effectiveFrom: z.string(),
});
export type PenaltyRuleFormData = z.infer<typeof penaltyRuleSchema>;

/** An earning rule must stay positive and a penalty rule negative (the API enforces the same). */
export function buildScoreRuleSchema(kind: "Earning" | "Penalty") {
  const points = wholeNumber("Points")
    .min(-MAX_POINTS, `Must be at least -${MAX_POINTS}`)
    .max(MAX_POINTS, `Must be at most ${MAX_POINTS}`);

  return z.object({
    scoreDelta:
      kind === "Earning"
        ? points.min(1, "An earning rule must add at least 1 point")
        : points.max(-1, "A penalty rule must remove at least 1 point"),
    effectiveFrom: z.string(),
  });
}
export type ScoreRuleFormData = { scoreDelta: number; effectiveFrom: string };

/** Converts a datetime-local value into an ISO string, or undefined when empty. */
export function toIsoOrUndefined(value: string): string | undefined {
  if (!value) return undefined;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
}
