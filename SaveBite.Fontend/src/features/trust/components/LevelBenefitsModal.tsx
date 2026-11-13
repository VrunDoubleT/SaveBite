import { useEffect, useMemo, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { Plus, Trash2 } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { RecordId } from "@/shared/components/RecordId";
import type {
  AdminTrustLevel,
  TrustLevelDepositRangeInput,
  UpdateLevelBenefitsPayload,
} from "../types/adminTrust.types";
import { buildLevelBenefitsResolver, type LevelBenefitsFormData } from "../schemas/trust.schema";
import { formatCurrency } from "../utils/format";
import { inputClass } from "../utils/formStyles";
import { FormField, ModalActions } from "./FormField";

interface LevelBenefitsModalProps {
  level: AdminTrustLevel | null;
  existingLevels?: AdminTrustLevel[];
  onClose: () => void;
  /** Resolves with an error message, or null on success. */
  onSave: (levelId: string, payload: UpdateLevelBenefitsPayload) => Promise<string | null>;
}

function buildDefaultRanges(
  freeAmount: number,
  maxAmount: number,
): TrustLevelDepositRangeInput[] {
  const safeFree = freeAmount > 0 ? freeAmount : 10000;
  const safeMax = maxAmount > safeFree ? maxAmount : safeFree * 10;
  const mid1 = Math.max(safeFree + 1000, Math.round((safeMax * 0.4) / 1000) * 1000);
  const mid2 = Math.max(mid1 + 1000, Math.round((safeMax * 0.8) / 1000) * 1000);

  if (safeMax <= mid1) {
    return [
      {
        fromOrderAmount: safeFree,
        toOrderAmount: safeMax,
        depositAmount: Math.max(2000, Math.min(safeFree, Math.round((safeFree * 0.5) / 1000) * 1000)),
      },
    ];
  }

  return [
    {
      fromOrderAmount: safeFree,
      toOrderAmount: mid1,
      depositAmount: Math.max(2000, Math.min(safeFree, Math.round((safeMax * 0.05) / 1000) * 1000)),
    },
    {
      fromOrderAmount: mid1,
      toOrderAmount: mid2,
      depositAmount: Math.max(2000, Math.min(mid1, Math.round((safeMax * 0.2) / 1000) * 1000)),
    },
    {
      fromOrderAmount: mid2,
      toOrderAmount: safeMax,
      depositAmount: Math.max(2000, Math.min(mid2, Math.round((safeMax * 0.3) / 1000) * 1000)),
    },
  ];
}

// UC-130: edit the benefits and PayAtShop deposit policy for one level.
export function LevelBenefitsModal({
  level,
  existingLevels,
  onClose,
  onSave,
}: LevelBenefitsModalProps) {
  const [serverError, setServerError] = useState<string | null>(null);

  const precedingLevel = useMemo(() => {
    if (!level || !existingLevels) return null;
    const lower = existingLevels
      .filter((l) => l.id !== level.id && l.rank < level.rank)
      .sort((a, b) => b.rank - a.rank);
    return lower[0] ?? null;
  }, [level, existingLevels]);

  const succeedingLevel = useMemo(() => {
    if (!level || !existingLevels) return null;
    const higher = existingLevels
      .filter((l) => l.id !== level.id && l.rank > level.rank)
      .sort((a, b) => a.rank - b.rank);
    return higher[0] ?? null;
  }, [level, existingLevels]);

  const resolver = useMemo(
    () => buildLevelBenefitsResolver(precedingLevel, succeedingLevel),
    [precedingLevel, succeedingLevel],
  );

  const {
    register,
    control,
    handleSubmit,
    reset,
    watch,
    getValues,
    setValue,
    clearErrors,
    trigger,
    formState: { errors, isSubmitting },
  } = useForm<LevelBenefitsFormData>({
    resolver,
    mode: "onChange",
    reValidateMode: "onChange",
    defaultValues: {
      description: "",
      requireFullOnlinePayment: false,
      depositFreeOrderAmount: 10000,
      maxDepositOrderAmount: 100000,
      ranges: [],
    },
  });

  const { fields, append, replace } = useFieldArray({
    control,
    name: "ranges",
  });

  const requireFullOnline = watch("requireFullOnlinePayment");
  const watchedRanges = watch("ranges");

  useEffect(() => {
    if (!level) return;

    const policy = level.depositPolicy;
    const isFullOnline = policy?.requireFullOnlinePayment ?? level.rank === 1;
    const freeAmount = policy?.depositFreeOrderAmount ?? 10000;
    const maxAmount = policy?.maxDepositOrderAmount ?? 100000;
    const existingRanges =
      policy?.ranges && policy.ranges.length > 0
        ? policy.ranges.map((r) => ({
            fromOrderAmount: r.fromOrderAmount,
            toOrderAmount: r.toOrderAmount,
            depositAmount: r.depositAmount,
          }))
        : buildDefaultRanges(freeAmount, maxAmount);

    reset({
      description: level.description ?? "",
      requireFullOnlinePayment: isFullOnline,
      depositFreeOrderAmount: isFullOnline ? null : freeAmount,
      maxDepositOrderAmount: isFullOnline ? null : maxAmount,
      ranges: isFullOnline ? [] : existingRanges,
    });
  }, [level, reset]);

  const handleClose = () => {
    setServerError(null);
    onClose();
  };

  const handleToggleFullOnline = (checked: boolean) => {
    setValue("requireFullOnlinePayment", checked, { shouldValidate: true });
    if (checked) {
      clearErrors(["depositFreeOrderAmount", "maxDepositOrderAmount", "ranges"]);
    } else if (fields.length === 0) {
      const defaultFree = level?.depositPolicy?.depositFreeOrderAmount ?? 10000;
      const defaultMax = level?.depositPolicy?.maxDepositOrderAmount ?? 100000;
      setValue("depositFreeOrderAmount", defaultFree, { shouldValidate: true });
      setValue("maxDepositOrderAmount", defaultMax, { shouldValidate: true });
      replace(buildDefaultRanges(defaultFree, defaultMax));
    }
    trigger([
      "requireFullOnlinePayment",
      "depositFreeOrderAmount",
      "maxDepositOrderAmount",
      "ranges",
    ]);
  };

  const handleFreeAmountChange = (value: number | undefined) => {
    setValue("depositFreeOrderAmount", value as number, { shouldValidate: true });
    if (fields.length > 0) {
      setValue("ranges.0.fromOrderAmount", value as number, { shouldValidate: true });
    }
    trigger(["depositFreeOrderAmount", "maxDepositOrderAmount", "ranges"]);
  };

  const handleRangeToChange = (index: number, value: number | undefined) => {
    setValue(`ranges.${index}.toOrderAmount`, value as number, { shouldValidate: true });
    if (index + 1 < fields.length) {
      setValue(`ranges.${index + 1}.fromOrderAmount`, value as number, { shouldValidate: true });
    }
    trigger(["depositFreeOrderAmount", "maxDepositOrderAmount", "ranges"]);
  };

  const handleMaxAmountChange = (value: number | undefined) => {
    setValue("maxDepositOrderAmount", value as number, { shouldValidate: true });
    if (fields.length > 0) {
      const lastIndex = fields.length - 1;
      setValue(`ranges.${lastIndex}.toOrderAmount`, value as number, { shouldValidate: true });
    }
    trigger(["depositFreeOrderAmount", "maxDepositOrderAmount", "ranges"]);
  };

  const handleAddRange = () => {
    const maxVal = getValues("maxDepositOrderAmount");
    const currentLen = watchedRanges.length;

    // Previous last tier becomes intermediate: clear its toOrderAmount so user can define this boundary
    if (currentLen > 0) {
      setValue(`ranges.${currentLen - 1}.toOrderAmount`, undefined as unknown as number, {
        shouldValidate: false,
      });
    }

    append({
      fromOrderAmount: undefined as unknown as number,
      toOrderAmount: maxVal ?? (undefined as unknown as number),
      depositAmount: undefined as unknown as number,
    });
  };

  const handleRemoveRange = (index: number) => {
    if (fields.length <= 1) return;
    const currentRanges = [...watchedRanges];
    currentRanges.splice(index, 1);

    const freeAmount = getValues("depositFreeOrderAmount");
    const maxAmount = getValues("maxDepositOrderAmount");

    const validFree =
      typeof freeAmount === "number" && !Number.isNaN(freeAmount) && freeAmount > 0
        ? freeAmount
        : (undefined as unknown as number);
    const validMax =
      typeof maxAmount === "number" && !Number.isNaN(maxAmount) && maxAmount > 0
        ? maxAmount
        : (undefined as unknown as number);

    // Re-chain bounds cleanly without arbitrary increments
    for (let i = 0; i < currentRanges.length; i++) {
      currentRanges[i].fromOrderAmount =
        i === 0 ? validFree : currentRanges[i - 1].toOrderAmount;
    }

    // Ensure the new last tier has its toOrderAmount locked to maxAmount
    if (currentRanges.length > 0) {
      currentRanges[currentRanges.length - 1].toOrderAmount = validMax;
    }

    replace(currentRanges);
    trigger(["depositFreeOrderAmount", "maxDepositOrderAmount", "ranges"]);
  };

  const onSubmit = async (data: LevelBenefitsFormData) => {
    if (!level) return;
    setServerError(null);

    const trimmed = data.description.trim();
    const freeAmount = data.depositFreeOrderAmount;
    const maxAmount = data.maxDepositOrderAmount;
    const processedRanges: TrustLevelDepositRangeInput[] = data.ranges.map((r, i, arr) => {
      const from = i === 0 ? (freeAmount ?? Number(r.fromOrderAmount)) : Number(arr[i - 1].toOrderAmount);
      const to = i === arr.length - 1 ? (maxAmount ?? Number(r.toOrderAmount)) : Number(r.toOrderAmount);
      return {
        fromOrderAmount: Number(from) || 0,
        toOrderAmount: Number(to) || 0,
        depositAmount: Number(r.depositAmount) || 0,
      };
    });

    const payload: UpdateLevelBenefitsPayload = data.requireFullOnlinePayment
      ? {
          description: trimmed === "" ? null : trimmed,
          requireFullOnlinePayment: true,
          depositFreeOrderAmount: null,
          maxDepositOrderAmount: null,
          ranges: [],
        }
      : {
          description: trimmed === "" ? null : trimmed,
          requireFullOnlinePayment: false,
          depositFreeOrderAmount: data.depositFreeOrderAmount ?? null,
          maxDepositOrderAmount: data.maxDepositOrderAmount ?? null,
          ranges: processedRanges,
        };

    const error = await onSave(level.id, payload);
    if (error) setServerError(error);
    else handleClose();
  };

  return (
    <SharedModal
      isOpen={level !== null}
      onClose={handleClose}
      title={level ? `${level.name} (Rank ${level.rank}) benefits & deposit policy` : undefined}
      maxWidthClass="max-w-2xl"
    >
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-5">
        {level && <RecordId id={level.id} variant="full" />}
        <FormField
          label="Benefits description"
          htmlFor="level-benefits"
          error={errors.description?.message}
          hint="Customers see this text on their Trust Score page. Use a new line for each benefit."
        >
          <textarea
            id="level-benefits"
            rows={4}
            {...register("description")}
            className={`${inputClass(!!errors.description)} resize-none`}
            placeholder="Describe what customers on this level get"
          />
        </FormField>

        <div className="space-y-4 rounded-xl border border-border-default bg-neutral-50/70 p-4">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h4 className="text-sm font-bold text-text-primary">Pay at shop & deposit policy</h4>
              <p className="mt-0.5 text-xs text-text-secondary">
                Changes apply only to future orders and do not affect existing orders.
              </p>
              {(precedingLevel || succeedingLevel) && (
                <div className="mt-2 space-y-1 rounded-lg border border-neutral-200 bg-white p-2.5 text-xs text-text-secondary">
                  {precedingLevel && (
                    <p>
                      <span className="font-semibold text-text-primary">
                        Lower level ({precedingLevel.name} - Rank {precedingLevel.rank}):
                      </span>{" "}
                      {precedingLevel.depositPolicy?.requireFullOnlinePayment ? (
                        "100% online payment"
                      ) : (
                        <>
                          Free deposit &le;{" "}
                          <strong>
                            {formatCurrency(precedingLevel.depositPolicy?.depositFreeOrderAmount ?? 0)}
                          </strong>
                          {" • "}
                          Max Pay at shop:{" "}
                          <strong>
                            {formatCurrency(precedingLevel.depositPolicy?.maxDepositOrderAmount ?? 0)}
                          </strong>
                        </>
                      )}
                    </p>
                  )}
                  {succeedingLevel && (
                    <p>
                      <span className="font-semibold text-text-primary">
                        Higher level ({succeedingLevel.name} - Rank {succeedingLevel.rank}):
                      </span>{" "}
                      {succeedingLevel.depositPolicy?.requireFullOnlinePayment ? (
                        "100% online payment"
                      ) : (
                        <>
                          Free deposit &le;{" "}
                          <strong>
                            {formatCurrency(succeedingLevel.depositPolicy?.depositFreeOrderAmount ?? 0)}
                          </strong>
                          {" • "}
                          Max Pay at shop:{" "}
                          <strong>
                            {formatCurrency(succeedingLevel.depositPolicy?.maxDepositOrderAmount ?? 0)}
                          </strong>
                        </>
                      )}
                    </p>
                  )}
                </div>
              )}
            </div>
          </div>

          <div>
            <label className="flex cursor-pointer items-center gap-3 rounded-lg border border-border-default bg-white p-3 text-sm font-semibold text-text-primary">
              <input
                type="checkbox"
                checked={requireFullOnline}
                onChange={(e) => handleToggleFullOnline(e.target.checked)}
                className="size-4 rounded border-border-default text-primary-600 focus:ring-primary-500"
              />
              <span>Require 100% online payment for all orders (Disable Pay at shop)</span>
            </label>
            {errors.requireFullOnlinePayment && (
              <p className="mt-1 text-xs font-medium text-danger">
                {errors.requireFullOnlinePayment.message}
              </p>
            )}
          </div>

          {!requireFullOnline && (
            <div className="space-y-4 pt-1">
              <div className="grid gap-4 sm:grid-cols-2">
                <FormField
                  label="Deposit-free order threshold (VND)"
                  htmlFor="policy-free-amount"
                  error={errors.depositFreeOrderAmount?.message}
                  hint="Orders <= this amount need 0 VND deposit."
                >
                  <input
                    id="policy-free-amount"
                    type="number"
                    step="1000"
                    inputMode="numeric"
                    placeholder="e.g. 100000"
                    {...register("depositFreeOrderAmount", {
                      setValueAs: (v) => (v === "" || v === null || v === undefined ? null : Number(v)),
                      onChange: (e) => {
                        const val = e.target.value === "" ? undefined : Number(e.target.value);
                        handleFreeAmountChange(val);
                      },
                    })}
                    className={inputClass(!!errors.depositFreeOrderAmount)}
                  />
                </FormField>

                <FormField
                  label="Max Pay at shop order amount (VND)"
                  htmlFor="policy-max-amount"
                  error={errors.maxDepositOrderAmount?.message}
                  hint="Orders > this amount must pay 100% online."
                >
                  <input
                    id="policy-max-amount"
                    type="number"
                    step="1000"
                    inputMode="numeric"
                    placeholder="e.g. 1000000"
                    {...register("maxDepositOrderAmount", {
                      setValueAs: (v) => (v === "" || v === null || v === undefined ? null : Number(v)),
                      onChange: (e) => {
                        const val = e.target.value === "" ? undefined : Number(e.target.value);
                        handleMaxAmountChange(val);
                      },
                    })}
                    className={inputClass(!!errors.maxDepositOrderAmount)}
                  />
                </FormField>
              </div>

              <div className="space-y-2.5">
                <div className="flex items-center justify-between gap-2">
                  <div>
                    <p className="text-sm font-semibold text-text-primary">
                      Fixed Pay at shop deposit tiers
                    </p>
                    <p className="text-xs text-text-secondary">
                      Contiguous intervals (From, Up to]. Deposit cannot exceed the starting amount.
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={handleAddRange}
                    className="inline-flex items-center gap-1.5 rounded-md border border-primary-200 bg-primary-50 px-3 py-1.5 text-xs font-semibold text-primary-700 transition-colors hover:bg-primary-100"
                  >
                    <Plus className="size-3.5" />
                    Add tier
                  </button>
                </div>

                {errors.ranges?.root?.message && (
                  <p className="text-xs font-medium text-danger">{errors.ranges.root.message}</p>
                )}

                <div className="space-y-2.5">
                  {fields.map((field, index) => {
                    const rangeErr = errors.ranges?.[index];
                    const currentRange = watchedRanges?.[index];
                    const isLastTier = index === fields.length - 1;

                    return (
                      <div
                        key={field.id}
                        className="rounded-lg border border-border-default bg-white p-3 shadow-2xs"
                      >
                        <div className="mb-2 flex items-center justify-between">
                          <span className="text-xs font-bold text-text-secondary">
                            Tier #{index + 1}
                            {currentRange &&
                              Number.isFinite(currentRange.fromOrderAmount) &&
                              Number.isFinite(currentRange.toOrderAmount) && (
                                <span className="ml-2 font-normal text-text-muted">
                                  ({formatCurrency(Number(currentRange.fromOrderAmount))} &lt; Order &le;{" "}
                                  {formatCurrency(Number(currentRange.toOrderAmount))})
                                </span>
                              )}
                          </span>
                          {fields.length > 1 && (
                            <button
                              type="button"
                              onClick={() => handleRemoveRange(index)}
                              className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-danger hover:bg-danger/10"
                              title="Remove tier"
                            >
                              <Trash2 className="size-3.5" />
                              Remove
                            </button>
                          )}
                        </div>

                        <div className="grid gap-3 sm:grid-cols-3">
                          {/* From: locked for all tiers */}
                          <div className="cursor-not-allowed">
                            <div className="mb-1 flex items-center justify-between">
                              <label className="block text-xs font-medium text-text-secondary cursor-not-allowed">
                                From (&gt; VND)
                              </label>
                              <span className="text-[10px] text-text-muted font-normal">(Locked)</span>
                            </div>
                            <input
                              type="number"
                              step="1000"
                              readOnly
                              tabIndex={-1}
                              {...register(`ranges.${index}.fromOrderAmount`, {
                                setValueAs: (v) =>
                                  v === "" || v === null || v === undefined ? undefined : Number(v),
                              })}
                              className={`${inputClass(!!rangeErr?.fromOrderAmount)} bg-neutral-100 text-text-secondary cursor-not-allowed pointer-events-none select-none`}
                              placeholder={
                                index === 0
                                  ? "Synced from Free threshold"
                                  : `Synced from Tier #${index} Up to`
                              }
                            />
                            {rangeErr?.fromOrderAmount && (
                              <p className="mt-1 text-xs text-danger">
                                {rangeErr.fromOrderAmount.message}
                              </p>
                            )}
                          </div>

                          {/* Up to: locked for the last tier, editable otherwise */}
                          <div className={isLastTier ? "cursor-not-allowed" : ""}>
                            <div className="mb-1 flex items-center justify-between">
                              <label
                                className={`block text-xs font-medium text-text-secondary ${
                                  isLastTier ? "cursor-not-allowed" : ""
                                }`}
                              >
                                Up to (&le; VND)
                              </label>
                              {isLastTier && (
                                <span className="text-[10px] text-text-muted font-normal">(Locked)</span>
                              )}
                            </div>
                            <input
                              type="number"
                              step="1000"
                              readOnly={isLastTier}
                              tabIndex={isLastTier ? -1 : 0}
                              {...register(`ranges.${index}.toOrderAmount`, {
                                setValueAs: (v) =>
                                  v === "" || v === null || v === undefined ? undefined : Number(v),
                                onChange: (e) => {
                                  if (!isLastTier) {
                                    const val = e.target.value === "" ? undefined : Number(e.target.value);
                                    handleRangeToChange(index, val);
                                  }
                                },
                              })}
                              className={`${inputClass(!!rangeErr?.toOrderAmount)} ${
                                isLastTier
                                  ? "bg-neutral-100 text-text-secondary cursor-not-allowed pointer-events-none select-none"
                                  : ""
                              }`}
                              placeholder={isLastTier ? "Synced from Max Pay at shop" : "e.g. 300000"}
                            />
                            {rangeErr?.toOrderAmount && (
                              <p className="mt-1 text-xs text-danger">
                                {rangeErr.toOrderAmount.message}
                              </p>
                            )}
                          </div>

                          {/* Fixed deposit: always editable */}
                          <div>
                            <label className="mb-1 block text-xs font-medium text-text-secondary">
                              Fixed deposit (VND)
                            </label>
                            <input
                              type="number"
                              step="1000"
                              {...register(`ranges.${index}.depositAmount`, {
                                setValueAs: (v) =>
                                  v === "" || v === null || v === undefined ? undefined : Number(v),
                                onChange: (e) => {
                                  const val = e.target.value === "" ? undefined : Number(e.target.value);
                                  setValue(`ranges.${index}.depositAmount`, val as number, { shouldValidate: true });
                                  trigger(["depositFreeOrderAmount", "maxDepositOrderAmount", "ranges"]);
                                },
                              })}
                              className={inputClass(!!rangeErr?.depositAmount)}
                              placeholder="e.g. 50000"
                            />
                            {rangeErr?.depositAmount && (
                              <p className="mt-1 text-xs text-danger">
                                {rangeErr.depositAmount.message}
                              </p>
                            )}
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          )}
        </div>

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