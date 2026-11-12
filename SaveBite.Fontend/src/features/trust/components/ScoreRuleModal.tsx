import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { RecordId } from "@/shared/components/RecordId";
import type { TrustScoreRule, UpdateScoreRulePayload } from "../types/adminTrust.types";
import { buildScoreRuleSchema, type ScoreRuleFormData } from "../schemas/trust.schema";
import { inputClass } from "../utils/formStyles";
import { FormField, ModalActions } from "./FormField";

interface ScoreRuleModalProps {
  rule: TrustScoreRule | null;
  onClose: () => void;
  /** Resolves with an error message, or null on success. */
  onSave: (ruleId: string, payload: UpdateScoreRulePayload) => Promise<string | null>;
}

// Fixed system earning rules: points and status are edited together, effective immediately.
export function ScoreRuleModal({ rule, onClose, onSave }: ScoreRuleModalProps) {
  const [serverError, setServerError] = useState<string | null>(null);
  const kind = rule?.kind ?? "Earning";
  const schema = useMemo(() => buildScoreRuleSchema(kind), [kind]);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ScoreRuleFormData>({
    resolver: zodResolver(schema),
    defaultValues: { scoreDelta: 0, isActive: true },
  });

  useEffect(() => {
    if (!rule) return;
    reset({ scoreDelta: rule.scoreDelta, isActive: rule.isActive });
  }, [rule, reset]);

  const handleClose = () => {
    setServerError(null);
    onClose();
  };

  const onSubmit = async (data: ScoreRuleFormData) => {
    if (!rule) return;
    const error = await onSave(rule.id, {
      scoreDelta: data.scoreDelta,
      isActive: data.isActive,
    });
    if (error) setServerError(error);
    else handleClose();
  };

  return (
    <SharedModal
      isOpen={rule !== null}
      onClose={handleClose}
      title={kind === "Earning" ? "Update earning rule" : "Update penalty score rule"}
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <p className="text-sm text-text-secondary">
          Changes apply immediately to subsequent order completions. Previously awarded points stay
          unchanged.
        </p>
        {rule && <RecordId id={rule.id} variant="full" />}

        <FormField
          label="Points"
          htmlFor="sr-points"
          error={errors.scoreDelta?.message}
          hint={
            kind === "Earning"
              ? "Points added to the score. Use a positive number."
              : "Points removed from the score. Use a negative number."
          }
        >
          <input
            id="sr-points"
            type="number"
            inputMode="numeric"
            {...register("scoreDelta", { valueAsNumber: true })}
            className={inputClass(!!errors.scoreDelta)}
          />
        </FormField>

        <label className="flex min-h-11 items-center gap-3 text-sm font-medium">
          <input type="checkbox" {...register("isActive")} className="size-4 accent-primary-700" />
          Active — award points for this order type
        </label>
        <p className="text-sm text-text-secondary">
          Inactive stops points only. Eligible order counts and accumulated value still increase
          outside penalty recovery.
        </p>

        <ModalActions
          onCancel={handleClose}
          isSubmitting={isSubmitting}
          submitLabel="Save changes"
          serverError={serverError}
        />
      </form>
    </SharedModal>
  );
}
