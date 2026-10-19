import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { PenaltyRule, UpdatePenaltyRulePayload } from "../types/adminTrust.types";
import {
  penaltyRuleSchema,
  toIsoOrUndefined,
  type PenaltyRuleFormData,
} from "../schemas/trust.schema";
import { inputClass } from "../utils/formStyles";
import { FormField, ModalActions } from "./FormField";

interface PenaltyRuleModalProps {
  rule: PenaltyRule | null;
  onClose: () => void;
  /** Resolves with an error message, or null on success. */
  onSave: (ruleId: string, payload: UpdatePenaltyRulePayload) => Promise<string | null>;
}

// UC-134: saving creates a new version of the penalty rule.
export function PenaltyRuleModal({ rule, onClose, onSave }: PenaltyRuleModalProps) {
  const [serverError, setServerError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<PenaltyRuleFormData>({
    resolver: zodResolver(penaltyRuleSchema),
    defaultValues: {
      minimumOccurrence: 1,
      scoreDeduction: 1,
      requiredOnlineOrders: 0,
      requiredOnlineOrderValue: 0,
      permanentOfflineLock: false,
      isActive: true,
      effectiveFrom: "",
    },
  });

  useEffect(() => {
    if (!rule) return;
    reset({
      minimumOccurrence: rule.minimumOccurrence,
      scoreDeduction: rule.scoreDeduction,
      requiredOnlineOrders: rule.requiredOnlineOrders,
      requiredOnlineOrderValue: rule.requiredOnlineOrderValue,
      permanentOfflineLock: rule.permanentOfflineLock,
      isActive: rule.isActive,
      effectiveFrom: "",
    });
  }, [rule, reset]);

  const handleClose = () => {
    setServerError(null);
    onClose();
  };

  const onSubmit = async (data: PenaltyRuleFormData) => {
    if (!rule) return;
    const { effectiveFrom, ...values } = data;
    const error = await onSave(rule.id, {
      ...values,
      effectiveFrom: toIsoOrUndefined(effectiveFrom),
    });
    if (error) setServerError(error);
    else handleClose();
  };

  return (
    <SharedModal isOpen={rule !== null} onClose={handleClose} title="Update penalty rule">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <p className="text-sm text-text-secondary">
          Saving creates a new version of this rule. Penalties already issued keep the requirements
          they were created with.
        </p>

        <FormField
          label="Applies from no-show number"
          htmlFor="pr-occurrence"
          error={errors.minimumOccurrence?.message}
          hint="Counted per customer, per month."
        >
          <input
            id="pr-occurrence"
            type="number"
            inputMode="numeric"
            {...register("minimumOccurrence", { valueAsNumber: true })}
            className={inputClass(!!errors.minimumOccurrence)}
          />
        </FormField>

        <FormField
          label="Points deducted"
          htmlFor="pr-deduction"
          error={errors.scoreDeduction?.message}
        >
          <input
            id="pr-deduction"
            type="number"
            inputMode="numeric"
            {...register("scoreDeduction", { valueAsNumber: true })}
            className={inputClass(!!errors.scoreDeduction)}
          />
        </FormField>

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            label="Online orders to recover"
            htmlFor="pr-orders"
            error={errors.requiredOnlineOrders?.message}
          >
            <input
              id="pr-orders"
              type="number"
              inputMode="numeric"
              {...register("requiredOnlineOrders", { valueAsNumber: true })}
              className={inputClass(!!errors.requiredOnlineOrders)}
            />
          </FormField>
          <FormField
            label="Online order value (VND)"
            htmlFor="pr-value"
            error={errors.requiredOnlineOrderValue?.message}
          >
            <input
              id="pr-value"
              type="number"
              inputMode="numeric"
              {...register("requiredOnlineOrderValue", { valueAsNumber: true })}
              className={inputClass(!!errors.requiredOnlineOrderValue)}
            />
          </FormField>
        </div>

        <FormField
          label="Take effect at"
          htmlFor="pr-effective"
          hint="Leave empty to start immediately."
        >
          <input
            id="pr-effective"
            type="datetime-local"
            {...register("effectiveFrom")}
            className={inputClass(false)}
          />
        </FormField>

        <div className="space-y-3">
          <label className="flex cursor-pointer items-center gap-3 text-sm font-semibold text-text-primary">
            <input
              type="checkbox"
              {...register("permanentOfflineLock")}
              className="size-4 rounded border-border-default text-primary-600 focus:ring-primary-500"
            />
            Permanently turn off pay at shop
          </label>
          <label className="flex cursor-pointer items-center gap-3 text-sm font-semibold text-text-primary">
            <input
              type="checkbox"
              {...register("isActive")}
              className="size-4 rounded border-border-default text-primary-600 focus:ring-primary-500"
            />
            Rule is enabled
          </label>
        </div>

        <ModalActions
          onCancel={handleClose}
          isSubmitting={isSubmitting}
          submitLabel="Save new version"
          serverError={serverError}
        />
      </form>
    </SharedModal>
  );
}
