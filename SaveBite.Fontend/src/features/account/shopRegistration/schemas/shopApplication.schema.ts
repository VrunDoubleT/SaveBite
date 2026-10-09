import { z } from "zod";

/* -------------------------------------------------------------------------- */
/*  Validation helpers                                                         */
/* -------------------------------------------------------------------------- */

const requiredText = (label: string, maxLength?: number) => {
  let schema = z.string().trim().min(1, `${label} is required.`);

  if (maxLength) {
    schema = schema.max(maxLength, `${label} must not exceed ${maxLength} characters.`);
  }

  return schema;
};

const coordinate = (label: string, min: number, max: number) =>
  z
    .string()
    .trim()
    .min(1, `${label} is required.`)
    .refine((value) => {
      const number = Number(value);

      return Number.isFinite(number) && number >= min && number <= max;
    }, `${label} must be between ${min} and ${max}.`);

const time = (label: string) =>
  z
    .string()
    .min(1, `${label} is required.`)
    .regex(/^([01]\d|2[0-3]):[0-5]\d$/, `${label} must be a valid time.`);

/* -------------------------------------------------------------------------- */
/*  Shop application schema                                                    */
/* -------------------------------------------------------------------------- */

export const shopApplicationSchema = z
  .object({
    name: requiredText("Shop name", 200),

    description: z.string().max(2000, "Description must not exceed 2000 characters."),

    businessLicenseNo: requiredText("Business license number", 10),

    addressLine: requiredText("Address", 500),

    ward: z.string().max(100, "Ward must not exceed 100 characters."),

    district: z.string().max(100, "District must not exceed 100 characters."),

    city: z.string().max(100, "City must not exceed 100 characters."),

    latitude: coordinate("Latitude", -90, 90),

    longitude: coordinate("Longitude", -180, 180),

    openingTime: time("Opening time"),

    closingTime: time("Closing time"),

    bankName: requiredText("Bank", 200),

    bankAccountNumber: requiredText("Account number", 200).regex(
      /^\d+$/,
      "Account number must contain numbers only.",
    ),

    bankAccountHolder: requiredText("Account holder", 200),

    payosClientId: z.string(),

    payosApiKey: z.string(),

    payosChecksumKey: z.string(),
  })
  .superRefine((values, context) => {
    const validTimePattern = /^([01]\d|2[0-3]):[0-5]\d$/;

    if (
      validTimePattern.test(values.openingTime) &&
      validTimePattern.test(values.closingTime) &&
      values.closingTime <= values.openingTime
    ) {
      context.addIssue({
        code: "custom",
        path: ["closingTime"],
        message: "Closing time must be later than opening time.",
      });

      context.addIssue({
        code: "custom",
        path: ["openingTime"],
        message: "Opening time must be earlier than closing time.",
      });
    }
  });

export type ShopApplicationFormValues = z.infer<typeof shopApplicationSchema>;

/* -------------------------------------------------------------------------- */
/*  Shop application validation                                                */
/* -------------------------------------------------------------------------- */

export function validateShopApplication(
  values: ShopApplicationFormValues,
  hasExistingPayos: boolean,
  documents: { type: string; file: File }[],
) {
  const result = shopApplicationSchema.safeParse(values);

  const errors: Partial<Record<keyof ShopApplicationFormValues | "documents", string>> = {};

  if (!result.success) {
    for (const issue of result.error.issues) {
      const field = issue.path[0] as keyof ShopApplicationFormValues;

      errors[field] ??= issue.message;
    }
  }

  // Existing PayOS configuration can be kept by leaving the fields empty.
  if (!hasExistingPayos) {
    if (!values.payosClientId.trim()) {
      errors.payosClientId = "PayOS Client ID is required.";
    }

    if (!values.payosApiKey.trim()) {
      errors.payosApiKey = "PayOS API key is required.";
    }

    if (!values.payosChecksumKey.trim()) {
      errors.payosChecksumKey = "PayOS Checksum key is required.";
    }
  }

  // Supporting document validation.
  if (documents.length > 10) {
    errors.documents = "You can upload up to 10 documents.";
  } else if (!documents.some((document) => document.type === "BusinessLicense")) {
    errors.documents = "Business license document is required.";
  } else if (!documents.some((document) => document.type === "FoodSafetyCertificate")) {
    errors.documents = "Food safety certificate is required.";
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors,
  };
}
