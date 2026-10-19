import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { AdminTrustLevel } from "../types/adminTrust.types";
import { levelBenefitsSchema, type LevelBenefitsFormData } from "../schemas/trust.schema";
import { inputClass } from "../utils/formStyles";
import { FormField, ModalActions } from "./FormField";

interface LevelBenefitsModalProps {
  level: AdminTrustLevel | null;
  onClose: () => void;
  /** Resolves with an error message, or null on success. */
  onSave: (levelId: string, description: string | null) => Promise<string | null>;
}

// UC-130: edit the benefits shown to customers for one level.
export function LevelBenefitsModal({ level, onClose, onSave }: LevelBenefitsModalProps) {
  const [serverError, setServerError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<LevelBenefitsFormData>({
    resolver: zodResolver(levelBenefitsSchema),
    defaultValues: { description: "" },
  });

  useEffect(() => {
    if (!level) return;
    reset({ description: level.description ?? "" });
  }, [level, reset]);

  const handleClose = () => {
    setServerError(null);
    onClose();
  };

  const onSubmit = async (data: LevelBenefitsFormData) => {
    if (!level) return;
    const trimmed = data.description.trim();
    const error = await onSave(level.id, trimmed === "" ? null : trimmed);
    if (error) setServerError(error);
    else handleClose();
  };

  return (
    <SharedModal
      isOpen={level !== null}
      onClose={handleClose}
      title={level ? `${level.name} benefits` : undefined}
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          label="Benefits"
          htmlFor="level-benefits"
          error={errors.description?.message}
          hint="Customers see this text on their Trust Score page. Use a new line for each benefit."
        >
          <textarea
            id="level-benefits"
            rows={6}
            {...register("description")}
            className={`${inputClass(!!errors.description)} resize-none`}
            placeholder="Describe what customers on this level get"
          />
        </FormField>
        <ModalActions
          onCancel={handleClose}
          isSubmitting={isSubmitting}
          submitLabel="Save benefits"
          serverError={serverError}
        />
      </form>
    </SharedModal>
  );
}
