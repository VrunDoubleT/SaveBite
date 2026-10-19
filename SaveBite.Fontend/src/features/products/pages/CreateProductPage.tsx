import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft, Check, LoaderCircle, PackagePlus } from "lucide-react";
import { productApi } from "../api/productApi";
import { useProductWorkspace } from "../hooks/useProductWorkspace";
import { Alert, inputClass, Panel, primaryButton, secondaryButton } from "../components/ProductUi";
import { GalleryEditor } from "../components/GalleryEditor";
import { AttributesEditor } from "../components/AttributesEditor";
import { VariantPrices } from "../components/VariantPrices";
import type { DraftAttribute, PriceMap } from "../types/product.types";
import {
  appendAttributes,
  appendPrices,
  combinations,
  describeError,
  fileError,
  validateAttributes,
  validatePrices,
} from "../utils/productForm";

export function CreateProductPage() {
  const { categories, productPath } = useProductWorkspace();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [attributes, setAttributes] = useState<DraftAttribute[]>([]);
  const [prices, setPrices] = useState<PriceMap>({});
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{
    name?: string;
    categoryId?: string;
    description?: string;
  }>({});
  const variants = combinations(attributes);
  async function submit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy) return;
    const images = [
      ...files,
      ...attributes.flatMap((a) => a.values.flatMap((v) => (v.image ? [v.image] : []))),
    ];
    const errors: { name?: string; categoryId?: string; description?: string } = {};
    const trimmedName = name.trim();
    if (!trimmedName) {
      errors.name = "Please enter a product name.";
    } else if (trimmedName.length < 2) {
      errors.name = "Product name must be at least 2 characters.";
    } else if (trimmedName.length > 200) {
      errors.name = "Product name cannot exceed 200 characters.";
    }
    if (!categoryId) {
      errors.categoryId = "Please select a category.";
    }
    if (description.length > 10000) {
      errors.description = "Description cannot exceed 10,000 characters.";
    }
    setFieldErrors(errors);
    const hasFieldErrors = Boolean(errors.name || errors.categoryId || errors.description);
    const attrValidation = validateAttributes(attributes);
    const variantValidation =
      variants.count > 100 ? "A product cannot have more than 100 variants." : "";
    const priceValidation = validatePrices(variants.rows, prices);
    const imgValidation = fileError(images);
    const firstOtherError = attrValidation || variantValidation || priceValidation || imgValidation;
    if (hasFieldErrors || firstOtherError) {
      setError(
        firstOtherError ||
          errors.name ||
          errors.categoryId ||
          errors.description ||
          "Please fix validation errors.",
      );
      return;
    }
    setBusy(true);
    setError("");
    const body = new FormData();
    body.append("Name", name.trim());
    body.append("Description", description.trim());
    body.append("CategoryId", categoryId);
    files.forEach((file) => body.append("Images", file));
    appendAttributes(body, attributes);
    appendPrices(body, variants.rows, prices);
    try {
      const product = await productApi.create(body);
      navigate(productPath(`/${product.id}`), {
        replace: true,
        state: { message: "Product created and ready for flash deals." },
      });
    } catch (err) {
      setError(describeError(err));
    } finally {
      setBusy(false);
    }
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
        onSubmit={submit}
        noValidate
        className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_280px]"
      >
        <fieldset disabled={busy} className="min-w-0 space-y-5 disabled:opacity-70">
          <Panel
            title="01 · Product information"
            description="Clear names and descriptions help customers choose."
          >
            <div className="space-y-4">
              <label className="block text-sm font-medium">
                Product name <span className="text-danger">*</span>
                <input
                  className={`${inputClass} ${
                    fieldErrors.name ? "border-danger focus:border-danger focus:ring-danger" : ""
                  }`}
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    if (fieldErrors.name) setFieldErrors((prev) => ({ ...prev, name: undefined }));
                  }}
                  maxLength={200}
                  placeholder="E.g. Honey grilled chicken rice"
                />
                <div className="mt-1 flex items-center justify-between">
                  {fieldErrors.name ? (
                    <p className="text-xs font-medium text-danger">{fieldErrors.name}</p>
                  ) : (
                    <span />
                  )}
                  <p className="text-xs text-neutral-400">{name.length}/200 characters</p>
                </div>
              </label>
              <label className="block text-sm font-medium">
                Category <span className="text-danger">*</span>
                <select
                  className={`${inputClass} ${
                    fieldErrors.categoryId
                      ? "border-danger focus:border-danger focus:ring-danger"
                      : ""
                  }`}
                  value={categoryId}
                  onChange={(e) => {
                    setCategoryId(e.target.value);
                    if (fieldErrors.categoryId)
                      setFieldErrors((prev) => ({ ...prev, categoryId: undefined }));
                  }}
                >
                  <option value="">Select a category</option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
                {fieldErrors.categoryId && (
                  <p className="mt-1 text-xs font-medium text-danger">{fieldErrors.categoryId}</p>
                )}
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
                  className={`${inputClass} ${
                    fieldErrors.description
                      ? "border-danger focus:border-danger focus:ring-danger"
                      : ""
                  }`}
                  rows={4}
                  maxLength={10000}
                  value={description}
                  onChange={(e) => {
                    setDescription(e.target.value);
                    if (fieldErrors.description)
                      setFieldErrors((prev) => ({ ...prev, description: undefined }));
                  }}
                  placeholder="Describe the food, ingredients, portions, and any notes…"
                />
                <div className="mt-1 flex items-center justify-between">
                  {fieldErrors.description ? (
                    <p className="text-xs font-medium text-danger">{fieldErrors.description}</p>
                  ) : (
                    <span />
                  )}
                  <p className="text-xs text-neutral-400">{description.length}/10,000 characters</p>
                </div>
              </label>
            
            </div>
          </Panel>
          <Panel
            title="02 · Product images"
            description="Up to 10 gallery images. Add variant images in the attributes section."
          >
            <GalleryEditor files={files} onFilesChange={setFiles} />
          </Panel>
          <Panel
            title="03 · Attributes and values"
            description="Up to 3 attributes and 20 values per attribute."
          >
            <AttributesEditor attributes={attributes} onChange={setAttributes} />
          </Panel>
          <Panel
            title="04 · Variant prices"
            description="Combinations are generated automatically. Enter a price for each variant before saving."
          >
            <VariantPrices
              rows={variants.rows}
              count={variants.count}
              prices={prices}
              onChange={setPrices}
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
            disabled={busy || variants.count > 100 || !categories.length}
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
