import { z } from "zod";

/*  Validation helpers */
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

/*  Shop application schema */
export const shopApplicationSchema = z.object({
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
  payosClientId: z.string(),
  payosApiKey: z.string(),
  payosChecksumKey: z.string(),
});

export type ShopApplicationFormValues = z.infer<typeof shopApplicationSchema>;

/*  Logo validation */
const LOGO_ALLOWED_TYPES = ["image/jpeg", "image/png", "image/webp"];
const LOGO_ALLOWED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".webp"];
const LOGO_MAX_BYTES = 5 * 1024 * 1024;

export function validateLogoFile(file: File): string | undefined {
  const name = file.name.toLowerCase();
  const validExtension = LOGO_ALLOWED_EXTENSIONS.some((ext) => name.endsWith(ext));

  if (!LOGO_ALLOWED_TYPES.includes(file.type) || !validExtension) {
    return "Shop logo must be an image file (JPG, PNG or WEBP).";
  }

  if (file.size === 0) return "Shop logo must not be empty.";

  if (file.size > LOGO_MAX_BYTES) return "Shop logo must not exceed 5 MB.";

  return undefined;
}

/*  Shop application validation */
export function validateShopApplication(
  values: ShopApplicationFormValues,
  hasExistingPayos: boolean,
  documents: { type: string; file: File }[],
  existingDocumentTypes: string[] = [],
  logoOptions: { file?: File; hasExisting?: boolean } = {},
) {
  const result = shopApplicationSchema.safeParse(values);

  const errors: Partial<Record<keyof ShopApplicationFormValues | "documents" | "logo", string>> =
    {};

  if (!result.success) {
    for (const issue of result.error.issues) {
      const field = issue.path[0] as keyof ShopApplicationFormValues;

      errors[field] ??= issue.message;
    }
  }

  // Existing PayOS configuration can be kept by leaving the fields empty.
  if (
    values.openingTime &&
    values.closingTime &&
    values.openingTime === values.closingTime &&
    values.openingTime !== "00:00"
  ) {
    errors.closingTime =
      "Opening time and closing time cannot be the same (except 00:00 - 00:00 for open all day / 24/7).";
  }

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

  // Existing documents remain valid unless a replacement file of the same type is selected.
  const availableDocumentTypes = new Set([
    ...existingDocumentTypes,
    ...documents.map((document) => document.type),
  ]);

  if (documents.length > 10) {
    errors.documents = "You can upload up to 10 documents.";
  } else if (!availableDocumentTypes.has("BusinessLicense")) {
    errors.documents = "Business license document is required.";
  } else if (!availableDocumentTypes.has("FoodSafetyCertificate")) {
    errors.documents = "Food safety certificate is required.";
  }

  if (logoOptions.file) {
    const logoError = validateLogoFile(logoOptions.file);
    if (logoError) errors.logo = logoError;
  } else if (!logoOptions.hasExisting) {
    errors.logo = "Shop logo is required.";
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors,
  };
}
