import { z } from "zod";

const emailSchema = z
  .string()
  .trim()
  .min(1, "Email is required.")
  .max(255, "Email must not exceed 255 characters.")
  .email("Enter a valid email address.");

const passwordSchema = z
  .string()
  .min(1, "Password is required.")
  .min(8, "Password must contain at least 8 characters.")
  .max(72, "Password must not exceed 72 characters.")
  .refine((value) => new TextEncoder().encode(value).length <= 72, {
    message: "Password must not exceed 72 UTF-8 bytes.",
  })
  .regex(/[A-Z]/, "Password must contain an uppercase letter.")
  .regex(/[a-z]/, "Password must contain a lowercase letter.")
  .regex(/[0-9]/, "Password must contain a digit.")
  .regex(/[^a-zA-Z0-9]/, "Password must contain a special character.");

const otpSchema = z
  .string()
  .trim()
  .regex(/^\d{6}$/, "Verification code must contain exactly 6 digits.");

export const loginSchema = z.object({
  email: emailSchema,
  password: passwordSchema,
});

export const registerSchema = z
  .object({
    fullName: z
      .string()
      .trim()
      .min(2, "Full name must contain at least 2 characters.")
      .max(150, "Full name must not exceed 150 characters."),
    email: emailSchema,
    password: passwordSchema,
    confirmPassword: z.string().min(1, "Confirm your password."),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ["confirmPassword"],
    message: "Passwords do not match.",
  });

export const forgotPasswordSchema = z
  .object({
    email: emailSchema,
    newPassword: passwordSchema,
    confirmPassword: z.string().min(1, "Confirm your new password."),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ["confirmPassword"],
    message: "Passwords do not match.",
  });

export const verifyOtpSchema = z.object({
  email: emailSchema,
  otp: otpSchema,
});

export type LoginFormValues = z.infer<typeof loginSchema>;
export type RegisterFormValues = z.infer<typeof registerSchema>;
export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;
export type VerifyOtpFormValues = z.infer<typeof verifyOtpSchema>;
