import { z } from "zod";

const MAX_COUNT = 1_000_000;
const MAX_VALUE = 9_999_999_999;
const MAX_POINTS = 1_000;

const wholeNumber = (label: string) =>
  z.number({ error: `${label} is required` }).int(`${label} must be a whole number`);

const depositRangeItemSchema = z.object({
  fromOrderAmount: wholeNumber("From amount")
    .min(1, "Must be greater than 0")
    .max(MAX_VALUE, "Too large"),
  toOrderAmount: wholeNumber("Up to amount")
    .min(1, "Must be greater than 0")
    .max(MAX_VALUE, "Too large"),
  depositAmount: wholeNumber("Deposit amount")
    .min(1, "Must be greater than 0")
    .max(MAX_VALUE, "Too large"),
});

export const levelBenefitsSchema = z
  .object({
    description: z.string().max(2000, "Keep the benefits under 2000 characters"),
    requireFullOnlinePayment: z.boolean(),
    depositFreeOrderAmount: z.number().nullable().optional(),
    maxDepositOrderAmount: z.number().nullable().optional(),
    ranges: z.array(depositRangeItemSchema),
  })
  .superRefine((data, ctx) => {
    if (data.requireFullOnlinePayment) {
      return;
    }

    const freeAmount = data.depositFreeOrderAmount;
    const maxAmount = data.maxDepositOrderAmount;

    if (freeAmount === null || freeAmount === undefined || Number.isNaN(freeAmount)) {
      ctx.addIssue({
        code: "custom",
        path: ["depositFreeOrderAmount"],
        message: "Deposit-free order amount is required",
      });
    } else if (!Number.isInteger(freeAmount) || freeAmount <= 0 || freeAmount > MAX_VALUE) {
      ctx.addIssue({
        code: "custom",
        path: ["depositFreeOrderAmount"],
        message: "Must be a positive whole VND amount",
      });
    }

    if (maxAmount === null || maxAmount === undefined || Number.isNaN(maxAmount)) {
      ctx.addIssue({
        code: "custom",
        path: ["maxDepositOrderAmount"],
        message: "Maximum deposit order amount is required",
      });
    } else if (!Number.isInteger(maxAmount) || maxAmount <= 0 || maxAmount > MAX_VALUE) {
      ctx.addIssue({
        code: "custom",
        path: ["maxDepositOrderAmount"],
        message: "Must be a positive whole VND amount",
      });
    } else if (
      typeof freeAmount === "number" &&
      !Number.isNaN(freeAmount) &&
      maxAmount <= freeAmount
    ) {
      ctx.addIssue({
        code: "custom",
        path: ["maxDepositOrderAmount"],
        message: "Must be greater than the deposit-free order amount",
      });
    }

    if (data.ranges.length === 0) {
      ctx.addIssue({
        code: "custom",
        path: ["ranges"],
        message: "Add at least one deposit range when Pay at shop is enabled",
      });
      return;
    }

    for (let i = 0; i < data.ranges.length; i++) {
      const range = data.ranges[i];

      if (range.toOrderAmount <= range.fromOrderAmount) {
        ctx.addIssue({
          code: "custom",
          path: ["ranges", i, "toOrderAmount"],
          message: "Must be greater than the range start amount",
        });
      }

      if (range.depositAmount > range.fromOrderAmount) {
        ctx.addIssue({
          code: "custom",
          path: ["ranges", i, "depositAmount"],
          message: "Cannot exceed the range start amount",
        });
      }

      if (i === 0 && typeof freeAmount === "number" && !Number.isNaN(freeAmount)) {
        if (range.fromOrderAmount !== freeAmount) {
          ctx.addIssue({
            code: "custom",
            path: ["ranges", i, "fromOrderAmount"],
            message: `First range must start at ${freeAmount.toLocaleString("vi-VN")} VND`,
          });
        }
      }

      if (i > 0 && range.fromOrderAmount !== data.ranges[i - 1].toOrderAmount) {
        ctx.addIssue({
          code: "custom",
          path: ["ranges", i, "fromOrderAmount"],
          message: `Must equal previous range end (${data.ranges[i - 1].toOrderAmount.toLocaleString("vi-VN")} VND)`,
        });
      }
    }

    const lastIdx = data.ranges.length - 1;
    if (
      typeof maxAmount === "number" &&
      !Number.isNaN(maxAmount) &&
      data.ranges[lastIdx].toOrderAmount !== maxAmount
    ) {
      ctx.addIssue({
        code: "custom",
        path: ["ranges", lastIdx, "toOrderAmount"],
        message: `Last range must end at ${maxAmount.toLocaleString("vi-VN")} VND`,
      });
    }
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