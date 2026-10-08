import { z } from "zod";

const addressFieldsSchema = {
  label: z.string().trim().max(100, "Label must not exceed 100 characters."),

  addressLine: z
    .string()
    .trim()
    .min(1, "Address line is required.")
    .max(500, "Address line must not exceed 500 characters."),

  ward: z.string().trim().max(100, "Ward must not exceed 100 characters."),

  district: z.string().trim().max(100, "District must not exceed 100 characters."),

  city: z.string().trim().max(100, "City / Province must not exceed 100 characters."),

  latitude: z
    .string()
    .trim()
    .refine(
      (value) => {
        const parsed = Number(value);
        return value !== "" && Number.isFinite(parsed) && parsed >= -90 && parsed <= 90;
      },
      {
        message: "Latitude must be a number between -90 and 90.",
      },
    ),

  longitude: z
    .string()
    .trim()
    .refine(
      (value) => {
        const parsed = Number(value);
        return value !== "" && Number.isFinite(parsed) && parsed >= -180 && parsed <= 180;
      },
      {
        message: "Longitude must be a number between -180 and 180.",
      },
    ),

  isDefault: z.boolean(),
};

export const createAddressSchema = z.object(addressFieldsSchema);

export const updateAddressSchema = z.object(addressFieldsSchema);

export type CreateAddressFormValues = z.infer<typeof createAddressSchema>;

export type UpdateAddressFormValues = z.infer<typeof updateAddressSchema>;
