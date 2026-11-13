import { useEffect, useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { RecordId } from "@/shared/components/RecordId";
import type {
  CreatePenaltyRulePayload,
  PenaltyRule,
  UpdatePenaltyRulePayload,
} from "../types/adminTrust.types";
import {
  penaltyRuleSchema,
  toDateTimeLocalValue,
  toIsoOrUndefined,
  type PenaltyRuleFormData,
} from "../schemas/trust.schema";
import { formatDateTime } from "../utils/format";
import { inputClass } from "../utils/formStyles";
import { FormField, ModalActions } from "./FormField";

interface PenaltyRuleModalProps {
  isOpen: boolean;
  /** null = create mode (UC-156), PenaltyRule = edit mode (UC-134) */
  rule: PenaltyRule | null;
  existingRules: PenaltyRule[];
  suggestedOccurrence?: number;
  onClose: () => void;
  onCreate: (payload: CreatePenaltyRulePayload) => Promise<string | null>;
  onSave: (ruleId: string, payload: UpdatePenaltyRulePayload) => Promise<string | null>;
}

const ONE_MINUTE_MS = 60_000;

// Read current time at module scope: only called from event handler (submit), not during render.
const getNowMs = () => Date.now();

export function PenaltyRuleModal({
  isOpen,
  rule,
  existingRules,
  suggestedOccurrence = 1,
  onClose,
  onCreate,
  onSave,
}: PenaltyRuleModalProps) {
  const isEditMode = rule !== null;
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<PenaltyRuleFormData>({
    resolver: zodResolver(penaltyRuleSchema),
    defaultValues: {
      minimumOccurrence: suggestedOccurrence,
      scoreDeduction: 20,
      requiredOnlineOrders: 1,
      requiredOnlineOrderValue: 50000,
      permanentOfflineLock: false,
      isActive: true,
      effectiveFrom: "",
      effectiveTo: "",
    },
  });

  const watchedOccurrence = useWatch({ control, name: "minimumOccurrence" });
  const watchedEffectiveFrom = useWatch({ control, name: "effectiveFrom" });
  const watchedEffectiveTo = useWatch({ control, name: "effectiveTo" });

  useEffect(() => {
    if (!isOpen) return;

    if (rule) {
      reset({
        minimumOccurrence: rule.minimumOccurrence,
        scoreDeduction: rule.scoreDeduction,
        requiredOnlineOrders: rule.requiredOnlineOrders,
        requiredOnlineOrderValue: rule.requiredOnlineOrderValue,
        permanentOfflineLock: rule.permanentOfflineLock,
        isActive: rule.isActive,
        effectiveFrom: toDateTimeLocalValue(rule.effectiveFrom),
        effectiveTo: toDateTimeLocalValue(rule.effectiveTo),
      });
    } else {
      reset({
        minimumOccurrence: suggestedOccurrence,
        scoreDeduction: 20,
        requiredOnlineOrders: 1,
        requiredOnlineOrderValue: 50000,
        permanentOfflineLock: false,
        isActive: true,
        effectiveFrom: "",
        effectiveTo: "",
      });
    }
  }, [isOpen, rule, suggestedOccurrence, reset]);

  useEffect(() => {
    if (!isOpen || rule) return;
    const latest = existingRules
      .filter((r) => r.minimumOccurrence === Number(watchedOccurrence))
      .sort((a, b) => Date.parse(b.effectiveFrom) - Date.parse(a.effectiveFrom))[0];
    if (latest?.effectiveTo && Date.parse(latest.effectiveTo) > Date.now()) {
      setValue("effectiveFrom", toDateTimeLocalValue(latest.effectiveTo));
    } else setValue("effectiveFrom", "");
    setValue("effectiveTo", "");
  }, [isOpen, rule, existingRules, watchedOccurrence, setValue]);

  const handleClose = () => {
    setServerError(null);
    onClose();
  };

  // Existing schedules for the selected occurrence so the admin can easily see occupied intervals.
  const sameOccurrenceRules = existingRules
    .filter((r) => r.minimumOccurrence === Number(watchedOccurrence) && r.id !== rule?.id)
    .sort((a, b) => new Date(a.effectiveFrom).getTime() - new Date(b.effectiveFrom).getTime());

  const onSubmit = async (data: PenaltyRuleFormData) => {
    setServerError(null);
    const nowMs = getNowMs();

    // 1. Resolve From timestamp
    let fromIso: string | undefined;
    let fromMs: number;

    if (isEditMode) {
      if (!data.effectiveFrom) {
        setError("effectiveFrom", {
          type: "manual",
          message: "'From' date/time is required when editing a rule.",
        });
        return;
      }

      const parsedFromMs = new Date(data.effectiveFrom).getTime();
      const originalFromMs = new Date(rule.effectiveFrom).getTime();
      const unchangedFrom = Math.abs(parsedFromMs - originalFromMs) < ONE_MINUTE_MS;

      if (!unchangedFrom && parsedFromMs < nowMs - ONE_MINUTE_MS) {
        setError("effectiveFrom", {
          type: "manual",
          message:
            originalFromMs > nowMs
              ? "'From' date/time cannot be in the past."
              : "Cannot move the start time of an already-started rule to another time in the past.",
        });
        return;
      }

      fromMs = unchangedFrom ? originalFromMs : parsedFromMs;
      fromIso = unchangedFrom ? rule.effectiveFrom : toIsoOrUndefined(data.effectiveFrom);
    } else {
      if (data.effectiveFrom) {
        const parsedFromMs = new Date(data.effectiveFrom).getTime();
        if (parsedFromMs < nowMs - ONE_MINUTE_MS) {
          setError("effectiveFrom", {
            type: "manual",
            message: "'From' date/time cannot be in the past.",
          });
          return;
        }
        fromMs = parsedFromMs;
        fromIso = toIsoOrUndefined(data.effectiveFrom);
      } else {
        fromMs = nowMs;
        fromIso = undefined;
      }
    }

    if (!isEditMode && data.effectiveFrom) {
      const adjoining = sameOccurrenceRules.find(
        (other) =>
          other.effectiveTo && Math.abs(Date.parse(other.effectiveTo) - fromMs) < ONE_MINUTE_MS,
      );
      if (adjoining?.effectiveTo) {
        fromIso = adjoining.effectiveTo;
        fromMs = Date.parse(adjoining.effectiveTo);
      }
    }

    // 2. Resolve Until timestamp
    let toIso: string | null = null;
    let toMs: number = Number.POSITIVE_INFINITY;

    if (data.effectiveTo) {
      const parsedToMs = new Date(data.effectiveTo).getTime();
      if (parsedToMs <= fromMs) {
        setError("effectiveTo", {
          type: "manual",
          message: "'Until' date/time must be strictly after 'From' date/time.",
        });
        return;
      }
      toMs = parsedToMs;
      toIso = toIsoOrUndefined(data.effectiveTo) ?? null;
    }

    if (!isEditMode && !data.effectiveTo) {
      const next = sameOccurrenceRules.find((other) => Date.parse(other.effectiveFrom) > fromMs);
      if (next) {
        toIso = next.effectiveFrom;
        toMs = Date.parse(next.effectiveFrom);
      }
    }

    // 3. Validate non-overlapping intervals with all other rules of the same MinimumOccurrence
    const overlapping = existingRules.find((other) => {
      if (other.id === rule?.id) return false;
      if (other.minimumOccurrence !== data.minimumOccurrence) return false;

      const otherFromMs = new Date(other.effectiveFrom).getTime();
      const otherToMs = other.effectiveTo
        ? new Date(other.effectiveTo).getTime()
        : Number.POSITIVE_INFINITY;

      // Creating a successor automatically closes an open-ended predecessor.
      if (!isEditMode && !other.effectiveTo && otherFromMs < fromMs) return false;
      return fromMs < otherToMs && otherFromMs < toMs;
    });

    if (overlapping) {
      const rangeText = `${formatDateTime(overlapping.effectiveFrom)} – ${
        overlapping.effectiveTo ? formatDateTime(overlapping.effectiveTo) : "Indefinite"
      }`;
      setServerError(
        `Time period overlaps with an existing rule for no-show #${overlapping.minimumOccurrence} (${rangeText}). ` +
          `Choose a period outside the existing schedule. Open-ended predecessors are closed automatically when adding a successor.`,
      );
      return;
    }

    const payload = {
      minimumOccurrence: data.minimumOccurrence,
      scoreDeduction: data.scoreDeduction,
      requiredOnlineOrders: data.requiredOnlineOrders,
      requiredOnlineOrderValue: data.requiredOnlineOrderValue,
      permanentOfflineLock: data.permanentOfflineLock,
      isActive: data.isActive,
      effectiveFrom: fromIso,
      effectiveTo: toIso,
    };

    const error = isEditMode ? await onSave(rule.id, payload) : await onCreate(payload);

    if (error) setServerError(error);
    else handleClose();
  };

  return (
    <SharedModal
      isOpen={isOpen}
      onClose={handleClose}
      title={
        isEditMode
          ? `Edit penalty rule (No-show #${rule.minimumOccurrence})`
          : "Add new penalty rule"
      }
      maxWidthClass="max-w-lg"
    >
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
        {rule && <RecordId id={rule.id} variant="full" />}
        <p className="text-sm text-text-secondary">
          Configure the points deducted, recovery requirements, and effective period. Rules with the
          same no-show number cannot overlap. Adding a successor automatically ends its open-ended
          predecessor. Gaps are allowed only when no higher no-show threshold is in effect.
        </p>

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            label="Applies from no-show #"
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
        </div>

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
              step="1000"
              inputMode="numeric"
              {...register("requiredOnlineOrderValue", { valueAsNumber: true })}
              className={inputClass(!!errors.requiredOnlineOrderValue)}
            />
          </FormField>
        </div>

        {sameOccurrenceRules.length > 0 && (
          <div className="rounded-lg border border-neutral-200 bg-neutral-50 p-3 text-xs text-text-secondary">
            <p className="font-semibold text-text-primary">
              Existing periods for no-show #{watchedOccurrence}:
            </p>
            <ul className="mt-1 space-y-0.5 pl-4 list-disc">
              {sameOccurrenceRules.map((item) => (
                <li key={item.id} className="tabular-nums">
                  From <strong>{formatDateTime(item.effectiveFrom)}</strong>{" "}
                  {item.effectiveTo ? (
                    <>
                      until <strong>{formatDateTime(item.effectiveTo)}</strong>
                    </>
                  ) : (
                    <span className="text-amber-700 font-semibold">
                      (Open-ended — automatically ends when its successor starts)
                    </span>
                  )}
                </li>
              ))}
            </ul>
          </div>
        )}

        {!isEditMode && sameOccurrenceRules.some((r) => !r.effectiveTo) && (
          <p className="rounded-lg bg-primary-50 p-3 text-sm text-primary-700">
            The existing open-ended rule will end{" "}
            {watchedEffectiveFrom
              ? `at ${formatDateTime(new Date(watchedEffectiveFrom).toISOString())}`
              : "when the new rule starts"}
            . Both changes are saved together.
          </p>
        )}
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            label="Effective from"
            htmlFor="pr-effective-from"
            error={errors.effectiveFrom?.message}
            hint={isEditMode ? "Start of effective period." : "Leave empty to start immediately."}
          >
            <input
              id="pr-effective-from"
              type="datetime-local"
              {...register("effectiveFrom")}
              className={inputClass(!!errors.effectiveFrom)}
            />
          </FormField>

          <FormField
            label="Effective until (optional)"
            htmlFor="pr-effective-to"
            error={errors.effectiveTo?.message}
            hint="Leave empty to end at the next scheduled version, or remain indefinite if none exists."
          >
            <div className="space-y-1">
              <input
                id="pr-effective-to"
                type="datetime-local"
                {...register("effectiveTo")}
                className={inputClass(!!errors.effectiveTo)}
              />
              {watchedEffectiveTo && (
                <button
                  type="button"
                  onClick={() => setValue("effectiveTo", "", { shouldValidate: true })}
                  className="text-xs font-medium text-primary-700 hover:underline"
                >
                  Clear end date (set indefinite)
                </button>
              )}
            </div>
          </FormField>
        </div>

        <div className="space-y-3 pt-1">
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
          submitLabel={isEditMode ? "Save changes" : "Create penalty rule"}
          serverError={serverError}
        />
      </form>
    </SharedModal>
  );
}
