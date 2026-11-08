import { useState } from "react";
import { useForm } from "react-hook-form";
import { productInfoRules, type ProductInfoValues } from "../utils/productValidation";
import { FieldError } from "./FieldError";
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
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ProductInfoValues>({
    defaultValues: {
      name: product.name,
      description: product.description ?? "",
      categoryId: product.categoryId,
    },
    mode: "onTouched",
  });
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  return (
    <Panel
      title="Product information"
      description="Changes apply immediately. Variant prices and SKUs stay the same."
    >
      <form
        className="space-y-4"
        noValidate
        onSubmit={handleSubmit(async ({ name, description, categoryId }) => {
          if (busy) return;
          setBusy(true);
          setError("");
          try {
            onSaved(
              await productApi.update(product.id, {
                name: name.trim(),
                description: description.trim(),
                categoryId,
              }),
            );
          } catch (err) {
            setError(describeError(err));
          } finally {
            setBusy(false);
          }
        })}
      >
        <fieldset disabled={busy} className="space-y-4">
          <label className="block text-sm font-medium">
            Product name
            <input
              className={inputClass}
              maxLength={200}
              required
              {...register("name", productInfoRules.name)}
              aria-invalid={!!errors.name}
              aria-describedby="edit-product-name-error"
            />
            <FieldError id="edit-product-name-error" message={errors.name?.message} />
          </label>
          <label className="block text-sm font-medium">
            Category
            <select
              className={inputClass}
              required
              {...register("categoryId", productInfoRules.categoryId)}
              aria-invalid={!!errors.categoryId}
              aria-describedby="edit-product-categoryId-error"
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
            <FieldError id="edit-product-categoryId-error" message={errors.categoryId?.message} />
          </label>
          <label className="block text-sm font-medium">
            Description
            <textarea
              className={inputClass}
              rows={6}
              maxLength={10000}
              {...register("description", productInfoRules.description)}
              aria-invalid={!!errors.description}
              aria-describedby="edit-product-description-error"
            />
            <FieldError id="edit-product-description-error" message={errors.description?.message} />
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
