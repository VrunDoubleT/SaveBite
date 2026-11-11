import { useEffect, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Plus, Trash2 } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type {
  AdminTrustLevel,
  CreateTrustLevelPayload,
  TrustLevelDepositRangeInput,
} from "../types/adminTrust.types";
import { createLevelSchema, type CreateLevelFormData } from "../schemas/trust.schema";
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

function buildDefaultRanges(
  freeAmount: number,
  maxAmount: number,
): TrustLevelDepositRangeInput[] {
  const safeFree = freeAmount > 0 ? freeAmount : 50000;
  const safeMax = maxAmount > safeFree ? maxAmount : safeFree * 10;
  const mid1 = Math.max(safeFree + 1000, Math.round((safeMax * 0.4) / 1000) * 1000);
  const mid2 = Math.max(mid1 + 1000, Math.round((safeMax * 0.8) / 1000) * 1000);

  return [
    {
      fromOrderAmount: safeFree,
      toOrderAmount: mid1,
      depositAmount: Math.max(1000, Math.min(safeFree, Math.round((safeMax * 0.05) / 1000) * 1000)),
    },
    {
      fromOrderAmount: mid1,
      toOrderAmount: mid2,
      depositAmount: Math.max(1000, Math.min(mid1, Math.round((safeMax * 0.2) / 1000) * 1000)),
    },
    {
      fromOrderAmount: mid2,
      toOrderAmount: safeMax,
      depositAmount: Math.max(1000, Math.min(mid2, Math.round((safeMax * 0.3) / 1000) * 1000)),
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

  const {
    register,
    control,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<CreateLevelFormData>({
    resolver: zodResolver(createLevelSchema),
    defaultValues: {
      name: "",
      minScore: 0,
      minCompletedOrders: 0,
      minTotalOrderValue: 0,
      description: "",
      requireFullOnlinePayment: false,
      depositFreeOrderAmount: 100000,
      maxDepositOrderAmount: 1000000,
      ranges: buildDefaultRanges(100000, 1000000),
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

    const freeAmount = 100000;
    const maxAmount = 1000000;

    reset({
      name: "",
      minScore: suggestedScore,
      minCompletedOrders: suggestedOrders,
      minTotalOrderValue: suggestedValue,
      description: "",
      requireFullOnlinePayment: false,
      depositFreeOrderAmount: freeAmount,
      maxDepositOrderAmount: maxAmount,
      ranges: buildDefaultRanges(freeAmount, maxAmount),
    });
  }, [isOpen, highestLevel, reset]);

  const handleClose = () => {
    setServerError(null);
    onClose();
  };

  const handleToggleFullOnline = (checked: boolean) => {
    setValue("requireFullOnlinePayment", checked, { shouldValidate: true });
    if (!checked && fields.length === 0) {
      const defaultFree = 100000;
      const defaultMax = 1000000;
      setValue("depositFreeOrderAmount", defaultFree, { shouldValidate: true });
      setValue("maxDepositOrderAmount", defaultMax, { shouldValidate: true });
      replace(buildDefaultRanges(defaultFree, defaultMax));
    }
  };

  const handleFreeAmountChange = (value: number) => {
    setValue("depositFreeOrderAmount", value);
    if (fields.length > 0 && Number.isFinite(value) && value > 0) {
      setValue("ranges.0.fromOrderAmount", value, { shouldValidate: true });
    }
  };

  const handleRangeToChange = (index: number, value: number) => {
    setValue(`ranges.${index}.toOrderAmount`, value);
    if (!Number.isFinite(value) || value <= 0) return;

    if (index + 1 < fields.length) {
      setValue(`ranges.${index + 1}.fromOrderAmount`, value, { shouldValidate: true });
    } else {
      setValue("maxDepositOrderAmount", value, { shouldValidate: true });
    }
  };

  const handleMaxAmountChange = (value: number) => {
    setValue("maxDepositOrderAmount", value);
    if (fields.length > 0 && Number.isFinite(value) && value > 0) {
      setValue(`ranges.${fields.length - 1}.toOrderAmount`, value, { shouldValidate: true });
    }
  };

  const handleAddRange = () => {
    const lastRange = watchedRanges[watchedRanges.length - 1];
    const nextFrom = lastRange?.toOrderAmount ?? (watch("depositFreeOrderAmount") || 100000);
    const nextTo = nextFrom + 100000;
    const suggestedDeposit = Math.max(
      1000,
      Math.min(nextFrom, Math.round((nextFrom * 0.3) / 1000) * 1000),
    );

    append({
      fromOrderAmount: nextFrom,
      toOrderAmount: nextTo,
      depositAmount: suggestedDeposit,
    });
    setValue("maxDepositOrderAmount", nextTo, { shouldValidate: true });
  };

  const handleRemoveRange = (index: number) => {
    if (fields.length <= 1) return;
    const currentRanges = [...watchedRanges];
    currentRanges.splice(index, 1);

    const freeAmount = watch("depositFreeOrderAmount") || 100000;
    for (let i = 0; i < currentRanges.length; i++) {
      currentRanges[i].fromOrderAmount = i === 0 ? freeAmount : currentRanges[i - 1].toOrderAmount;
      if (currentRanges[i].toOrderAmount <= currentRanges[i].fromOrderAmount) {
        currentRanges[i].toOrderAmount = currentRanges[i].fromOrderAmount + 50000;
      }
      if (currentRanges[i].depositAmount > currentRanges[i].fromOrderAmount) {
        currentRanges[i].depositAmount = currentRanges[i].fromOrderAmount;
      }
    }

    replace(currentRanges);
    setValue("maxDepositOrderAmount", currentRanges[currentRanges.length - 1].toOrderAmount, {
      shouldValidate: true,
    });
  };

  const onSubmit = async (data: CreateLevelFormData) => {
    setServerError(null);
    const trimmedDesc = data.description.trim();

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
            ranges: data.ranges.map((r) => ({
              fromOrderAmount: r.fromOrderAmount,
              toOrderAmount: r.toOrderAmount,
              depositAmount: r.depositAmount,
            })),
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
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
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
            {...register("name")}
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
                inputMode="numeric"
                {...register("minScore", { valueAsNumber: true })}
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
                inputMode="numeric"
                {...register("minCompletedOrders", { valueAsNumber: true })}
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
                {...register("minTotalOrderValue", { valueAsNumber: true })}
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
            {...register("description")}
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
          </div>

          <label className="flex cursor-pointer items-center gap-3 rounded-lg border border-border-default bg-white p-3 text-sm font-semibold text-text-primary">
            <input
              type="checkbox"
              checked={requireFullOnline}
              onChange={(e) => handleToggleFullOnline(e.target.checked)}
              className="size-4 rounded border-border-default text-primary-600 focus:ring-primary-500"
            />
            <span>Require 100% online payment for all orders (Disable Pay at shop)</span>
          </label>

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
                    {...register("depositFreeOrderAmount", {
                      valueAsNumber: true,
                      onChange: (e) => handleFreeAmountChange(Number(e.target.value)),
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
                    {...register("maxDepositOrderAmount", {
                      valueAsNumber: true,
                      onChange: (e) => handleMaxAmountChange(Number(e.target.value)),
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
                    const currentRange = watchedRanges[index];
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
                                  ({formatCurrency(currentRange.fromOrderAmount)} &lt; Order &le;{" "}
                                  {formatCurrency(currentRange.toOrderAmount)})
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
                          <div>
                            <label className="mb-1 block text-xs font-medium text-text-secondary">
                              From (&gt; VND)
                            </label>
                            <input
                              type="number"
                              step="1000"
                              readOnly={index > 0}
                              {...register(`ranges.${index}.fromOrderAmount`, {
                                valueAsNumber: true,
                                onChange: (e) => {
                                  if (index === 0) {
                                    setValue("depositFreeOrderAmount", Number(e.target.value), {
                                      shouldValidate: true,
                                    });
                                  }
                                },
                              })}
                              className={`${inputClass(!!rangeErr?.fromOrderAmount)} ${
                                index > 0 ? "bg-neutral-100 text-text-secondary" : ""
                              }`}
                            />
                            {rangeErr?.fromOrderAmount && (
                              <p className="mt-1 text-xs text-danger">
                                {rangeErr.fromOrderAmount.message}
                              </p>
                            )}
                          </div>

                          <div>
                            <label className="mb-1 block text-xs font-medium text-text-secondary">
                              Up to (&le; VND)
                            </label>
                            <input
                              type="number"
                              step="1000"
                              {...register(`ranges.${index}.toOrderAmount`, {
                                valueAsNumber: true,
                                onChange: (e) => handleRangeToChange(index, Number(e.target.value)),
                              })}
                              className={inputClass(!!rangeErr?.toOrderAmount)}
                            />
                            {rangeErr?.toOrderAmount && (
                              <p className="mt-1 text-xs text-danger">
                                {rangeErr.toOrderAmount.message}
                              </p>
                            )}
                          </div>

                          <div>
                            <label className="mb-1 block text-xs font-medium text-text-secondary">
                              Fixed deposit (VND)
                            </label>
                            <input
                              type="number"
                              step="1000"
                              {...register(`ranges.${index}.depositAmount`, {
                                valueAsNumber: true,
                              })}
                              className={inputClass(!!rangeErr?.depositAmount)}
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