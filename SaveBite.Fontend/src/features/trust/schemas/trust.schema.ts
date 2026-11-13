import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import type { FieldErrors, Resolver } from "react-hook-form";

export function parseNumeric(v: unknown): number | undefined {
  if (typeof v === "number" && !Number.isNaN(v)) return v;
  if (typeof v === "string" && v.trim() !== "") {
    const parsed = Number(v);
    if (!Number.isNaN(parsed)) return parsed;
  }
  return undefined;
}

const MAX_COUNT = 1_000_000;
const MAX_VALUE = 9_999_999_999;
const MAX_POINTS = 1_000;

const wholeNumber = (label: string) =>
  z.number({ error: `${label} is required` }).int(`${label} must be a whole number`);

const depositRangeItemSchema = z.object({
  fromOrderAmount: z.number().nullable().optional(),
  toOrderAmount: z.number().nullable().optional(),
  depositAmount: z.number().nullable().optional(),
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
    } else if (freeAmount % 1000 !== 0) {
      ctx.addIssue({
        code: "custom",
        path: ["depositFreeOrderAmount"],
        message: "Must be a multiple of 1,000 VND",
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
    } else if (maxAmount % 1000 !== 0) {
      ctx.addIssue({
        code: "custom",
        path: ["maxDepositOrderAmount"],
        message: "Must be a multiple of 1,000 VND",
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

      if (
        range.toOrderAmount !== null &&
        range.toOrderAmount !== undefined &&
        range.fromOrderAmount !== null &&
        range.fromOrderAmount !== undefined &&
        range.toOrderAmount <= range.fromOrderAmount
      ) {
        ctx.addIssue({
          code: "custom",
          path: ["ranges", i, "toOrderAmount"],
          message: "Must be greater than the range start amount",
        });
      }

      if (
        range.depositAmount !== null &&
        range.depositAmount !== undefined &&
        range.fromOrderAmount !== null &&
        range.fromOrderAmount !== undefined &&
        range.depositAmount > range.fromOrderAmount
      ) {
        ctx.addIssue({
          code: "custom",
          path: ["ranges", i, "depositAmount"],
          message: "Cannot exceed the range start amount",
        });
      } else if (
        range.depositAmount !== null &&
        range.depositAmount !== undefined &&
        range.depositAmount < 2000
      ) {
        ctx.addIssue({
          code: "custom",
          path: ["ranges", i, "depositAmount"],
          message: "Deposit amount must be at least 2,000 VND",
        });
      }

      if (i === 0 && typeof freeAmount === "number" && !Number.isNaN(freeAmount)) {
        if (
          range.fromOrderAmount !== null &&
          range.fromOrderAmount !== undefined &&
          range.fromOrderAmount !== freeAmount
        ) {
          ctx.addIssue({
            code: "custom",
            path: ["ranges", i, "fromOrderAmount"],
            message: `First range must start at ${freeAmount.toLocaleString("vi-VN")} VND`,
          });
        }
      }

      if (
        i > 0 &&
        range.fromOrderAmount !== null &&
        range.fromOrderAmount !== undefined &&
        data.ranges[i - 1].toOrderAmount !== null &&
        data.ranges[i - 1].toOrderAmount !== undefined &&
        range.fromOrderAmount !== data.ranges[i - 1].toOrderAmount
      ) {
        ctx.addIssue({
          code: "custom",
          path: ["ranges", i, "fromOrderAmount"],
          message: `Must equal previous range end (${data.ranges[i - 1].toOrderAmount!.toLocaleString("vi-VN")} VND)`,
        });
      }
    }

    const lastIdx = data.ranges.length - 1;
    if (
      typeof maxAmount === "number" &&
      !Number.isNaN(maxAmount) &&
      data.ranges[lastIdx].toOrderAmount !== null &&
      data.ranges[lastIdx].toOrderAmount !== undefined &&
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

export interface LevelPolicyReference {
  name: string;
  rank: number;
  requirement?: { minScore: number; minCompletedOrders: number; minTotalOrderValue: number } | null;
  depositPolicy?: {
    requireFullOnlinePayment: boolean;
    depositFreeOrderAmount?: number | null;
    maxDepositOrderAmount?: number | null;
    ranges?: Array<{
      fromOrderAmount: number;
      toOrderAmount: number;
      depositAmount: number;
    }> | null;
  } | null;
}

export type RangeErrorItem = {
  fromOrderAmount?: { type: string; message: string };
  toOrderAmount?: { type: string; message: string };
  depositAmount?: { type: string; message: string };
};

export interface CrossLevelValidationErrors {
  requireFullOnlinePayment?: { type: string; message: string };
  depositFreeOrderAmount?: { type: string; message: string };
  maxDepositOrderAmount?: { type: string; message: string };
  ranges?: RangeErrorItem[];
}

export function validateCrossLevelBenefits(
  values: {
    requireFullOnlinePayment: boolean;
    depositFreeOrderAmount?: number | null;
    maxDepositOrderAmount?: number | null;
    ranges?: Array<{
      fromOrderAmount?: number | null;
      toOrderAmount?: number | null;
      depositAmount?: number | null;
    }>;
  },
  errors: CrossLevelValidationErrors,
  precedingLevel?: LevelPolicyReference | null,
  succeedingLevel?: LevelPolicyReference | null,
) {
  const prevPolicy = precedingLevel?.depositPolicy;
  const nextPolicy = succeedingLevel?.depositPolicy;

  // 1. Cannot disable Pay at shop if lower rank allows it
  if (values.requireFullOnlinePayment) {
    if (prevPolicy && !prevPolicy.requireFullOnlinePayment) {
      errors.requireFullOnlinePayment = {
        type: "custom",
        message: `Cannot require 100% online payment because lower level (${precedingLevel!.name}) allows Pay at shop`,
      };
    }
    return;
  }

  const prevFree =
    prevPolicy && !prevPolicy.requireFullOnlinePayment
      ? (prevPolicy.depositFreeOrderAmount ?? 0)
      : undefined;
  const prevMax =
    prevPolicy && !prevPolicy.requireFullOnlinePayment
      ? (prevPolicy.maxDepositOrderAmount ?? 0)
      : undefined;

  const nextFree =
    nextPolicy && !nextPolicy.requireFullOnlinePayment
      ? (nextPolicy.depositFreeOrderAmount ?? 0)
      : undefined;
  const nextMax =
    nextPolicy && !nextPolicy.requireFullOnlinePayment
      ? (nextPolicy.maxDepositOrderAmount ?? 0)
      : undefined;

  const free = parseNumeric(values.depositFreeOrderAmount);
  const max = parseNumeric(values.maxDepositOrderAmount);

  // 2. Free threshold comparison (higher level >= lower level, higher level <= upper level)
  if (free !== undefined) {
    if (prevFree !== undefined && prevFree > 0 && free < prevFree) {
      errors.depositFreeOrderAmount = {
        type: "custom",
        message: `Must be at least ${prevFree.toLocaleString("vi-VN")} VND (greater than or equal to ${precedingLevel!.name})`,
      };
    } else if (nextFree !== undefined && nextFree > 0 && free > nextFree) {
      errors.depositFreeOrderAmount = {
        type: "custom",
        message: `Must not exceed ${nextFree.toLocaleString("vi-VN")} VND (less than or equal to ${succeedingLevel!.name})`,
      };
    }
  }

  // 3. Max Pay at shop comparison (higher level >= lower level, higher level <= upper level)
  if (max !== undefined) {
    if (prevMax !== undefined && prevMax > 0 && max < prevMax) {
      errors.maxDepositOrderAmount = {
        type: "custom",
        message: `Must be at least ${prevMax.toLocaleString("vi-VN")} VND (greater than or equal to ${precedingLevel!.name})`,
      };
    } else if (nextMax !== undefined && nextMax > 0 && max > nextMax) {
      errors.maxDepositOrderAmount = {
        type: "custom",
        message: `Must not exceed ${nextMax.toLocaleString("vi-VN")} VND (less than or equal to ${succeedingLevel!.name})`,
      };
    }
  }
}

export function buildLevelBenefitsResolver(
  precedingLevel?: LevelPolicyReference | null,
  succeedingLevel?: LevelPolicyReference | null,
): Resolver<LevelBenefitsFormData> {
  const baseResolver = zodResolver(levelBenefitsSchema);

  return async (values, context, options) => {
    const isSubmitting = !options || !options.names;
    const result = await baseResolver(values, context, options);
    const errors: FieldErrors<LevelBenefitsFormData> = result.errors ? { ...result.errors } : {};

    if (values.requireFullOnlinePayment) {
      delete errors.depositFreeOrderAmount;
      delete errors.maxDepositOrderAmount;
      delete errors.ranges;
    } else {
      const free = parseNumeric(values.depositFreeOrderAmount);
      const max = parseNumeric(values.maxDepositOrderAmount);

      if (isSubmitting && free === undefined) {
        errors.depositFreeOrderAmount = {
          type: "custom",
          message: "Deposit-free order amount is required",
        };
      } else if (free !== undefined) {
        if (free <= 0) {
          errors.depositFreeOrderAmount = {
            type: "custom",
            message: "Must be a positive whole VND amount",
          };
        } else if (free % 1000 !== 0) {
          errors.depositFreeOrderAmount = {
            type: "custom",
            message: "Must be a multiple of 1,000 VND",
          };
        }
      }

      if (isSubmitting && max === undefined) {
        errors.maxDepositOrderAmount = {
          type: "custom",
          message: "Maximum deposit order amount is required",
        };
      } else if (max !== undefined) {
        if (max <= 0) {
          errors.maxDepositOrderAmount = {
            type: "custom",
            message: "Must be a positive whole VND amount",
          };
        } else if (max % 1000 !== 0) {
          errors.maxDepositOrderAmount = {
            type: "custom",
            message: "Must be a multiple of 1,000 VND",
          };
        } else if (free !== undefined && max <= free) {
          errors.maxDepositOrderAmount = {
            type: "custom",
            message: "Must be greater than the deposit-free order amount",
          };
        }
      }

      if (isSubmitting && (!values.ranges || values.ranges.length === 0)) {
        errors.ranges = {
          root: {
            type: "custom",
            message: "Add at least one deposit range when Pay at shop is enabled",
          },
        } as unknown as NonNullable<FieldErrors<LevelBenefitsFormData>["ranges"]>;
      } else if (Array.isArray(values.ranges)) {
        const rangesErrors = (
          Array.isArray(errors.ranges) ? [...errors.ranges] : []
        ) as NonNullable<FieldErrors<LevelBenefitsFormData>["ranges"]>;
        let hasRangeError = false;

        values.ranges.forEach((r, idx) => {
          if (!rangesErrors[idx]) rangesErrors[idx] = {};
          const tierErr = rangesErrors[idx];
          if (!tierErr) return;

          const from = parseNumeric(r.fromOrderAmount);
          const to = parseNumeric(r.toOrderAmount);
          const dep = parseNumeric(r.depositAmount);

          if (from !== undefined && from % 1000 !== 0) {
            tierErr.fromOrderAmount = {
              type: "custom",
              message: "Must be a multiple of 1,000 VND",
            };
            hasRangeError = true;
          }

          if (to !== undefined) {
            if (to % 1000 !== 0) {
              tierErr.toOrderAmount = {
                type: "custom",
                message: "Must be a multiple of 1,000 VND",
              };
              hasRangeError = true;
            } else if (from !== undefined && to <= from) {
              tierErr.toOrderAmount = {
                type: "custom",
                message: "Must be greater than the range start amount",
              };
              hasRangeError = true;
            }
          }

          if (dep !== undefined) {
            if (dep % 1000 !== 0) {
              tierErr.depositAmount = {
                type: "custom",
                message: "Must be a multiple of 1,000 VND",
              };
              hasRangeError = true;
            } else if (dep < 2000) {
              tierErr.depositAmount = {
                type: "custom",
                message: "Deposit amount must be at least 2,000 VND",
              };
              hasRangeError = true;
            } else if (from !== undefined && dep > from) {
              tierErr.depositAmount = {
                type: "custom",
                message: "Cannot exceed the range start amount",
              };
              hasRangeError = true;
            }
          }

          if (isSubmitting) {
            if (from === undefined) {
              tierErr.fromOrderAmount = {
                type: "custom",
                message: "Starting amount is required",
              };
              hasRangeError = true;
            }
            if (to === undefined) {
              tierErr.toOrderAmount = {
                type: "custom",
                message: "Ending amount is required",
              };
              hasRangeError = true;
            }
            if (dep === undefined) {
              tierErr.depositAmount = {
                type: "custom",
                message: "Deposit amount is required",
              };
              hasRangeError = true;
            }
          }

          if (isSubmitting && idx === 0 && free !== undefined && from !== free) {
            tierErr.fromOrderAmount = {
              type: "custom",
              message: `First range must start at ${free.toLocaleString("vi-VN")} VND`,
            };
            hasRangeError = true;
          }

          if (isSubmitting && idx > 0) {
            const prevTo = parseNumeric(values.ranges[idx - 1]?.toOrderAmount);
            if (prevTo !== undefined && from !== prevTo) {
              tierErr.fromOrderAmount = {
                type: "custom",
                message: `Must equal previous range end (${prevTo.toLocaleString("vi-VN")} VND)`,
              };
              hasRangeError = true;
            }
          }

          if (
            isSubmitting &&
            idx === values.ranges.length - 1 &&
            max !== undefined &&
            to !== max
          ) {
            tierErr.toOrderAmount = {
              type: "custom",
              message: `Last range must end at ${max.toLocaleString("vi-VN")} VND`,
            };
            hasRangeError = true;
          }
        });

        if (hasRangeError) {
          errors.ranges = rangesErrors;
        }
      }
    }

    validateCrossLevelBenefits(
      values,
      errors as unknown as CrossLevelValidationErrors,
      precedingLevel,
      succeedingLevel,
    );

    const hasErrors = Object.keys(errors).length > 0;
    if (hasErrors) {
      return {
        values: {},
        errors,
      };
    }

    return {
      values,
      errors: {},
    };
  };
}

export const levelRequirementSchema = z.object({
  minScore: wholeNumber("Minimum score").min(0, "Cannot be negative").max(MAX_COUNT, "Too large"),
  minCompletedOrders: wholeNumber("Minimum completed orders")
    .min(0, "Cannot be negative")
    .max(MAX_COUNT, "Too large"),
  minTotalOrderValue: z
    .number({ error: "Minimum order value is required" })
    .min(0, "Cannot be negative")
    .max(MAX_VALUE, "Too large")
    .refine((v) => v % 1000 === 0, { message: "Must be a multiple of 1,000 VND" }),
});

export type LevelRequirementFormData = z.infer<typeof levelRequirementSchema>;

export const levelInfoSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Level name is required")
    .max(100, "Level name must not exceed 100 characters"),
});

export type LevelInfoFormData = z.infer<typeof levelInfoSchema>;

export const createLevelSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Level name is required")
    .max(100, "Level name must not exceed 100 characters"),
  minScore: wholeNumber("Minimum score").min(0, "Cannot be negative").max(MAX_COUNT, "Too large"),
  minCompletedOrders: wholeNumber("Minimum completed orders")
    .min(0, "Cannot be negative")
    .max(MAX_COUNT, "Too large"),
  minTotalOrderValue: z
    .number({ error: "Minimum order value is required" })
    .min(0, "Cannot be negative")
    .max(MAX_VALUE, "Too large")
    .refine((v) => v % 1000 === 0, { message: "Must be a multiple of 1,000 VND" }),
  description: z.string().max(2000, "Keep the benefits under 2000 characters"),
  requireFullOnlinePayment: z.boolean(),
  depositFreeOrderAmount: z.number().nullable().optional(),
  maxDepositOrderAmount: z.number().nullable().optional(),
  ranges: z.array(depositRangeItemSchema),
});

export type CreateLevelFormData = z.infer<typeof createLevelSchema>;

export function buildCreateLevelResolver(
  highestLevel?: LevelPolicyReference | null,
  existingNames?: string[],
): Resolver<CreateLevelFormData> {
  const baseResolver = zodResolver(createLevelSchema);

  return async (values, context, options) => {
    const isSubmitting = !options || !options.names;
    const result = await baseResolver(values, context, options);
    const errors: FieldErrors<CreateLevelFormData> = result.errors ? { ...result.errors } : {};

    if (values.requireFullOnlinePayment) {
      delete errors.depositFreeOrderAmount;
      delete errors.maxDepositOrderAmount;
      delete errors.ranges;
    }

    // 1. Duplicate name
    if (values.name && existingNames && existingNames.length > 0) {
      const trimmed = values.name.trim().toLowerCase();
      if (existingNames.some((n) => n.trim().toLowerCase() === trimmed)) {
        errors.name = {
          type: "custom",
          message: "A trust level with this name already exists",
        };
      }
    }

    // 2. Requirement thresholds against preceding highest level (BR-157, BR-158)
    if (highestLevel?.requirement) {
      const prevScore = highestLevel.requirement.minScore ?? 0;
      const prevOrders = highestLevel.requirement.minCompletedOrders ?? 0;
      const prevValue = highestLevel.requirement.minTotalOrderValue ?? 0;

      const score = parseNumeric(values.minScore);
      const orders = parseNumeric(values.minCompletedOrders);
      const val = parseNumeric(values.minTotalOrderValue);

      if (score !== undefined && score < prevScore) {
        errors.minScore = {
          type: "custom",
          message: `Must be at least ${prevScore} (greater than or equal to ${highestLevel.name})`,
        };
      }

      if (orders !== undefined && orders < prevOrders) {
        errors.minCompletedOrders = {
          type: "custom",
          message: `Must be at least ${prevOrders} (greater than or equal to ${highestLevel.name})`,
        };
      }

      if (val !== undefined) {
        if (val < 0) {
          errors.minTotalOrderValue = {
            type: "custom",
            message: "Cannot be negative",
          };
        } else if (val % 1000 !== 0) {
          errors.minTotalOrderValue = {
            type: "custom",
            message: "Must be a multiple of 1,000 VND",
          };
        } else if (val < prevValue) {
          errors.minTotalOrderValue = {
            type: "custom",
            message: `Must be at least ${prevValue.toLocaleString("vi-VN")} VND (greater than or equal to ${highestLevel.name})`,
          };
        }
      }

      // If all three fields are provided and none strictly exceed the previous level
      if (
        score !== undefined &&
        orders !== undefined &&
        val !== undefined &&
        score >= prevScore &&
        orders >= prevOrders &&
        val >= prevValue &&
        score === prevScore &&
        orders === prevOrders &&
        val === prevValue
      ) {
        errors.minScore = {
          type: "custom",
          message: `Requirements must exceed ${highestLevel.name}: at least one criterion must be strictly greater`,
        };
      }
    }

    // 3. Pay at shop & deposit policy (checked in real-time)
    if (!values.requireFullOnlinePayment) {
      const free = parseNumeric(values.depositFreeOrderAmount);
      const max = parseNumeric(values.maxDepositOrderAmount);

      if (isSubmitting && free === undefined) {
        errors.depositFreeOrderAmount = {
          type: "custom",
          message: "Deposit-free order amount is required",
        };
      } else if (free !== undefined) {
        if (free <= 0) {
          errors.depositFreeOrderAmount = {
            type: "custom",
            message: "Must be a positive whole VND amount",
          };
        } else if (free % 1000 !== 0) {
          errors.depositFreeOrderAmount = {
            type: "custom",
            message: "Must be a multiple of 1,000 VND",
          };
        }
      }

      if (isSubmitting && max === undefined) {
        errors.maxDepositOrderAmount = {
          type: "custom",
          message: "Maximum deposit order amount is required",
        };
      } else if (max !== undefined) {
        if (max <= 0) {
          errors.maxDepositOrderAmount = {
            type: "custom",
            message: "Must be a positive whole VND amount",
          };
        } else if (max % 1000 !== 0) {
          errors.maxDepositOrderAmount = {
            type: "custom",
            message: "Must be a multiple of 1,000 VND",
          };
        } else if (free !== undefined && max <= free) {
          errors.maxDepositOrderAmount = {
            type: "custom",
            message: "Must be greater than the deposit-free order amount",
          };
        }
      }

      if (isSubmitting && (!values.ranges || values.ranges.length === 0)) {
        errors.ranges = {
          root: {
            type: "custom",
            message: "Add at least one deposit range when Pay at shop is enabled",
          },
        };
      }

      if (Array.isArray(values.ranges)) {
        const rangesErrors = (
          Array.isArray(errors.ranges) ? [...errors.ranges] : []
        ) as NonNullable<FieldErrors<CreateLevelFormData>["ranges"]>;
        let hasRangeError = false;

        values.ranges.forEach((r, idx) => {
          if (!rangesErrors[idx]) rangesErrors[idx] = {};
          const tierErr = rangesErrors[idx];
          if (!tierErr) return;

          const from = parseNumeric(r.fromOrderAmount);
          const to = parseNumeric(r.toOrderAmount);
          const dep = parseNumeric(r.depositAmount);

          if (from !== undefined && from % 1000 !== 0) {
            tierErr.fromOrderAmount = {
              type: "custom",
              message: "Must be a multiple of 1,000 VND",
            };
            hasRangeError = true;
          }

          if (to !== undefined) {
            if (to % 1000 !== 0) {
              tierErr.toOrderAmount = {
                type: "custom",
                message: "Must be a multiple of 1,000 VND",
              };
              hasRangeError = true;
            } else if (from !== undefined && to <= from) {
              tierErr.toOrderAmount = {
                type: "custom",
                message: "Must be greater than the range start amount",
              };
              hasRangeError = true;
            }
          }

          if (dep !== undefined) {
            if (dep % 1000 !== 0) {
              tierErr.depositAmount = {
                type: "custom",
                message: "Must be a multiple of 1,000 VND",
              };
              hasRangeError = true;
            } else if (dep < 2000) {
              tierErr.depositAmount = {
                type: "custom",
                message: "Deposit amount must be at least 2,000 VND",
              };
              hasRangeError = true;
            } else if (from !== undefined && dep > from) {
              tierErr.depositAmount = {
                type: "custom",
                message: "Cannot exceed the range start amount",
              };
              hasRangeError = true;
            }
          }

          if (isSubmitting) {
            if (from === undefined) {
              tierErr.fromOrderAmount = {
                type: "custom",
                message: "Starting amount is required",
              };
              hasRangeError = true;
            }
            if (to === undefined) {
              tierErr.toOrderAmount = {
                type: "custom",
                message: "Ending amount is required",
              };
              hasRangeError = true;
            }
            if (dep === undefined) {
              tierErr.depositAmount = {
                type: "custom",
                message: "Deposit amount is required",
              };
              hasRangeError = true;
            }
          }

          if (isSubmitting && idx === 0 && free !== undefined && from !== free) {
            tierErr.fromOrderAmount = {
              type: "custom",
              message: `First range must start at ${free.toLocaleString("vi-VN")} VND`,
            };
            hasRangeError = true;
          }

          if (isSubmitting && idx > 0) {
            const prevTo = parseNumeric(values.ranges[idx - 1]?.toOrderAmount);
            if (prevTo !== undefined && from !== prevTo) {
              tierErr.fromOrderAmount = {
                type: "custom",
                message: `Must equal previous range end (${prevTo.toLocaleString("vi-VN")} VND)`,
              };
              hasRangeError = true;
            }
          }

          if (
            isSubmitting &&
            idx === values.ranges.length - 1 &&
            max !== undefined &&
            to !== max
          ) {
            tierErr.toOrderAmount = {
              type: "custom",
              message: `Last range must end at ${max.toLocaleString("vi-VN")} VND`,
            };
            hasRangeError = true;
          }
        });

        if (hasRangeError) {
          errors.ranges = rangesErrors;
        }
      }
    }

    validateCrossLevelBenefits(
      values,
      errors as unknown as CrossLevelValidationErrors,
      highestLevel,
      null,
    );

    const hasAnyError = Object.keys(errors).length > 0;
    if (hasAnyError) {
      return {
        values: {},
        errors,
      };
    }

    return {
      values,
      errors: {},
    };
  };
}

export const penaltyRuleSchema = z
  .object({
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
      .max(MAX_VALUE, "Too large")
      .refine((v) => v % 1000 === 0, { message: "Must be a multiple of 1,000 VND" }),
    permanentOfflineLock: z.boolean(),
    isActive: z.boolean(),
    /** datetime-local value; empty in Create mode means "start now". */
    effectiveFrom: z.string(),
    /** datetime-local value; empty means "no end date (indefinite)". */
    effectiveTo: z.string(),
  })
  .superRefine((data, ctx) => {
    const fromTime = data.effectiveFrom ? new Date(data.effectiveFrom).getTime() : Date.now();
    if (data.effectiveFrom && Number.isNaN(fromTime)) {
      ctx.addIssue({
        code: "custom",
        path: ["effectiveFrom"],
        message: "Invalid start date/time",
      });
      return;
    }

    if (data.effectiveTo) {
      const toTime = new Date(data.effectiveTo).getTime();
      if (Number.isNaN(toTime)) {
        ctx.addIssue({
          code: "custom",
          path: ["effectiveTo"],
          message: "Invalid end date/time",
        });
      } else if (toTime <= fromTime) {
        ctx.addIssue({
          code: "custom",
          path: ["effectiveTo"],
          message: "'Until' date/time must be strictly after 'From' date/time",
        });
      }
    }
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
    isActive: z.boolean(),
  });
}

export type ScoreRuleFormData = { scoreDelta: number; isActive: boolean };

/** Converts a datetime-local value into an ISO string, or undefined when empty. */
export function toIsoOrUndefined(value: string): string | undefined {
  if (!value) return undefined;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
}

/** Converts an ISO date string into `YYYY-MM-DDTHH:mm` in local time for `<input type="datetime-local">`. */
export function toDateTimeLocalValue(iso?: string | null): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
