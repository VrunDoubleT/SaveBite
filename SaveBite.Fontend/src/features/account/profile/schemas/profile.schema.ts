import { z } from "zod";

export const updateUserProfileSchema = z.object({
  fullName: z
    .string()
    .trim()
    .min(1, "Full name is required.")
    .max(150, "Full name must not exceed 150 characters."),

  phone: z
    .string()
    .trim()
    .refine(
      (value) => {
        if (!value) {
          return true;
        }

        return /^\d+$/.test(value);
      },
      {
        message: "Phone number is invalid.",
      },
    )
    .refine(
      (value) => {
        if (!value) {
          return true;
        }

        return value.length === 9 || value.length === 10;
      },
      {
        message: "Phone number must contain 9 or 10 digits.",
      },
    ),
});

export type UpdateUserProfileFormValues = z.infer<typeof updateUserProfileSchema>;
