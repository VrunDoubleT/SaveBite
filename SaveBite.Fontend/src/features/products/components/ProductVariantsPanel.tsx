import { isUploadCancelled } from "@/shared/uploads/uploadErrors";
import { toast } from "@/shared/stores/toastStore";
import { UploadDraftChoice } from "@/shared/uploads/UploadDraftChoice";
import { UploadTaskGuard } from "@/shared/uploads/UploadTaskGuard";
import { useEffect, useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { useLocation } from "react-router-dom";
import { FieldError } from "./FieldError";
import { Pencil, Plus, X } from "lucide-react";
import { MIN_AMOUNT_VND } from "@/shared/config/money";
import { productApi } from "../api/productApi";
import type { DraftAttribute, PriceMap, Product, ProductVariant } from "../types/product.types";
import {
  appendAttributes,
  appendPrices,
  combinationKey,
  combinations,
  describeError,
  fileError,
  money,
  validateAttributes,
  validatePrices,
  validateProductPrice,
  PRODUCT_PRICE_STEP,
} from "../utils/productForm";
import { AttributesEditor } from "./AttributesEditor";
import { VariantPrices } from "./VariantPrices";
import {
  Alert,
  inputClass,
  Modal,
  Panel,
  primaryButton,
  secondaryButton,
  StatusBadge,
} from "./ProductUi";
import { useAuthStore } from "@/shared/stores/authStore";
import { getUploadDraft, removeUploadDraft } from "@/shared/uploads/uploadDraftStore";
import { startOrResumeUploadDraft, uploadReferences } from "@/shared/uploads/uploadDrafts";
import { getApiErrorMessage } from "@/shared/api";

export function ProductVariantsPanel({
  product,
  onSaved,
}: {
  product: Product;
  onSaved: (product: Product) => void;
}) {
  const location = useLocation();
  const userId = useAuthStore((state) => state.user?.id);
  const [adding, setAdding] = useState(false);
  const [resumeError, setResumeError] = useState("");
  useEffect(() => {
    const draftId = (location.state as { uploadDraftId?: string } | null)?.uploadDraftId;
    if (!draftId) return;
    let active = true;
    void getUploadDraft(draftId)
      .then((draft) => {
        if (!active) return;
        if (!draft || draft.userId !== userId) {
          setResumeError("This saved product work is unavailable or has expired.");
          return;
        }
        const form = draft.formData as { operation?: string };
        if (form.operation === "add-values") setAdding(true);
      })
      .catch((error) => setResumeError(getApiErrorMessage(error)));
    return () => {
      active = false;
    };
  }, [location.state, userId]);
  const [editingVariant, setEditingVariant] = useState<ProductVariant | null>(null);
  const valueNames = new Map(
    product.attributes.flatMap((a) => a.values.map((v) => [v.id, v.value] as const)),
  );
  return (
    <div className="space-y-5">
      {resumeError && <Alert>{resumeError}</Alert>}
      <Panel
        title={`Product variants · ${product.variants.length}/100`}
        description="Manage prices, status, and attributes for your variants. Add values to create new variants."
        aside={
          !adding && (
            <button
              className={primaryButton}
              disabled={product.variants.length >= 100 || !product.attributes.length}
              onClick={() => setAdding(true)}
            >
              <Plus className="size-4" />
              Add value
            </button>
          )
        }
      >
        <div className="mb-5 flex flex-wrap gap-2">
          {product.attributes.map((a) => (
            <div key={a.id} className="rounded-lg bg-neutral-50 px-3 py-2 text-xs">
              <span className="font-semibold">{a.name}:</span>{" "}
              <span className="text-neutral-600">{a.values.map((v) => v.value).join(", ")}</span>
            </div>
          ))}
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[580px] text-left text-sm">
            <thead className="border-b border-neutral-200 text-xs text-neutral-500">
              <tr>
                {["Variants", "SKU", "Original price", "Deal price", "Status"].map((label) => (
                  <th key={label} className="px-3 py-3 font-medium">
                    {label}
                  </th>
                ))}
                <th className="px-3 py-3 font-medium text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-100">
              {product.variants.map((v) => (
                <tr key={v.id}>
                  <td className="px-3 py-4 font-medium">
                    {v.valueIds.map((id) => valueNames.get(id) ?? "—").join(" / ") || "Default"}
                  </td>
                  <td className="max-w-36 px-3 py-4">
                    <span
                      className="block truncate font-mono text-xs text-neutral-500"
                      title={v.sku}
                    >
                      {v.sku ?? "—"}
                    </span>
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-neutral-500">
                    {money(v.originalPrice)}
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 font-semibold text-primary-700">
                    {money(v.dealPrice)}
                  </td>
                  <td className="px-3 py-4">
                    <StatusBadge hidden={!v.isActive} />
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-right">
                    <button
                      type="button"
                      className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:border-primary-400 hover:text-primary-700"
                      onClick={() => setEditingVariant(v)}
                    >
                      <Pencil className="size-3.5" />
                      Edit
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Panel>
      {adding && (
        <AddValuesForm
          product={product}
          onCancel={() => setAdding(false)}
          onSaved={(saved) => {
            setAdding(false);
            onSaved(saved);
          }}
        />
      )}
      {editingVariant && (
        <EditVariantModal
          product={product}
          variant={editingVariant}
          onClose={() => setEditingVariant(null)}
          onSaved={onSaved}
        />
      )}
    </div>
  );
}

function AddValuesFormContent({
  product,
  onCancel,
  onSaved,
}: {
  product: Product;
  onCancel: () => void;
  onSaved: (product: Product) => void;
}) {
  const location = useLocation();
  const userId = useAuthStore((state) => state.user?.id);
  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<{ drafts: DraftAttribute[]; prices: PriceMap }>({
    defaultValues: {
      drafts: product.attributes.map((a) => ({ id: a.id, name: a.name, values: [] })),
      prices: {},
    },
    mode: "onChange",
  });
  const drafts = useWatch({ control, name: "drafts" });
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  const [activeDraftId, setActiveDraftId] = useState<string>();
  useEffect(() => {
    const draftId = (location.state as { uploadDraftId?: string } | null)?.uploadDraftId;
    if (!draftId || !userId) return;
    let active = true;
    void getUploadDraft(draftId).then((draft) => {
      if (!active || !draft || draft.userId !== userId) return;
      const saved = draft.formData as {
        operation?: string;
        drafts?: DraftAttribute[];
        prices?: PriceMap;
      };
      if (saved.operation !== "add-values") return;
      setActiveDraftId(draft.id);
      reset({ drafts: saved.drafts ?? [], prices: saved.prices ?? {} });
    });
    return () => {
      active = false;
    };
  }, [location.state, reset, userId]);
  const merged = product.attributes.map((a) => ({
    ...a,
    values: [...a.values, ...(drafts.find((d) => d.id === a.id)?.values ?? [])],
  }));
  const generated = combinations(merged);
  const existingKeys = new Set(product.variants.map((v) => combinationKey(v.valueIds)));
  const rows = generated.rows.filter((row) => !existingKeys.has(row.key));
  return (
    <Panel
      title="Add attribute values"
      description="Existing values stay the same. Enter prices for new combinations only."
    >
      <form
        className="space-y-5"
        noValidate
        onSubmit={handleSubmit(async ({ drafts, prices }) => {
          if (busy) return;
          setBusy(true);
          setError("");
          const body = new FormData();
          const included = drafts.filter((attribute) => attribute.values.length > 0);
          appendPrices(body, rows, prices);
          try {
            const imageUploads = included.flatMap((attribute, attributeIndex) =>
              attribute.values.flatMap((value, valueIndex) =>
                value.image
                  ? [
                      {
                        field: `Attributes[${attributeIndex}].Values[${valueIndex}].Image`,
                        file: value.image,
                      },
                    ]
                  : [],
              ),
            );
            if (imageUploads.length) {
              if (!userId) throw new Error("Sign in before uploading product images.");
              const draftId = activeDraftId ?? crypto.randomUUID();
              setActiveDraftId(draftId);
              const draft = await startOrResumeUploadDraft(
                {
                  draftId,
                  userId,
                  purpose: "product",
                  targetId: product.id,
                  title: `Add values to ${product.name}`,
                  path: `${location.pathname}?tab=variants`,
                  formData: { operation: "add-values", drafts, prices },
                  files: imageUploads,
                },
                draftId,
              );
              setActiveDraftId(draft.id);
              appendAttributes(body, drafts, true, false);
              body.append("__uploadAssets", JSON.stringify(uploadReferences(draft.files)));
              onSaved(await productApi.addValues(product.id, body));
              await removeUploadDraft(draft.id);
              setActiveDraftId(undefined);
              return;
            }
            appendAttributes(body, drafts, true);
            onSaved(await productApi.addValues(product.id, body));
          } catch (err) {
            if (isUploadCancelled(err))
              toast.info(
                "Upload stopped. Your draft is saved; edit it or submit again when ready.",
              );
            else setError(describeError(err));
          } finally {
            setBusy(false);
          }
        })}
      >
        <fieldset disabled={busy} className="space-y-5">
          <Controller
            control={control}
            name="drafts"
            rules={{
              deps: "prices",
              validate: (value) =>
                !value.some((a) => a.values.length)
                  ? "Add at least one new value."
                  : validateAttributes(value, product.attributes) ||
                    fileError(
                      value.flatMap((a) => a.values.flatMap((v) => (v.image ? [v.image] : []))),
                    ) ||
                    true,
            }}
            render={({ field, fieldState }) => (
              <AttributesEditor
                attributes={field.value}
                onChange={field.onChange}
                existing={product.attributes}
                showErrors={fieldState.invalid}
                validationMessage={fieldState.error?.message}
              />
            )}
          />
          <div>
            <h3 className="mb-3 text-sm font-semibold">
              {rows.length} new variants · Total {generated.count}/100
            </h3>
            <Controller
              control={control}
              name="prices"
              rules={{
                validate: (value) =>
                  generated.count > 100
                    ? "The total number of variants exceeds 100."
                    : validatePrices(rows, value) || true,
              }}
              render={({ field, fieldState }) => (
                <VariantPrices
                  showErrors={fieldState.invalid}
                  rows={rows}
                  count={generated.count}
                  prices={field.value}
                  onChange={field.onChange}
                />
              )}
            />
            <FieldError
              id="add-values-prices-error"
              message={
                typeof errors.prices?.message === "string" ? errors.prices.message : undefined
              }
            />
          </div>
        </fieldset>
        {error && <Alert>{error}</Alert>}
        <div className="flex justify-end gap-3">
          <button type="button" disabled={busy} className={secondaryButton} onClick={onCancel}>
            <X className="size-4" />
            Cancel
          </button>
          <button className={primaryButton} disabled={busy}>
            {busy ? "Adding…" : "Save new values and variants"}
          </button>
        </div>
      </form>
    </Panel>
  );
}

function EditVariantModal({
  product,
  variant,
  onClose,
  onSaved,
}: {
  product: Product;
  variant: ProductVariant;
  onClose: () => void;
  onSaved: (product: Product) => void;
}) {
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<{ originalPrice: string; dealPrice: string; isActive: boolean }>({
    defaultValues: {
      originalPrice: String(variant.originalPrice),
      dealPrice: String(variant.dealPrice),
      isActive: variant.isActive,
    },
    mode: "onTouched",
  });
  const originalPrice = useWatch({ control, name: "originalPrice" });
  const dealPrice = useWatch({ control, name: "dealPrice" });
  const priceRules = {
    required: "Please enter a price.",
    validate: (value: string) => validateProductPrice(value) || true,
  };
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const valueNames = new Map(
    product.attributes.flatMap((a) => a.values.map((v) => [v.id, v.value] as const)),
  );
  const variantTitle =
    variant.valueIds.map((id) => valueNames.get(id) ?? "—").join(" / ") || "Default";

  const submit = async ({
    originalPrice,
    dealPrice,
    isActive,
  }: {
    originalPrice: string;
    dealPrice: string;
    isActive: boolean;
  }) => {
    if (busy) return;

    const op = Number(originalPrice);
    const dp = Number(dealPrice);

    setBusy(true);
    setError("");
    try {
      const updated = await productApi.updateVariant(product.id, variant.id, {
        originalPrice: op,
        dealPrice: dp,
        isActive,
      });
      onSaved(updated);
      onClose();
    } catch (err) {
      if (isUploadCancelled(err))
        toast.info("Upload stopped. Your draft is saved; edit it or submit again when ready.");
      else setError(describeError(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal title={`Edit variant · ${variantTitle}`} busy={busy} onClose={onClose}>
      <form noValidate onSubmit={handleSubmit(submit)} className="space-y-4">
        {variant.sku && (
          <div>
            <span className="text-xs font-medium text-neutral-500">SKU</span>
            <p className="mt-0.5 font-mono text-sm text-neutral-800">{variant.sku}</p>
          </div>
        )}

        <div>
          <label className="block text-sm font-medium text-neutral-800">
            Original price (VND) <span className="text-red-500">*</span>
            <input
              type="number"
              step={PRODUCT_PRICE_STEP}
              min={MIN_AMOUNT_VND}
              required
              className={inputClass}
              {...register("originalPrice", { ...priceRules, deps: "dealPrice" })}
              aria-invalid={!!errors.originalPrice}
              aria-describedby="variant-originalPrice-error"
            />
            <FieldError id="variant-originalPrice-error" message={errors.originalPrice?.message} />
          </label>
        </div>

        <div>
          <label className="block text-sm font-medium text-neutral-800">
            Deal price (VND) <span className="text-red-500">*</span>
            <input
              type="number"
              step={PRODUCT_PRICE_STEP}
              min={MIN_AMOUNT_VND}
              required
              className={inputClass}
              {...register("dealPrice", {
                ...priceRules,
                validate: (value) => {
                  const result = priceRules.validate(value);
                  return result !== true
                    ? result
                    : Number(value) < Number(originalPrice) ||
                        "Deal price must be less than original price.";
                },
              })}
              aria-invalid={!!errors.dealPrice}
              aria-describedby="variant-dealPrice-error"
            />
            <FieldError id="variant-dealPrice-error" message={errors.dealPrice?.message} />
          </label>
          {Number(originalPrice) > 0 &&
            Number(dealPrice) >= MIN_AMOUNT_VND &&
            Number(dealPrice) < Number(originalPrice) && (
              <p className="mt-1 text-xs font-medium text-primary-600">
                Discount:{" "}
                {Math.round(
                  ((Number(originalPrice) - Number(dealPrice)) / Number(originalPrice)) * 100,
                )}
                %
              </p>
            )}
        </div>

        <div className="flex items-center gap-3 pt-2">
          <input
            id="variant-active-toggle"
            type="checkbox"
            {...register("isActive", {
              validate: (value) =>
                value ||
                product.status !== 0 ||
                product.variants.some((v) => v.id !== variant.id && v.isActive) ||
                "An active product must have at least one active variant.",
            })}
            aria-invalid={!!errors.isActive}
            aria-describedby="variant-active-error"
            className="size-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500"
          />
          <label
            htmlFor="variant-active-toggle"
            className="cursor-pointer text-sm font-medium text-neutral-800"
          >
            Active for sale
          </label>
        </div>

        <FieldError id="variant-active-error" message={errors.isActive?.message} />
        {error && <Alert>{error}</Alert>}

        <div className="mt-6 flex justify-end gap-3">
          <button type="button" disabled={busy} className={secondaryButton} onClick={onClose}>
            Cancel
          </button>
          <button type="submit" disabled={busy} className={primaryButton}>
            {busy ? "Saving…" : "Save variant"}
          </button>
        </div>
      </form>
    </Modal>
  );
}

function AddValuesForm(props: Parameters<typeof AddValuesFormContent>[0]) {
  return (
    <UploadTaskGuard
      scope={{ purpose: "product", targetIds: [props.product.id], operation: "add-values" }}
    >
      <UploadDraftChoice
        scope={{ purpose: "product", targetIds: [props.product.id], operation: "add-values" }}
      >
        {(draftId) => <AddValuesFormContent key={draftId ?? "new"} {...props} />}
      </UploadDraftChoice>
    </UploadTaskGuard>
  );
}
