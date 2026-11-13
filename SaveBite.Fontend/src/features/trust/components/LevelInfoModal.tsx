import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { RecordId } from "@/shared/components/RecordId";
import type { AdminTrustLevel, UpdateLevelInfoPayload } from "../types/adminTrust.types";
import { levelInfoSchema, type LevelInfoFormData } from "../schemas/trust.schema";
import { inputClass } from "../utils/formStyles";
import { FormField, ModalActions } from "./FormField";

interface LevelInfoModalProps {
  level: AdminTrustLevel | null;
  onClose: () => void;
  /** Resolves with an error message, or null on success. */
  onSave: (levelId: string, payload: UpdateLevelInfoPayload) => Promise<string | null>;
}

// UC-154: Update Level Information (Rename only).
export function LevelInfoModal({ level, onClose, onSave }: LevelInfoModalProps) {
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    trigger,
    formState: { errors, isSubmitting },
  } = useForm<LevelInfoFormData>({
    resolver: zodResolver(levelInfoSchema),
    mode: "onChange",
    reValidateMode: "onChange",
    defaultValues: { name: "" },
  });

  useEffect(() => {
    if (!level) return;
    reset({ name: level.name });
  }, [level, reset]);

  const handleClose = () => {
    setServerError(null);
    onClose();
  };

  const onSubmit = async (data: LevelInfoFormData) => {
    if (!level) return;
    setServerError(null);
    const error = await onSave(level.id, { name: data.name.trim() });
    if (error) setServerError(error);
    else handleClose();
  };

  return (
    <SharedModal
      isOpen={level !== null}
      onClose={handleClose}
      title={level ? `Rename ${level.name} (Rank ${level.rank})` : undefined}
    >
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
        {level && <RecordId id={level.id} variant="full" />}
        <p className="text-sm text-text-secondary">
          Update the display name of this level. Rank ({level?.rank}), requirements, and deposit
          policy remain unchanged.
        </p>

        <FormField
          label="Level name"
          htmlFor="level-info-name"
          error={errors.name?.message}
          hint="1–100 characters, unique across all levels (case-insensitive)."
        >
          <input
            id="level-info-name"
            type="text"
            maxLength={100}
            {...register("name", {
              onChange: () => trigger("name"),
            })}
            className={inputClass(!!errors.name)}
            placeholder="e.g. Emerald"
          />
        </FormField>

        <ModalActions
          onCancel={handleClose}
          isSubmitting={isSubmitting}
          submitLabel="Save name"
          serverError={serverError}
        />
      </form>
    </SharedModal>
  );
}