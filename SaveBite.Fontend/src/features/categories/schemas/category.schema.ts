import { z } from "zod";

export const categorySchema = z.object({
  name: z.string().min(1, "Name is required").max(150),
  description: z.string().optional(),
  imageUrl: z.string().url("Must be a valid URL").optional().or(z.literal("")),
  isActive: z.boolean(),
});

export type CategoryFormData = z.infer<typeof categorySchema>;
