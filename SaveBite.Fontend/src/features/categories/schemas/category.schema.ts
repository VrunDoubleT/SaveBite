import { z } from "zod";

export const categorySchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "The category name is required.")
    .max(150, "The category name must not exceed 150 characters."),
  description: z
    .string()
    .max(500, "The category description must not exceed 500 characters.")
    .optional()
    .or(z.literal("")),
  imageUrl: z.string().url("Must be a valid URL").optional().or(z.literal("")),
  isActive: z.boolean(),
});

export type CategoryFormData = z.infer<typeof categorySchema>;
