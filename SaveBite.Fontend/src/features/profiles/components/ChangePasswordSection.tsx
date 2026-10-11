import { zodResolver } from "@hookform/resolvers/zod";
import { LoaderCircle, LockKeyhole } from "lucide-react";
import { useForm } from "react-hook-form";
import { authApi } from "@/features/auth/api/authApi";
import { PasswordField } from "@/features/auth/components/PasswordField";
import {
  changePasswordSchema,
  type ChangePasswordFormValues,
} from "@/features/auth/schemas/auth.schema";
import { getApiErrorMessage, getApiValidationErrors } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";
import { toast } from "@/shared/stores/toastStore";

export function ChangePasswordSection({ onClose }: { onClose: () => void }) {
  const isLoggingOut = useAuthStore((state) => state.isLoggingOut);
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    mode: "onTouched",
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  });

  const submit = handleSubmit(async ({ currentPassword, newPassword }) => {
    try {
      await authApi.changePassword({ currentPassword, newPassword });
      reset();
      onClose();
      toast.success("Password changed successfully.");
    } catch (error) {
      const apiErrors = getApiValidationErrors(error);
      if (apiErrors.currentPassword)
        setError("currentPassword", { message: apiErrors.currentPassword });
      if (apiErrors.newPassword) setError("newPassword", { message: apiErrors.newPassword });
      const message = getApiErrorMessage(error);
      if (message === "Current password is incorrect.") {
        setError("currentPassword", { message }, { shouldFocus: true });
      } else {
        setError("root.server", { message });
      }
    }
  });

  return (
    <section
      id="change-password-form"
      className="mt-6 border-t border-border-default pt-6"
      aria-labelledby="change-password-title"
    >
      <h2
        id="change-password-title"
        className="flex items-center gap-2 text-lg font-bold text-text-primary"
      >
        <LockKeyhole className="size-5" aria-hidden="true" />
        Change password
      </h2>
      <p className="mt-2 text-sm text-text-secondary">
        Enter your current password to set a new one.
      </p>
      <form className="mt-5 space-y-5" onSubmit={submit} noValidate>
        <fieldset disabled={isSubmitting || isLoggingOut} className="space-y-5 disabled:opacity-60">
          <PasswordField
            {...register("currentPassword")}
            autoFocus
            label="Current password"
            autoComplete="current-password"
            aria-invalid={Boolean(errors.currentPassword)}
            error={errors.currentPassword?.message}
          />
          <div className="grid gap-5 sm:grid-cols-2">
            <PasswordField
              {...register("newPassword")}
              label="New password"
              autoComplete="new-password"
              aria-invalid={Boolean(errors.newPassword)}
              error={errors.newPassword?.message}
            />
            <PasswordField
              {...register("confirmPassword")}
              label="Confirm new password"
              autoComplete="new-password"
              aria-invalid={Boolean(errors.confirmPassword)}
              error={errors.confirmPassword?.message}
            />
          </div>
          <p className="text-xs text-text-muted">
            Use 8–72 characters, including uppercase, lowercase, a number and a special character
            (maximum 72 UTF-8 bytes).
          </p>
          {errors.root?.server?.message && (
            <p role="alert" className="text-sm text-danger">
              {errors.root.server.message}
            </p>
          )}
          <div className="flex flex-wrap justify-end gap-3 border-t border-border-default pt-5">
            <button
              type="button"
              onClick={() => {
                reset();
                onClose();
              }}
              className="rounded-full border border-border-default px-5 py-2.5 text-sm font-semibold text-text-secondary hover:bg-bg-subtle"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="inline-flex items-center gap-2 rounded-full border border-primary-200 bg-primary-50 px-5 py-2.5 text-sm font-semibold text-primary-700 hover:bg-primary-100 disabled:cursor-not-allowed"
            >
              {isSubmitting && <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />}
              {isSubmitting ? "Changing password..." : "Save new password"}
            </button>
          </div>
        </fieldset>
      </form>
    </section>
  );
}
