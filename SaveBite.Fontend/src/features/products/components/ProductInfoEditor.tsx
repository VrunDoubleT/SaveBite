import { useState } from "react";
import { Save } from "lucide-react";
import { productApi } from "../api/productApi";
import type { Option, Product } from "../types/product.types";
import { describeError } from "../utils/productForm";
import { Alert, inputClass, Panel, primaryButton } from "./ProductUi";

export function ProductInfoEditor({
  product,
  categories,
  onSaved,
}: {
  product: Product;
  categories: Option[];
  onSaved: (product: Product) => void;
}) {
  const [name, setName] = useState(product.name),
    [description, setDescription] = useState(product.description ?? ""),
    [categoryId, setCategoryId] = useState(product.categoryId);
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{
    name?: string;
    categoryId?: string;
    description?: string;
  }>({});

  return (
    <Panel
      title="Product information"
      description="Changes apply immediately. Variant prices and SKUs stay the same."
    >
      <form
        noValidate
        className="space-y-4"
        onSubmit={async (e) => {
          e.preventDefault();
          if (busy) return;
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

          if (errors.name || errors.categoryId || errors.description) {
            setError(
              errors.name ||
                errors.categoryId ||
                errors.description ||
                "Please fix validation errors.",
            );
            return;
          }

          setBusy(true);
          setError("");
          try {
            onSaved(
              await productApi.update(product.id, {
                name: trimmedName,
                description: description.trim(),
                categoryId,
              }),
            );
          } catch (err) {
            setError(describeError(err));
          } finally {
            setBusy(false);
          }
        }}
      >
        <fieldset disabled={busy} className="space-y-4">
          <label className="block text-sm font-medium">
            Product name <span className="text-danger">*</span>
            <input
              className={`${inputClass} ${
                fieldErrors.name ? "border-danger focus:border-danger focus:ring-danger" : ""
              }`}
              value={name}
              maxLength={200}
              onChange={(e) => {
                setName(e.target.value);
                if (fieldErrors.name) setFieldErrors((prev) => ({ ...prev, name: undefined }));
              }}
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
                fieldErrors.categoryId ? "border-danger focus:border-danger focus:ring-danger" : ""
              }`}
              value={categoryId}
              onChange={(e) => {
                setCategoryId(e.target.value);
                if (fieldErrors.categoryId)
                  setFieldErrors((prev) => ({ ...prev, categoryId: undefined }));
              }}
            >
              {!categories.some((c) => c.id === product.categoryId) && (
                <option value={product.categoryId} disabled>
                  {product.categoryName} (inactive)
                </option>
              )}
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

          <label className="block text-sm font-medium">
            Description
            <textarea
              className={`${inputClass} ${
                fieldErrors.description ? "border-danger focus:border-danger focus:ring-danger" : ""
              }`}
              rows={6}
              maxLength={10000}
              value={description}
              onChange={(e) => {
                setDescription(e.target.value);
                if (fieldErrors.description)
                  setFieldErrors((prev) => ({ ...prev, description: undefined }));
              }}
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
        </fieldset>

        {error && <Alert>{error}</Alert>}

        <div className="flex justify-end">
          <button type="submit" disabled={busy || !categories.length} className={primaryButton}>
            <Save className="size-4" />
            {busy ? "Saving…" : "Save details"}
          </button>
        </div>
      </form>
    </Panel>
  );
}
