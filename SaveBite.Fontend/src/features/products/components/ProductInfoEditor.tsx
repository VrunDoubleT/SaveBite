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
  return (
    <Panel
      title="Product information"
      description="Changes apply immediately. Variant prices and SKUs stay the same."
    >
      <form
        className="space-y-4"
        onSubmit={async (e) => {
          e.preventDefault();
          if (busy) return;
          if (!name.trim()) {
            setError("Please enter a product name.");
            return;
          }
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
        }}
      >
        <fieldset disabled={busy} className="space-y-4">
          <label className="block text-sm font-medium">
            Product name
            <input
              className={inputClass}
              value={name}
              maxLength={200}
              required
              onChange={(e) => setName(e.target.value)}
            />
          </label>
          <label className="block text-sm font-medium">
            Category
            <select
              className={inputClass}
              required
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
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
          </label>
          <label className="block text-sm font-medium">
            Description
            <textarea
              className={inputClass}
              rows={6}
              maxLength={10000}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
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
