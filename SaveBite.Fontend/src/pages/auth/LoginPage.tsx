import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { HOME_PATH_BY_ROLE } from "@/app/router/rolePaths";
import {
  AuthFormCard,
  FieldError,
  FormAlert,
  authInputClass,
  authSubmitClass,
} from "@/features/auth/components/AuthFormCard";
import { loginSchema, type LoginFormValues } from "@/features/auth/schemas/auth.schema";
import { PasswordField } from "@/features/auth/components/PasswordField";
import { getApiErrorMessage, getApiValidationErrors } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";
import { Link } from "react-router-dom";

interface LoginLocationState {
  from?: { pathname?: string };
  message?: string;
}

export function LoginPage() {
  const login = useAuthStore((state) => state.login);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const isInitializing = useAuthStore((state) => state.isInitializing);
  const user = useAuthStore((state) => state.user);
  const location = useLocation();
  const navigate = useNavigate();
  const locationState = location.state as LoginLocationState | null;
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    mode: "onTouched",
    defaultValues: { email: "", password: "" },
  });

  if (isInitializing) {
    return (
      <div className="grid min-h-[65vh] place-items-center text-sm font-semibold text-neutral-500">
        Checking your session...
      </div>
    );
  }

  if (isAuthenticated && user) {
    return <Navigate to={HOME_PATH_BY_ROLE[user.role]} replace />;
  }

  const onSubmit = handleSubmit(async (credentials) => {
    try {
      const authenticatedUser = await login(credentials);
      const requestedPath = locationState?.from?.pathname;

      const defaultPath = HOME_PATH_BY_ROLE[authenticatedUser.role];
      navigate(requestedPath ?? defaultPath, { replace: true });
    } catch (loginError) {
      const apiErrors = getApiValidationErrors(loginError);
      if (apiErrors.email) setError("email", { message: apiErrors.email });
      if (apiErrors.password) setError("password", { message: apiErrors.password });
      setError("root.server", { message: getApiErrorMessage(loginError) });
    }
  });

  return (
    <AuthFormCard
      eyebrow="Welcome back"
      title="Sign in to SaveBite"
      description="Enter your account details to continue."
      footer={
        <>
          New to SaveBite?{" "}
          <Link
            className="font-semibold text-primary-600 hover:text-primary-700"
            to={APP_PATHS.REGISTER}
          >
            Create an account
          </Link>
        </>
      }
    >
      <form className="mt-8 space-y-5" onSubmit={onSubmit} noValidate>
        {locationState?.message && <FormAlert tone="info">{locationState.message}</FormAlert>}

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
          autoComplete="current-password"
          aria-invalid={Boolean(errors.password)}
          placeholder="Enter your password"
          error={errors.password?.message}
        />

        <div className="text-right">
          <Link
            className="text-sm font-semibold text-primary-600 hover:text-primary-700"
            to={APP_PATHS.FORGOT_PASSWORD}
          >
            Forgot password?
          </Link>
        </div>

        {errors.root?.server?.message && <FormAlert>{errors.root.server.message}</FormAlert>}

        <button type="submit" disabled={isSubmitting} className={authSubmitClass}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </button>
      </form>
    </AuthFormCard>
  );
}
