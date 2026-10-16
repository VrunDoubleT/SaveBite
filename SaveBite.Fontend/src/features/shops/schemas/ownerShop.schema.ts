import { z } from "zod";

const bankAccountShape = {
  bankName: z.string().trim().max(255, "Bank name must not exceed 255 characters."),

  bankAccountNumber: z
    .string()
    .trim()
    .max(50, "Bank account number must not exceed 50 characters.")
    .regex(/^\d*$/, "Bank account number must contain digits only."),

  bankAccountHolder: z
    .string()
    .trim()
    .max(255, "Account holder name must not exceed 255 characters."),
};

const payOSShape = {
  payosClientId: z
    .string()
    .trim()
    .min(1, "PayOS Client ID is required.")
    .max(255, "PayOS Client ID must not exceed 255 characters."),

  payosApiKey: z.string().trim().max(500, "PayOS API Key must not exceed 500 characters."),

  payosChecksumKey: z
    .string()
    .trim()
    .max(500, "PayOS Checksum Key must not exceed 500 characters."),
};

export type BankAccountValues = z.infer<z.ZodObject<typeof bankAccountShape>>;

export type PayOSValues = z.infer<z.ZodObject<typeof payOSShape>>;

export type UpdateShopPaymentConfigValues = BankAccountValues & PayOSValues;

export type ShopPaymentConfigView = {
  bankName: string | null;
  bankAccountNumber: string | null;
  bankAccountHolder: string | null;
  payosClientId: string;
  hasPayosApiKey: boolean;
  hasPayosChecksumKey: boolean;
  updatedAt: string | null;
};

/**
 * Bank account:
 * - All fields empty: allowed, meaning remove bank account.
 * - At least one field filled: all three fields are required.
 */
function refineBankAccount(values: BankAccountValues, ctx: z.RefinementCtx) {
  const hasBankName = values.bankName.length > 0;
  const hasAccountNumber = values.bankAccountNumber.length > 0;
  const hasAccountHolder = values.bankAccountHolder.length > 0;

  const hasAnyBankField = hasBankName || hasAccountNumber || hasAccountHolder;

  if (!hasAnyBankField) return;

  if (!hasBankName) {
    ctx.addIssue({
      code: "custom",
      path: ["bankName"],
      message: "Bank name is required when providing bank details.",
    });
  }

  if (!hasAccountNumber) {
    ctx.addIssue({
      code: "custom",
      path: ["bankAccountNumber"],
      message: "Bank account number is required when providing bank details.",
    });
  }

  if (!hasAccountHolder) {
    ctx.addIssue({
      code: "custom",
      path: ["bankAccountHolder"],
      message: "Account holder name is required when providing bank details.",
    });
  }
}

export const bankAccountSchema = z.object(bankAccountShape).superRefine(refineBankAccount);

/**
 * Secret fields are required only when they haven't been configured before.
 * An empty secret on an already configured PayOS account means "keep existing".
 */
export function createPayOSSchema(options: {
  hasPayosApiKey: boolean;
  hasPayosChecksumKey: boolean;
}) {
  return z.object(payOSShape).superRefine((values, ctx) => {
    if (!options.hasPayosApiKey && !values.payosApiKey.trim()) {
      ctx.addIssue({
        code: "custom",
        path: ["payosApiKey"],
        message: "PayOS API Key is required.",
      });
    }

    if (!options.hasPayosChecksumKey && !values.payosChecksumKey.trim()) {
      ctx.addIssue({
        code: "custom",
        path: ["payosChecksumKey"],
        message: "PayOS Checksum Key is required.",
      });
    }
  });
}

/**
 * Full update-request schema.
 * Secret fields may be empty because the backend can preserve existing secrets.
 */
export const updateShopPaymentConfigSchema = z
  .object({
    ...bankAccountShape,
    ...payOSShape,
  })
  .superRefine((values, ctx) => {
    refineBankAccount(values, ctx);
  });

export type FieldErrors = Record<string, string>;

export function getFieldErrors(schema: z.ZodType, values: unknown): FieldErrors {
  const result = schema.safeParse(values);

  if (result.success) return {};

  const errors: FieldErrors = {};

  for (const issue of result.error.issues) {
    const field = issue.path[0];

    if (typeof field !== "string") continue;

    // Keep the first message for each field.
    if (!errors[field]) {
      errors[field] = issue.message;
    }
  }

  return errors;
}
