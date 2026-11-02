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
import { Button, FormField, Input } from "@/shared/components/ui";
import { Check, X } from "lucide-react";

interface EditProfileFormProps {
  user: AuthUser;
  isSubmitting: boolean;
  selectedAvatarFile: File | null;
  onSubmit: (input: UpdateUserProfileInput, avatarFile: File | null) => Promise<void>;
  onCancel: () => void;
}

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
    defaultValues: { fullName: user.fullName, phone: user.phone ?? "" },
  });
  useEffect(() => {
    reset({ fullName: user.fullName, phone: user.phone ?? "" });
  }, [reset, user.fullName, user.phone]);
  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(
        { fullName: values.fullName.trim(), phone: values.phone.trim() || null },
        selectedAvatarFile,
      );
    } catch (error) {
      const apiErrors = getApiValidationErrors(error);
      if (apiErrors.fullName) setError("fullName", { message: apiErrors.fullName });
      if (apiErrors.phone) setError("phone", { message: apiErrors.phone });
      setError("root.server", { message: getApiErrorMessage(error) });
    }
  });
  return (
    <form className="space-y-4" onSubmit={submit} noValidate aria-labelledby="edit-profile-heading">
      <div>
        <h3 id="edit-profile-heading" className="text-section font-semibold text-text-primary">
          Edit personal information
        </h3>
        <p className="mt-1 text-body text-text-secondary">
          Update your name and phone number. Your email stays linked to this account.
        </p>
      </div>
      <fieldset disabled={isSubmitting} className="grid min-w-0 gap-4 sm:grid-cols-2">
        <FormField label="Full name" error={errors.fullName?.message}>
          {(field) => (
            <Input
              variant="auth"
              {...field}
              {...register("fullName")}
              required
              autoComplete="name"
              maxLength={150}
              placeholder="Your full name"
              className="min-h-11"
            />
          )}
        </FormField>
        <FormField
          label="Phone number"
          hint="Optional · 9 or 10 digits"
          error={errors.phone?.message}
        >
          {(field) => (
            <Input
              variant="auth"
              {...field}
              {...register("phone")}
              type="tel"
              inputMode="numeric"
              autoComplete="tel"
              placeholder="Your phone number"
              className="min-h-11"
            />
          )}
        </FormField>
      </fieldset>
      {errors.root?.server?.message && (
        <div
          role="alert"
          className="rounded-md border border-danger-soft bg-danger-soft p-3 text-body text-danger-strong"
        >
          {errors.root.server.message}
        </div>
      )}
      <div className="flex flex-wrap justify-end gap-3 border-t border-border-default pt-4">
        <Button variant="secondary" onClick={onCancel} disabled={isSubmitting} className="min-h-11">
          <X className="size-4" aria-hidden="true" />
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting} loading={isSubmitting} className="min-h-11">
          <Check className="size-4" aria-hidden="true" />
          {isSubmitting ? "Saving changes…" : "Save changes"}
        </Button>
      </div>
    </form>
  );
}
