import { isUploadCancelled } from "@/shared/uploads/uploadErrors";
import { toast } from "@/shared/stores/toastStore";
import { useCallback, useEffect, useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { productInfoRules, type ProductInfoValues } from "../utils/productValidation";
import { FieldError } from "../components/FieldError";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { ArrowLeft, Check, LoaderCircle, PackagePlus } from "lucide-react";
import { productApi } from "../api/productApi";
import { useProductWorkspace } from "../hooks/useProductWorkspace";
import { Alert, inputClass, Panel, primaryButton, secondaryButton } from "../components/ProductUi";
import { GalleryEditor } from "../components/GalleryEditor";
import { AttributesEditor } from "../components/AttributesEditor";
import { VariantPrices } from "../components/VariantPrices";
import type { DraftAttribute, PriceMap } from "../types/product.types";
import { useShopSuspended } from "@/shared/hooks/useShopSuspended";
import {
  appendAttributes,
  appendPrices,
  combinations,
  describeError,
  fileError,
  validateAttributes,
  validatePrices,
} from "../utils/productForm";
import { APP_PATHS } from "@/app/router/paths";
import { useAuthStore } from "@/shared/stores/authStore";
import {
  getUploadDraft,
  isUploadDraftActive,
  releaseUploadDraft,
  removeUploadDraft,
} from "@/shared/uploads/uploadDraftStore";
import { startOrResumeUploadDraft, uploadReferences } from "@/shared/uploads/uploadDrafts";

type CreateProductFormValues = ProductInfoValues & {
  files: File[];
  attributes: DraftAttribute[];
  prices: PriceMap;
};

export function CreateProductPage() {
  const { categories, productPath } = useProductWorkspace();
  const shopSuspended = useShopSuspended();
  const navigate = useNavigate();
  const location = useLocation();
  const userId = useAuthStore((state) => state.user?.id);
  const [activeDraftId, setActiveDraftId] = useState<string>();
  const [resumeFinishedId, setResumeFinishedId] = useState<string>();
  const resumeDraftId = (location.state as { uploadDraftId?: string } | null)?.uploadDraftId;
  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateProductFormValues>({
    defaultValues: {
      name: "",
      description: "",
      categoryId: "",
      files: [],
      attributes: [],
      prices: {},
    },
    mode: "onChange",
  });
  const files = useWatch({ control, name: "files" });
  const attributes = useWatch({ control, name: "attributes" });
  const description = useWatch({ control, name: "description" });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const variants = combinations(attributes);
  useEffect(() => {
    const draftId = resumeDraftId;
    if (!draftId || !userId) return;
    let active = true;
    void (async () => {
      try {
        const draft = await getUploadDraft(draftId);
        if (!active) return;
        if (!draft || draft.userId !== userId) {
          setError("The saved product draft could not be restored.");
          setResumeFinishedId(draftId);
          return;
        }
        if (isUploadDraftActive(draft.id)) {
          setError("This product is still uploading. Follow its progress in the upload panel.");
          setResumeFinishedId(draftId);
          return;
        }
        setActiveDraftId(draft.id);
        reset(draft.formData as CreateProductFormValues);
        setResumeFinishedId(draft.id);
      } catch (resumeError) {
        if (active) {
          setError(describeError(resumeError));
          setResumeFinishedId(draftId);
        }
      }
    })();
    return () => {
      active = false;
    };
  }, [location.state, resumeDraftId, reset, userId]);
  const submit = useCallback(
    async ({
      name,
      description,
      categoryId,
      files,
      attributes,
      prices,
    }: CreateProductFormValues) => {
      if (busy || shopSuspended) return;
      setBusy(true);
      setError("");
      let queued = false;
      let submittedDraftId: string | undefined;
      try {
        if (!userId) throw new Error("Sign in before uploading product images.");
        const uploadFiles = [
          ...files.map((file) => ({ field: "Images", file })),
          ...attributes.flatMap((attribute, attributeIndex) =>
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
          ),
        ];
        const draftId = activeDraftId ?? crypto.randomUUID();
        submittedDraftId = draftId;
        setActiveDraftId(draftId);
        const draft = await startOrResumeUploadDraft(
          {
            draftId,
            userId,
            keepActiveUntilSubmitted: true,
            onQueued: () => {
              queued = true;
              reset({
                name: "",
                description: "",
                categoryId: "",
                files: [],
                attributes: [],
                prices: {},
              });
              setActiveDraftId(undefined);
              setBusy(false);
              if (resumeDraftId) navigate(location.pathname, { replace: true, state: null });
              toast.info(
                "Product creation is running in the background. You can create another product.",
              );
            },
            purpose: "product",
            title: `Create product: ${name.trim()}`,
            path: APP_PATHS.STORE_OWNER_PRODUCT_CREATE,
            formData: { name, description, categoryId, files, attributes, prices },
            files: uploadFiles,
          },
          draftId,
        );
        const body = new FormData();
        body.append("Name", name.trim());
        body.append("Description", description.trim());
        body.append("CategoryId", categoryId);
        appendAttributes(body, attributes, false, false);
        appendPrices(body, combinations(attributes).rows, prices);
        body.append("__uploadAssets", JSON.stringify(uploadReferences(draft.files)));
        await productApi.create(body);
        await removeUploadDraft(draft.id);
        toast.success(`Product "${name.trim()}" created successfully.`);
      } catch (err) {
        if (isUploadCancelled(err))
          toast.info("Upload stopped. Your draft is saved; edit it or submit again when ready.");
        else if (queued) toast.error(describeError(err));
        else setError(describeError(err));
      } finally {
        if (submittedDraftId) releaseUploadDraft(submittedDraftId);
        if (!queued) setBusy(false);
      }
    },
    [activeDraftId, busy, navigate, location.pathname, reset, resumeDraftId, shopSuspended, userId],
  );
  if (resumeDraftId && resumeFinishedId !== resumeDraftId) {
    return (
      <div
        role="status"
        className="flex min-h-64 items-center justify-center gap-3 text-sm text-neutral-500"
      >
        <LoaderCircle className="size-5 animate-spin" />
        Resuming product creation…
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <Link
        to={productPath()}
        className="inline-flex items-center gap-2 text-sm text-neutral-500 hover:text-primary-700"
      >
        <ArrowLeft className="size-4" />
        Product list
      </Link>
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Create product</h1>
        <p className="mt-2 text-sm text-text-secondary">Set up once and reuse across sales.</p>
      </div>
      <form
        noValidate
        onSubmit={handleSubmit(submit)}
        className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_280px]"
      >
        <fieldset
          disabled={busy || shopSuspended}
          className="min-w-0 space-y-5 disabled:opacity-70"
        >
          <Panel
            title="01 · Product information"
            description="Clear names and descriptions help customers choose."
          >
            <div className="space-y-4">
              <label className="block text-sm font-medium">
                Product name <span className="text-danger">*</span>
                <input
                  className={inputClass}
                  {...register("name", productInfoRules.name)}
                  aria-invalid={!!errors.name}
                  aria-describedby="create-product-name-error"
                  maxLength={200}
                  required
                  placeholder="E.g. Honey grilled chicken rice"
                />
                <FieldError id="create-product-name-error" message={errors.name?.message} />
              </label>
              <label className="block text-sm font-medium">
                Category <span className="text-danger">*</span>
                <select
                  className={inputClass}
                  {...register("categoryId", productInfoRules.categoryId)}
                  aria-invalid={!!errors.categoryId}
                  aria-describedby="create-product-categoryId-error"
                  required
                >
                  <option value="">Select a category</option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
                <FieldError
                  id="create-product-categoryId-error"
                  message={errors.categoryId?.message}
                />
              </label>
              {!categories.length && (
                <Alert>
                  No active categories are available. Contact an administrator before creating a
                  product.
                </Alert>
              )}
              <label className="block text-sm font-medium">
                Description
                <textarea
                  className={inputClass}
                  rows={4}
                  maxLength={10000}
                  {...register("description", productInfoRules.description)}
                  aria-invalid={!!errors.description}
                  aria-describedby="create-product-description-error"
                  placeholder="Describe the food, ingredients, portions, and any notes…"
                />
                <FieldError
                  id="create-product-description-error"
                  message={errors.description?.message}
                />
              </label>
              <p className="text-right text-xs text-neutral-400">
                {description.length}/10,000 characters
              </p>
            </div>
          </Panel>
          <Panel
            title="02 · Product images"
            description="Up to 10 gallery images. Attribute value images are included in the 70-image total."
          >
            <Controller
              control={control}
              name="files"
              rules={{
                validate: (value) =>
                  !value.length
                    ? "Please upload at least 1 gallery image."
                    : value.length > 10
                      ? "At most 10 gallery images are allowed."
                      : fileError([
                          ...value,
                          ...attributes.flatMap((a) =>
                            a.values.flatMap((v) => (v.image ? [v.image] : [])),
                          ),
                        ]) || true,
              }}
              render={({ field }) => (
                <GalleryEditor files={field.value} onFilesChange={field.onChange} />
              )}
            />
            <FieldError id="create-product-files-error" message={errors.files?.message} />
          </Panel>
          <Panel
            title="03 · Attributes and values"
            description="Up to 3 attributes and 20 values per attribute."
          >
            <Controller
              control={control}
              name="attributes"
              rules={{
                deps: ["prices", "files"],
                validate: (value) =>
                  validateAttributes(value) ||
                  fileError(
                    value.flatMap((a) => a.values.flatMap((v) => (v.image ? [v.image] : []))),
                  ) ||
                  true,
              }}
              render={({ field, fieldState }) => (
                <AttributesEditor
                  attributes={field.value}
                  onChange={field.onChange}
                  showErrors={fieldState.invalid}
                  validationMessage={fieldState.error?.message}
                />
              )}
            />
          </Panel>
          <Panel
            title="04 · Variant prices"
            description="Combinations are generated automatically. Enter a price for each variant before saving."
          >
            <Controller
              control={control}
              name="prices"
              rules={{
                validate: (value) =>
                  variants.count > 100
                    ? "A product cannot have more than 100 variants."
                    : validatePrices(variants.rows, value) || true,
              }}
              render={({ field, fieldState }) => (
                <VariantPrices
                  showErrors={fieldState.invalid}
                  rows={variants.rows}
                  count={variants.count}
                  prices={field.value}
                  onChange={field.onChange}
                />
              )}
            />
          </Panel>
        </fieldset>
        <aside className="space-y-4 xl:sticky xl:top-24">
          <div className="rounded-xl border border-primary-200 bg-primary-50/60 p-5">
            <PackagePlus className="mb-4 size-8 text-primary-600" />
            <h2 className="font-semibold">Your product setup</h2>
            <dl className="mt-5 space-y-3 text-sm">
              <div className="flex justify-between gap-3">
                <dt className="text-neutral-600">Product images</dt>
                <dd className="font-semibold">{files.length}/10</dd>
              </div>
              <div className="flex justify-between gap-3">
                <dt className="text-neutral-600">Attribute</dt>
                <dd className="font-semibold">{attributes.length || "1 default"}</dd>
              </div>
              <div className="flex justify-between gap-3">
                <dt className="text-neutral-600">Variants</dt>
                <dd
                  className={`font-semibold ${variants.count > 100 ? "text-danger" : "text-primary-700"}`}
                >
                  {variants.count}/100
                </dd>
              </div>
            </dl>
            <div className="mt-5 border-t border-primary-200 pt-4 text-xs leading-6 text-primary-800">
              <p className="flex gap-2">
                <Check className="mt-1 size-3.5 shrink-0" />
                SKUs are generated automatically
              </p>
              <p className="flex gap-2">
                <Check className="mt-1 size-3.5 shrink-0" />
                Images and details can be updated later
              </p>
              <p className="flex gap-2">
                <Check className="mt-1 size-3.5 shrink-0" />
                Attributes are fixed after creation
              </p>
            </div>
          </div>
          {error && <Alert>{error}</Alert>}
          <button
            type="submit"
            className={`${primaryButton} w-full`}
            disabled={busy || shopSuspended || !categories.length}
          >
            {busy ? (
              <LoaderCircle className="size-4 animate-spin" />
            ) : (
              <PackagePlus className="size-4" />
            )}
            {busy ? "Creating product…" : "Create product"}
          </button>
          {!busy && (
            <Link to={productPath()} className={`${secondaryButton} w-full`}>
              Cancel
            </Link>
          )}
        </aside>
      </form>
    </div>
  );
}
