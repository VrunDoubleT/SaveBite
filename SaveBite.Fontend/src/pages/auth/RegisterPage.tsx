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
import { registerSchema, type RegisterFormValues } from "@/features/auth/schemas/auth.schema";
import { PasswordField } from "@/features/auth/components/PasswordField";
import { getApiErrorMessage, getApiValidationErrors } from "@/shared/api";

export function RegisterPage() {
  const navigate = useNavigate();
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    mode: "onTouched",
    defaultValues: { fullName: "", email: "", password: "", confirmPassword: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    const input = {
      fullName: values.fullName,
      email: values.email,
      password: values.password,
    };
    try {
      const message = await authApi.register(input);
      navigate(`${APP_PATHS.REGISTER_VERIFY}?email=${encodeURIComponent(input.email)}`, {
        state: { message },
      });
    } catch (error) {
      const apiErrors = getApiValidationErrors(error);
      if (apiErrors.fullName) setError("fullName", { message: apiErrors.fullName });
      if (apiErrors.email) setError("email", { message: apiErrors.email });
      if (apiErrors.password) setError("password", { message: apiErrors.password });
      setError("root.server", { message: getApiErrorMessage(error) });
    }
  });

  return (
    <AuthFormCard
      eyebrow="Join SaveBite"
      title="Create your account"
      description="Create an account, then enter the verification code sent to your email."
      footer={
        <>
          Already have an account?{" "}
          <Link
            className="font-semibold text-primary-600 hover:text-primary-700"
            to={APP_PATHS.LOGIN}
          >
            Sign in
          </Link>
        </>
      }
    >
      <form className="mt-8 space-y-5" onSubmit={onSubmit} noValidate>
        <label className="block text-sm font-semibold text-text-primary">
          Full name
          <input
            {...register("fullName")}
            autoComplete="name"
            aria-invalid={Boolean(errors.fullName)}
            className={authInputClass}
            placeholder="Your full name"
          />
          <FieldError message={errors.fullName?.message} />
        </label>

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
          {...register("password")}
          label="Password"
          autoComplete="new-password"
          aria-invalid={Boolean(errors.password)}
          placeholder="At least 8 characters"
          error={errors.password?.message}
        />

        <PasswordField
          {...register("confirmPassword")}
          label="Confirm password"
          autoComplete="new-password"
          aria-invalid={Boolean(errors.confirmPassword)}
          placeholder="Enter the password again"
          error={errors.confirmPassword?.message}
        />

        <p className="text-xs leading-relaxed text-text-muted">
          Use 8–72 characters with uppercase, lowercase, a number and a special character.
        </p>

        {errors.root?.server?.message && <FormAlert>{errors.root.server.message}</FormAlert>}

        <button className={authSubmitClass} type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Sending verification code..." : "Create account"}
        </button>
      </form>
    </AuthFormCard>
  );
}
