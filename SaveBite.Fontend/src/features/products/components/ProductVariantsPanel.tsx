import { useState } from "react";
import { Plus, X } from "lucide-react";
import { productApi } from "../api/productApi";
import type { DraftAttribute, PriceMap, Product } from "../types/product.types";
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
import { Alert, Panel, primaryButton, secondaryButton, StatusBadge } from "./ProductUi";

export function ProductVariantsPanel({
  product,

  onSaved,
}: {
  product: Product;

  onSaved: (product: Product) => void;
}) {
  const [adding, setAdding] = useState(false);
  const valueNames = new Map(
    product.attributes.flatMap((a) => a.values.map((v) => [v.id, v.value] as const)),
  );
  return (
    <div className="space-y-5">
      <Panel
        title={`Product variants · ${product.variants.length}/100`}
        description="Prices and SKUs are fixed. Add values to existing attributes to create new variants."
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
