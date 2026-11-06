import { z } from "zod";

// PayOS configuration schema.
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

export type PayOSValues = z.infer<z.ZodObject<typeof payOSShape>>;

export type UpdateShopPaymentConfigValues = PayOSValues;

export type ShopPaymentConfigView = {
  payosClientId: string;
  hasPayosApiKey: boolean;
  hasPayosChecksumKey: boolean;
  updatedAt: string | null;
};

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
export const updateShopPaymentConfigSchema = z.object(payOSShape);

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

// Business info & shop images
export const updateBusinessInfoSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "Shop name must contain at least 2 characters.")
    .max(200, "Shop name must not exceed 200 characters."),
  description: z.string().max(2000, "Description must not exceed 2000 characters."),
  addressLine: z
    .string()
    .trim()
    .min(1, "Address is required.")
    .max(500, "Address must not exceed 500 characters."),
  ward: z.string().max(100, "Ward must not exceed 100 characters."),
  district: z.string().max(100, "District must not exceed 100 characters."),
  city: z.string().max(100, "City must not exceed 100 characters."),
  latitude: z
    .number()
    .finite()
    .min(-90, "Latitude must be between -90 and 90.")
    .max(90, "Latitude must be between -90 and 90."),
  longitude: z
    .number()
    .finite()
    .min(-180, "Longitude must be between -180 and 180.")
    .max(180, "Longitude must be between -180 and 180."),
  openingTime: z.string().nullable(),
  closingTime: z.string().nullable(),
});

export const shopImageFileSchema = z
  .custom<File | null>((file) => file === null || file instanceof File)
  .refine((file) => !file || file.size > 0, "Image file must not be empty.")
  .refine((file) => !file || file.size <= 5 * 1024 * 1024, "Image must not exceed 5 MB.")
  .refine(
    (file) => !file || ["image/jpeg", "image/png", "image/webp"].includes(file.type),
    "Image must be JPG, PNG or WEBP.",
  );

export function validateShopImage(file: File | null): string | undefined {
  const result = shopImageFileSchema.safeParse(file);
  return result.success ? undefined : result.error.issues[0]?.message;
}

export type UpdateBusinessInfoValues = z.infer<typeof updateBusinessInfoSchema>;
