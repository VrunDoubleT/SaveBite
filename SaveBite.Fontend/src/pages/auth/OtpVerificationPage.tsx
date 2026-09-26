import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { authApi } from "@/features/auth/api/authApi";
import {
  AuthFormCard,
  FieldError,
  FormAlert,
  authInputClass,
  authSubmitClass,
} from "@/features/auth/components/AuthFormCard";
import { verifyOtpSchema, type VerifyOtpFormValues } from "@/features/auth/schemas/auth.schema";
import { getApiErrorMessage, getApiValidationErrors } from "@/shared/api";

interface OtpVerificationPageProps {
  purpose: "registration" | "password-reset";
}

interface VerificationLocationState {
  message?: string;
}

export function OtpVerificationPage({ purpose }: OtpVerificationPageProps) {
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const navigate = useNavigate();
  const isRegistration = purpose === "registration";
  const requestPath = isRegistration ? APP_PATHS.REGISTER : APP_PATHS.FORGOT_PASSWORD;
  const requestMessage = (location.state as VerificationLocationState | null)?.message;
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<VerifyOtpFormValues>({
    resolver: zodResolver(verifyOtpSchema),
    mode: "onTouched",
    defaultValues: { email: searchParams.get("email") ?? "", otp: "" },
  });

  const onSubmit = handleSubmit(async (input) => {
    try {
      const message = isRegistration
        ? await authApi.verifyRegistration(input)
        : await authApi.resetPassword(input);

      navigate(APP_PATHS.LOGIN, {
        replace: true,
        state: { message: message ?? "Verification completed. You can now sign in." },
      });
    } catch (error) {
      const apiErrors = getApiValidationErrors(error);
      if (apiErrors.email) setError("email", { message: apiErrors.email });
      if (apiErrors.otp) setError("otp", { message: apiErrors.otp });
      setError("root.server", { message: getApiErrorMessage(error) });
    }
  });

  return (
    <AuthFormCard
      eyebrow="Email verification"
      title={isRegistration ? "Verify your account" : "Verify password reset"}
      description="Enter the six-digit code sent to your email. The code expires after a limited time."
      footer={
        <>
          Need a new code?{" "}
          <Link className="font-semibold text-primary-600 hover:text-primary-700" to={requestPath}>
            Start again
          </Link>
        </>
      }
    >
      <form className="mt-8 space-y-5" onSubmit={onSubmit} noValidate>
        {requestMessage && <FormAlert tone="info">{requestMessage}</FormAlert>}

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

        <label className="block text-sm font-semibold text-text-primary">
          Verification code
          <input
            {...register("otp")}
            inputMode="numeric"
            autoComplete="one-time-code"
            maxLength={6}
            aria-invalid={Boolean(errors.otp)}
            className={`${authInputClass} text-center text-xl font-bold tracking-[0.45em]`}
            placeholder="000000"
          />
          <FieldError message={errors.otp?.message} />
        </label>

        {errors.root?.server?.message && <FormAlert>{errors.root.server.message}</FormAlert>}

        <button className={authSubmitClass} type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Verifying..." : "Verify code"}
        </button>
      </form>
    </AuthFormCard>
  );
}
