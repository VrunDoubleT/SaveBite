import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { AdminTrustLevel, UpdateLevelRequirementPayload } from "../types/adminTrust.types";
import { levelRequirementSchema, type LevelRequirementFormData } from "../schemas/trust.schema";
import { inputClass } from "../utils/formStyles";
import { FormField, ModalActions } from "./FormField";

interface LevelRequirementModalProps {
  level: AdminTrustLevel | null;
  onClose: () => void;
  /** Resolves with an error message, or null on success. */
  onSave: (levelId: string, payload: UpdateLevelRequirementPayload) => Promise<string | null>;
}

// UC-132: edit the thresholds a customer must reach for one level.
export function LevelRequirementModal({ level, onClose, onSave }: LevelRequirementModalProps) {
  const [serverError, setServerError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<LevelRequirementFormData>({
    resolver: zodResolver(levelRequirementSchema),
    defaultValues: { minScore: 0, minCompletedOrders: 0, minTotalOrderValue: 0 },
  });

  useEffect(() => {
    if (!level) return;
    reset({
      minScore: level.requirement?.minScore ?? 0,
      minCompletedOrders: level.requirement?.minCompletedOrders ?? 0,
      minTotalOrderValue: level.requirement?.minTotalOrderValue ?? 0,
    });
  }, [level, reset]);

  const handleClose = () => {
    setServerError(null);
    onClose();
  };

  const onSubmit = async (data: LevelRequirementFormData) => {
    if (!level) return;
    const error = await onSave(level.id, data);
    if (error) setServerError(error);
    else handleClose();
  };

  return (
    <SharedModal
      isOpen={level !== null}
      onClose={handleClose}
      title={level ? `${level.name} requirements` : undefined}
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <p className="text-sm text-text-secondary">
          Customers reach this level when they meet all three requirements. Requirements must grow
          with the level rank, and the lowest level must stay at zero.
        </p>
        <FormField label="Minimum Trust Score" htmlFor="req-score" error={errors.minScore?.message}>
          <input
            id="req-score"
            type="number"
            inputMode="numeric"
            {...register("minScore", { valueAsNumber: true })}
            className={inputClass(!!errors.minScore)}
          />
        </FormField>
        <FormField
          label="Minimum completed orders"
          htmlFor="req-orders"
          error={errors.minCompletedOrders?.message}
        >
          <input
            id="req-orders"
            type="number"
            inputMode="numeric"
            {...register("minCompletedOrders", { valueAsNumber: true })}
            className={inputClass(!!errors.minCompletedOrders)}
          />
        </FormField>
        <FormField
          label="Minimum total order value (VND)"
          htmlFor="req-value"
          error={errors.minTotalOrderValue?.message}
        >
          <input
            id="req-value"
            type="number"
            inputMode="numeric"
            {...register("minTotalOrderValue", { valueAsNumber: true })}
            className={inputClass(!!errors.minTotalOrderValue)}
          />
        </FormField>
        <ModalActions
          onCancel={handleClose}
          isSubmitting={isSubmitting}
          submitLabel="Save requirements"
          serverError={serverError}
        />
      </form>
    </SharedModal>
  );
}
