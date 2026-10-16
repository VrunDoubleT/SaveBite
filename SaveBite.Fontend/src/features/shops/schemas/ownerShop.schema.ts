import { z } from "zod";

export const updateBusinessInfoSchema = z
  .object({
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
  })
  .superRefine((values, ctx) => {
    if (values.openingTime && values.closingTime && values.openingTime >= values.closingTime) {
      ctx.addIssue({
        code: "custom",
        path: ["closingTime"],
        message: "Closing time must be later than opening time.",
      });
    }
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
