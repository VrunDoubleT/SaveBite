import { useEffect, useMemo, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { Plus, Trash2 } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type {
  AdminTrustLevel,
  CreateTrustLevelPayload,
  TrustLevelDepositRangeInput,
} from "../types/adminTrust.types";
import { buildCreateLevelResolver, type CreateLevelFormData } from "../schemas/trust.schema";
import { formatCurrency } from "../utils/format";
import { inputClass } from "../utils/formStyles";
import { FormField, ModalActions } from "./FormField";

interface CreateLevelModalProps {
  isOpen: boolean;
  existingLevels: AdminTrustLevel[];
  onClose: () => void;
  /** Resolves with an error message, or null on success. */
  onCreate: (payload: CreateTrustLevelPayload) => Promise<string | null>;
}

function buildInitialRanges(
  freeAmount?: number | null,
  maxAmount?: number | null,
): TrustLevelDepositRangeInput[] {
  return [
    {
      fromOrderAmount:
        typeof freeAmount === "number" && !Number.isNaN(freeAmount) && freeAmount > 0
          ? freeAmount
          : (undefined as unknown as number),
      toOrderAmount: undefined as unknown as number,
      depositAmount: undefined as unknown as number,
    },
    {
      fromOrderAmount: undefined as unknown as number,
      toOrderAmount: undefined as unknown as number,
      depositAmount: undefined as unknown as number,
    },
    {
      fromOrderAmount: undefined as unknown as number,
      toOrderAmount:
        typeof maxAmount === "number" && !Number.isNaN(maxAmount) && maxAmount > 0
          ? maxAmount
          : (undefined as unknown as number),
      depositAmount: undefined as unknown as number,
    },
  ];
}

// UC-153: Add New Level (creates Name + Requirement + Deposit Policy in one transaction).
export function CreateLevelModal({
  isOpen,
  existingLevels,
  onClose,
  onCreate,
}: CreateLevelModalProps) {
  const [serverError, setServerError] = useState<string | null>(null);

  const highestLevel =
    existingLevels.length > 0
      ? [...existingLevels].sort((a, b) => b.rank - a.rank)[0]
      : null;
  const nextRank = (highestLevel?.rank ?? 0) + 1;

  const resolver = useMemo(
    () => buildCreateLevelResolver(highestLevel, existingLevels.map((l) => l.name)),
    [highestLevel, existingLevels],
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
  } = useForm<CreateLevelFormData>({
    resolver,
    mode: "onChange",
    reValidateMode: "onChange",
    defaultValues: {
      name: "",
      minScore: 0,
      minCompletedOrders: 0,
      minTotalOrderValue: 0,
      description: "",
      requireFullOnlinePayment: false,
      depositFreeOrderAmount: undefined as unknown as number,
      maxDepositOrderAmount: undefined as unknown as number,
      ranges: buildInitialRanges(),
    },
  });

  const { fields, append, replace } = useFieldArray({
    control,
    name: "ranges",
  });

  const requireFullOnline = watch("requireFullOnlinePayment");
  const watchedRanges = watch("ranges");

  useEffect(() => {
    if (!isOpen) return;

    const prevScore = highestLevel?.requirement?.minScore ?? 0;
    const prevOrders = highestLevel?.requirement?.minCompletedOrders ?? 0;
    const prevValue = highestLevel?.requirement?.minTotalOrderValue ?? 0;

    const suggestedScore = highestLevel ? prevScore + 5 : 0;
    const suggestedOrders = highestLevel ? prevOrders + 10 : 0;
    const suggestedValue = highestLevel ? prevValue + 500000 : 0;

    reset({
      name: "",
      minScore: suggestedScore,
      minCompletedOrders: suggestedOrders,
      minTotalOrderValue: suggestedValue,
      description: "",
      requireFullOnlinePayment: false,
      depositFreeOrderAmount: undefined as unknown as number,
      maxDepositOrderAmount: undefined as unknown as number,
      ranges: buildInitialRanges(),
    });
    replace(buildInitialRanges());
  }, [isOpen, highestLevel, reset, replace]);

  const handleClose = () => {
    setServerError(null);
    const prevScore = highestLevel?.requirement?.minScore ?? 0;
    const prevOrders = highestLevel?.requirement?.minCompletedOrders ?? 0;
    const prevValue = highestLevel?.requirement?.minTotalOrderValue ?? 0;

    reset({
      name: "",
      minScore: highestLevel ? prevScore + 5 : 0,
      minCompletedOrders: highestLevel ? prevOrders + 10 : 0,
      minTotalOrderValue: highestLevel ? prevValue + 500000 : 0,
      description: "",
      requireFullOnlinePayment: false,
      depositFreeOrderAmount: undefined as unknown as number,
      maxDepositOrderAmount: undefined as unknown as number,
      ranges: buildInitialRanges(),
    });
    replace(buildInitialRanges());
    onClose();
  };

  const handleToggleFullOnline = (checked: boolean) => {
    setValue("requireFullOnlinePayment", checked, { shouldValidate: true });
    if (checked) {
      clearErrors(["depositFreeOrderAmount", "maxDepositOrderAmount", "ranges"]);
    } else if (fields.length === 0) {
      setValue("depositFreeOrderAmount", undefined as unknown as number);
      setValue("maxDepositOrderAmount", undefined as unknown as number);
      replace(buildInitialRanges());
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

    // The previous last tier was locked to maxDepositOrderAmount.
    // Since it is now an intermediate tier, clear its toOrderAmount so the user can define this boundary.
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

    // Re-chain bounds cleanly without arbitrary auto-increments
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

  const onSubmit = async (data: CreateLevelFormData) => {
    setServerError(null);
    const trimmedDesc = data.description.trim();

    // Ensure strict boundary alignment before sending
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

    const payload: CreateTrustLevelPayload = {
      name: data.name.trim(),
      requirement: {
        minScore: data.minScore,
        minCompletedOrders: data.minCompletedOrders,
        minTotalOrderValue: data.minTotalOrderValue,
      },
      benefits: data.requireFullOnlinePayment
        ? {
            description: trimmedDesc === "" ? null : trimmedDesc,
            requireFullOnlinePayment: true,
            depositFreeOrderAmount: null,
            maxDepositOrderAmount: null,
            ranges: [],
          }
        : {
            description: trimmedDesc === "" ? null : trimmedDesc,
            requireFullOnlinePayment: false,
            depositFreeOrderAmount: data.depositFreeOrderAmount ?? null,
            maxDepositOrderAmount: data.maxDepositOrderAmount ?? null,
            ranges: processedRanges,
          },
    };

    const error = await onCreate(payload);
    if (error) setServerError(error);
    else handleClose();
  };

  return (
    <SharedModal
      isOpen={isOpen}
      onClose={handleClose}
      title={`Add new Trust Level (Rank ${nextRank})`}
      maxWidthClass="max-w-2xl"
    >
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-5">
        <div className="rounded-lg border border-primary-100 bg-primary-50/50 p-3 text-xs text-text-secondary">
          This level will be created as <strong>Rank {nextRank}</strong> (Active).
          {highestLevel && (
            <span>
              {" "}
              Thresholds must exceed <strong>{highestLevel.name}</strong> (Score{" "}
              {highestLevel.requirement?.minScore ?? 0}, Orders{" "}
              {highestLevel.requirement?.minCompletedOrders ?? 0}, Value{" "}
              {formatCurrency(highestLevel.requirement?.minTotalOrderValue ?? 0)}).
            </span>
          )}
        </div>

        <FormField
          label="Level name"
          htmlFor="new-level-name"
          error={errors.name?.message}
          hint="1–100 characters, unique across all levels."
        >
          <input
            id="new-level-name"
            type="text"
            maxLength={100}
            {...register("name", {
              onChange: (e) => {
                setValue("name", e.target.value, { shouldValidate: true });
                trigger("name");
              },
            })}
            className={inputClass(!!errors.name)}
            placeholder="e.g. Master / Legend"
          />
        </FormField>

        <div className="space-y-3 rounded-xl border border-border-default bg-neutral-50/70 p-4">
          <h4 className="text-sm font-bold text-text-primary">Level requirements</h4>
          <div className="grid gap-4 sm:grid-cols-3">
            <FormField
              label="Min Trust Score"
              htmlFor="new-req-score"
              error={errors.minScore?.message}
            >
              <input
                id="new-req-score"
                type="number"
                step="1"
                inputMode="numeric"
                {...register("minScore", {
                  setValueAs: (v) =>
                    v === "" || v === null || v === undefined ? undefined : Number(v),
                  onChange: (e) => {
                    const val = e.target.value === "" ? undefined : Number(e.target.value);
                    setValue("minScore", val as number, { shouldValidate: true });
                    trigger(["minScore", "minCompletedOrders", "minTotalOrderValue"]);
                  },
                })}
                className={inputClass(!!errors.minScore)}
              />
            </FormField>

            <FormField
              label="Min completed orders"
              htmlFor="new-req-orders"
              error={errors.minCompletedOrders?.message}
            >
              <input
                id="new-req-orders"
                type="number"
                step="1"
                {...register("minCompletedOrders", {
                  setValueAs: (v) =>
                    v === "" || v === null || v === undefined ? undefined : Number(v),
                  onChange: (e) => {
                    const val = e.target.value === "" ? undefined : Number(e.target.value);
                    setValue("minCompletedOrders", val as number, { shouldValidate: true });
                    trigger(["minScore", "minCompletedOrders", "minTotalOrderValue"]);
                  },
                })}
                className={inputClass(!!errors.minCompletedOrders)}
              />
            </FormField>

            <FormField
              label="Min order value (VND)"
              htmlFor="new-req-value"
              error={errors.minTotalOrderValue?.message}
            >
              <input
                id="new-req-value"
                type="number"
                step="1000"
                inputMode="numeric"
                {...register("minTotalOrderValue", {
                  setValueAs: (v) =>
                    v === "" || v === null || v === undefined ? undefined : Number(v),
                  onChange: (e) => {
                    const val = e.target.value === "" ? undefined : Number(e.target.value);
                    setValue("minTotalOrderValue", val as number, { shouldValidate: true });
                    trigger(["minScore", "minCompletedOrders", "minTotalOrderValue"]);
                  },
                })}
                className={inputClass(!!errors.minTotalOrderValue)}
              />
            </FormField>
          </div>
        </div>

        <FormField
          label="Benefits description"
          htmlFor="new-level-benefits"
          error={errors.description?.message}
          hint="Optional description shown to customers on their Trust Score page."
        >
          <textarea
            id="new-level-benefits"
            rows={3}
            {...register("description", {
              onChange: () => {
                trigger("description");
              },
            })}
            className={`${inputClass(!!errors.description)} resize-none`}
            placeholder="Describe what customers on this level get"
          />
        </FormField>

        <div className="space-y-4 rounded-xl border border-border-default bg-neutral-50/70 p-4">
          <div>
            <h4 className="text-sm font-bold text-text-primary">Pay at shop & deposit policy</h4>
            <p className="mt-0.5 text-xs text-text-secondary">
              Configure Pay at shop eligibility and fixed deposit tiers for this level.
            </p>
            {highestLevel && (
              <div className="mt-2 rounded-lg border border-neutral-200 bg-white p-2.5 text-xs text-text-secondary">
                <span className="font-semibold text-text-primary">
                  Previous level baseline ({highestLevel.name} - Rank {highestLevel.rank}):
                </span>{" "}
                {highestLevel.depositPolicy?.requireFullOnlinePayment ? (
                  <span>100% online payment required (Pay at shop disabled).</span>
                ) : (
                  <span>
                    Free deposit &le;{" "}
                    <strong>
                      {formatCurrency(highestLevel.depositPolicy?.depositFreeOrderAmount ?? 0)}
                    </strong>
                    {" • "}
                    Max Pay at shop:{" "}
                    <strong>
                      {formatCurrency(highestLevel.depositPolicy?.maxDepositOrderAmount ?? 0)}
                    </strong>
                    {" • "}
                    New level must offer equal or better benefits.
                  </span>
                )}
              </div>
            )}
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
                  htmlFor="new-policy-free-amount"
                  error={errors.depositFreeOrderAmount?.message}
                  hint="Orders <= this amount need 0 VND deposit."
                >
                  <input
                    id="new-policy-free-amount"
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
                  htmlFor="new-policy-max-amount"
                  error={errors.maxDepositOrderAmount?.message}
                  hint="Orders > this amount must pay 100% online."
                >
                  <input
                    id="new-policy-max-amount"
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
          submitLabel="Create level"
          serverError={serverError}
        />
      </form>
    </SharedModal>
  );
}