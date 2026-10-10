import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import type { AuthUser } from "@/features/auth/types/auth.types";
import type { UpdateUserProfileInput } from "@/features/profiles/types/profile.types";
import {
  updateUserProfileSchema,
  type UpdateUserProfileFormValues,
} from "@/features/profiles/schemas/profile.schema";
import { getApiErrorMessage, getApiValidationErrors } from "@/shared/api";
import { Check, LoaderCircle, X } from "lucide-react";

interface EditProfileFormProps {
  user: AuthUser;
  isSubmitting: boolean;
  selectedAvatarFile: File | null;
  onSubmit: (input: UpdateUserProfileInput, avatarFile: File | null) => Promise<void>;
  onCancel: () => void;
}

const inputClass =
  "mt-1 block w-full rounded-xl border border-border-default bg-bg-surface px-4 py-3 text-sm text-text-primary outline-none transition focus:border-primary-500 focus:ring-2 focus:ring-primary-100";

export function EditProfileForm({
  user,
  isSubmitting,
  selectedAvatarFile,
  onSubmit,
  onCancel,
}: EditProfileFormProps) {
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<UpdateUserProfileFormValues>({
    resolver: zodResolver(updateUserProfileSchema),
    mode: "onTouched",
    defaultValues: {
      fullName: user.fullName,
      phone: user.phone ?? "",
    },
  });

  useEffect(() => {
    reset({
      fullName: user.fullName,
      phone: user.phone ?? "",
    });
  }, [reset, user.fullName, user.phone]);

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(
        {
          fullName: values.fullName.trim(),
          phone: values.phone.trim() || null,
        },
        selectedAvatarFile,
      );
    } catch (error) {
      const apiErrors = getApiValidationErrors(error);

      if (apiErrors.fullName) {
        setError("fullName", {
          message: apiErrors.fullName,
        });
      }

      if (apiErrors.phone) {
        setError("phone", {
          message: apiErrors.phone,
        });
      }

      setError("root.server", {
        message: getApiErrorMessage(error),
      });
    }
  });

  return (
    <form className="mt-6 space-y-5" onSubmit={submit} noValidate>
      <label className="block text-sm font-semibold text-text-primary">
        Full name
        <input
          {...register("fullName")}
          autoComplete="name"
          aria-invalid={Boolean(errors.fullName)}
          className={inputClass}
          placeholder="Your full name"
        />
        {errors.fullName?.message && (
          <span className="mt-1 block text-xs text-danger">{errors.fullName.message}</span>
        )}
      </label>

      <label className="block text-sm font-semibold text-text-primary">
        Phone number
        <input
          {...register("phone")}
          type="tel"
          inputMode="numeric"
          autoComplete="tel"
          aria-invalid={Boolean(errors.phone)}
          className={inputClass}
          placeholder="Your phone number"
        />
        {errors.phone?.message && (
          <span className="mt-1 block text-xs text-danger">{errors.phone.message}</span>
        )}
      </label>

      {errors.root?.server?.message && (
        <div className="rounded-xl border border-danger/20 bg-danger/5 px-4 py-3 text-sm text-danger">
          {errors.root.server.message}
        </div>
      )}

      <div className="flex flex-wrap justify-end gap-3 border-t border-border-default pt-5">
        <button
          type="button"
          onClick={onCancel}
          disabled={isSubmitting}
          className="inline-flex items-center gap-2 rounded-full border border-border-default px-5 py-2.5 text-sm font-semibold text-text-secondary transition-colors hover:bg-bg-subtle disabled:cursor-not-allowed disabled:opacity-60"
        >
          <X className="size-4" aria-hidden="true" />
          Cancel
        </button>

        <button
          type="submit"
          disabled={isSubmitting}
          className="inline-flex items-center gap-2 rounded-full border border-primary-200 bg-primary-50 px-5 py-2.5 text-sm font-semibold text-primary-700 transition-colors hover:bg-primary-100 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isSubmitting ? (
            <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
          ) : (
            <Check className="size-4" aria-hidden="true" />
          )}

          {isSubmitting ? (selectedAvatarFile ? "Saving changes..." : "Saving...") : "Save changes"}
        </button>
      </div>
    </form>
  );
}
