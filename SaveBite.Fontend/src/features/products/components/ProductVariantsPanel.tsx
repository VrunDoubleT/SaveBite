import { useState } from "react";
import { Pencil, Plus, X } from "lucide-react";
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

export function ProductVariantsPanel({
  product,
  onSaved,
}: {
  product: Product;
  onSaved: (product: Product) => void;
}) {
  const [adding, setAdding] = useState(false);
  const [editingVariant, setEditingVariant] = useState<ProductVariant | null>(null);
  const valueNames = new Map(
    product.attributes.flatMap((a) => a.values.map((v) => [v.id, v.value] as const)),
  );
  return (
    <div className="space-y-5">
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

function AddValuesForm({
  product,
  onCancel,
  onSaved,
}: {
  product: Product;
  onCancel: () => void;
  onSaved: (product: Product) => void;
}) {
  const [drafts, setDrafts] = useState<DraftAttribute[]>(
    product.attributes.map((a) => ({ id: a.id, name: a.name, values: [] })),
  );
  const [prices, setPrices] = useState<PriceMap>({}),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  const merged = product.attributes.map((a) => ({
    ...a,
    values: [...a.values, ...(drafts.find((d) => d.id === a.id)?.values ?? [])],
  }));
  const generated = combinations(merged);
  const existingKeys = new Set(product.variants.map((v) => combinationKey(v.valueIds)));
  const rows = generated.rows.filter((row) => !existingKeys.has(row.key));
  const newCount = drafts.reduce((n, a) => n + a.values.length, 0);
  return (
    <Panel
      title="Add attribute values"
      description="Existing values stay the same. Enter prices for new combinations only."
    >
      <form
        className="space-y-5"
        onSubmit={async (e) => {
          e.preventDefault();
          if (busy) return;
          const message = !newCount
            ? "Add at least one new value."
            : validateAttributes(drafts, product.attributes) ||
              (generated.count > 100 ? "The total number of variants exceeds 100." : "") ||
              validatePrices(rows, prices) ||
              fileError(drafts.flatMap((a) => a.values.flatMap((v) => (v.image ? [v.image] : []))));
          if (message) {
            setError(message);
            return;
          }
          setBusy(true);
          setError("");
          const body = new FormData();
          appendAttributes(body, drafts, true);
          appendPrices(body, rows, prices);
          try {
            onSaved(await productApi.addValues(product.id, body));
          } catch (err) {
            setError(describeError(err));
          } finally {
            setBusy(false);
          }
        }}
      >
        <fieldset disabled={busy} className="space-y-5">
          <AttributesEditor
            attributes={drafts}
            onChange={setDrafts}
            existing={product.attributes}
          />
          <div>
            <h3 className="mb-3 text-sm font-semibold">
              {rows.length} new variants · Total {generated.count}/100
            </h3>
            <VariantPrices
              rows={rows}
              count={generated.count}
              prices={prices}
              onChange={setPrices}
            />
          </div>
        </fieldset>
        {error && <Alert>{error}</Alert>}
        <div className="flex justify-end gap-3">
          <button type="button" disabled={busy} className={secondaryButton} onClick={onCancel}>
            <X className="size-4" />
            Cancel
          </button>
          <button className={primaryButton} disabled={busy || !newCount || generated.count > 100}>
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
  const [originalPrice, setOriginalPrice] = useState(String(variant.originalPrice));
  const [dealPrice, setDealPrice] = useState(String(variant.dealPrice));
  const [isActive, setIsActive] = useState(variant.isActive);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const valueNames = new Map(
    product.attributes.flatMap((a) => a.values.map((v) => [v.id, v.value] as const)),
  );
  const variantTitle =
    variant.valueIds.map((id) => valueNames.get(id) ?? "—").join(" / ") || "Default";

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (busy) return;

    const op = Number(originalPrice);
    const dp = Number(dealPrice);

    if (isNaN(op) || op <= 0) {
      setError("Original price must be greater than 0.");
      return;
    }
    if (isNaN(dp) || dp < 0) {
      setError("Deal price must be greater than or equal to 0.");
      return;
    }
    if (dp >= op) {
      setError("Deal price must be less than original price.");
      return;
    }
    if (!isActive && product.status === 0) {
      const hasOtherActive = product.variants.some((v) => v.id !== variant.id && v.isActive);
      if (!hasOtherActive) {
        setError("An active product must have at least one active variant.");
        return;
      }
    }

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
      setError(describeError(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal title={`Edit variant · ${variantTitle}`} busy={busy} onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
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
              step="any"
              min="1"
              required
              className={inputClass}
              value={originalPrice}
              onChange={(e) => setOriginalPrice(e.target.value)}
            />
          </label>
        </div>

        <div>
          <label className="block text-sm font-medium text-neutral-800">
            Deal price (VND) <span className="text-red-500">*</span>
            <input
              type="number"
              step="any"
              min="0"
              required
              className={inputClass}
              value={dealPrice}
              onChange={(e) => setDealPrice(e.target.value)}
            />
          </label>
          {Number(originalPrice) > 0 &&
            Number(dealPrice) >= 0 &&
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
            checked={isActive}
            onChange={(e) => setIsActive(e.target.checked)}
            className="size-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500"
          />
          <label
            htmlFor="variant-active-toggle"
            className="cursor-pointer text-sm font-medium text-neutral-800"
          >
            Active for sale
          </label>
        </div>

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
