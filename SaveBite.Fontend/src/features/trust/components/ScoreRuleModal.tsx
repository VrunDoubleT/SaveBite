import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { TrustScoreRule, UpdateScoreRulePayload } from "../types/adminTrust.types";
import {
  buildScoreRuleSchema,
  toIsoOrUndefined,
  type ScoreRuleFormData,
} from "../schemas/trust.schema";
import { inputClass } from "../utils/formStyles";
import { FormField, ModalActions } from "./FormField";

interface ScoreRuleModalProps {
  rule: TrustScoreRule | null;
  onClose: () => void;
  /** Resolves with an error message, or null on success. */
  onSave: (ruleId: string, payload: UpdateScoreRulePayload) => Promise<string | null>;
}

// UC-136 / UC-138: saving creates a new version of the score rule.
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
    defaultValues: { scoreDelta: 0, effectiveFrom: "" },
  });

  useEffect(() => {
    if (!rule) return;
    reset({ scoreDelta: rule.scoreDelta, effectiveFrom: "" });
  }, [rule, reset]);

  const handleClose = () => {
    setServerError(null);
    onClose();
  };

  const onSubmit = async (data: ScoreRuleFormData) => {
    if (!rule) return;
    const error = await onSave(rule.id, {
      scoreDelta: data.scoreDelta,
      effectiveFrom: toIsoOrUndefined(data.effectiveFrom),
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
          Saving creates a new version of this rule. Score changes already recorded are not
          recalculated.
        </p>

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

        <FormField
          label="Take effect at"
          htmlFor="sr-effective"
          hint="Leave empty to start immediately."
        >
          <input
            id="sr-effective"
            type="datetime-local"
            {...register("effectiveFrom")}
            className={inputClass(false)}
          />
        </FormField>

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
