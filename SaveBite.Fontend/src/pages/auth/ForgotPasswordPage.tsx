import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { authApi } from "@/features/auth/api/authApi";
import {
  AuthFormCard,
  FieldError,
  FormAlert,
  authInputClass,
  authSubmitClass,
} from "@/features/auth/components/AuthFormCard";
import {
  forgotPasswordSchema,
  type ForgotPasswordFormValues,
} from "@/features/auth/schemas/auth.schema";
import { PasswordField } from "@/features/auth/components/PasswordField";
import { getApiErrorMessage, getApiValidationErrors } from "@/shared/api";

export function ForgotPasswordPage() {
  const navigate = useNavigate();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    mode: "onTouched",
    defaultValues: { email: "", newPassword: "", confirmPassword: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    const input = { email: values.email, newPassword: values.newPassword };
    try {
      const message = await authApi.forgotPassword(input);
      navigate(`${APP_PATHS.RESET_PASSWORD_VERIFY}?email=${encodeURIComponent(input.email)}`, {
        state: { message },
      });
    } catch (error) {
      const apiErrors = getApiValidationErrors(error);
      if (apiErrors.email) setError("email", { message: apiErrors.email });
      if (apiErrors.newPassword) setError("newPassword", { message: apiErrors.newPassword });
      setError("root.server", { message: getApiErrorMessage(error) });
    }
  });

  return (
    <AuthFormCard
      eyebrow="Account recovery"
      title="Reset your password"
      description="Choose a new password. We will email you a code before applying the change."
      footer={
        <Link
          className="font-semibold text-primary-600 hover:text-primary-700"
          to={APP_PATHS.LOGIN}
        >
          Back to sign in
        </Link>
      }
    >
      <form className="mt-8 space-y-5" onSubmit={onSubmit} noValidate>
        <label className="block text-sm font-semibold text-text-primary">
          Email
          <input
            {...register("email")}
            type="email"
            autoComplete="email"
            aria-invalid={Boolean(errors.email)}
            className={authInputClass}
            placeholder="you@example.com"
          />
          <FieldError message={errors.email?.message} />
        </label>

        <PasswordField
          {...register("newPassword")}
          label="New password"
          autoComplete="new-password"
          aria-invalid={Boolean(errors.newPassword)}
          placeholder="At least 8 characters"
          error={errors.newPassword?.message}
        />

        <PasswordField
          {...register("confirmPassword")}
          label="Confirm new password"
          autoComplete="new-password"
          aria-invalid={Boolean(errors.confirmPassword)}
          placeholder="Enter the new password again"
          error={errors.confirmPassword?.message}
        />

        <p className="text-xs leading-relaxed text-text-muted">
          Use 8–72 characters with uppercase, lowercase, a number and a special character.
        </p>

        {errors.root?.server?.message && <FormAlert>{errors.root.server.message}</FormAlert>}

        <button className={authSubmitClass} type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Sending verification code..." : "Continue"}
        </button>
      </form>
    </AuthFormCard>
  );
}
