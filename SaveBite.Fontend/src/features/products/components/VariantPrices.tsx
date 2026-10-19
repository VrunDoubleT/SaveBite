import { useState } from "react";
import type { Combination, PriceInput, PriceMap } from "../types/product.types";
import { Alert, inputClass, secondaryButton } from "./ProductUi";
import { validatePrices } from "../utils/productForm";

export function VariantPrices({
  rows,
  count,
  prices,
  onChange,
}: {
  rows: Combination[];
  count: number;
  prices: PriceMap;
  onChange: (prices: PriceMap) => void;
}) {
  const [bulk, setBulk] = useState<PriceInput>({ originalPrice: "", dealPrice: "" });
  const [error, setError] = useState("");
  if (count > 100)
    return (
      <Alert>
        These values create {count} combinations, exceeding the limit of 100 variants. Reduce the
        number of values.
      </Alert>
    );
  if (!rows.length)
    return (
      <p className="rounded-lg bg-neutral-50 p-5 text-sm text-neutral-500">
        Add attribute values to see the combinations that need prices.
      </p>
    );
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end gap-3 rounded-lg bg-neutral-50 p-3">
        <label className="min-w-32 flex-1 text-xs font-medium text-neutral-600">
          Default original price (₫)
          <input
            className={inputClass}
            type="number"
            min="0.01"
            step="0.01"
            placeholder="E.g. 50000"
            value={bulk.originalPrice}
            onChange={(e) => setBulk({ ...bulk, originalPrice: e.target.value })}
          />
        </label>
        <label className="min-w-32 flex-1 text-xs font-medium text-neutral-600">
          Default deal price (₫)
          <input
            className={inputClass}
            type="number"
            min="0"
            step="0.01"
            placeholder="E.g. 30000"
            value={bulk.dealPrice}
            onChange={(e) => setBulk({ ...bulk, dealPrice: e.target.value })}
          />
        </label>
        <button
          type="button"
          className={secondaryButton}
          onClick={() => {
            const message = validatePrices([rows[0]], { [rows[0].key]: bulk });
            setError(message);
            if (!message)
              onChange({ ...prices, ...Object.fromEntries(rows.map((r) => [r.key, { ...bulk }])) });
          }}
        >
          Apply to all
        </button>
      </div>
      {error && <Alert>{error}</Alert>}
      <div className="max-h-[32rem] overflow-auto rounded-lg border border-neutral-200">
        <table className="w-full min-w-[480px] text-left text-sm">
          <thead className="sticky top-0 bg-neutral-50 text-xs text-neutral-500">
            <tr>
              <th className="px-4 py-3 font-medium">Variants ({rows.length})</th>
              <th className="px-4 py-3 font-medium">Original price (₫)</th>
              <th className="px-4 py-3 font-medium">Deal price (₫)</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-neutral-100">
            {rows.map((row) => (
              <tr key={row.key}>
                <td className="px-4 py-3 font-medium">{row.label}</td>
                {(["originalPrice", "dealPrice"] as const).map((field) => (
                  <td key={field} className="w-40 px-3 py-2">
                    <input
                      aria-label={`${field === "originalPrice" ? "Original price" : "Deal price"} ${row.label}`}
                      className={`${inputClass} mt-0! text-right`}
                      type="number"
                      step="0.01"
                      min={field === "originalPrice" ? "0.01" : "0"}
                      value={prices[row.key]?.[field] ?? ""}
                      onChange={(e) =>
                        onChange({
                          ...prices,
                          [row.key]: {
                            originalPrice: prices[row.key]?.originalPrice ?? "",
                            dealPrice: prices[row.key]?.dealPrice ?? "",
                            [field]: e.target.value,
                          },
                        })
                      }
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="text-xs text-neutral-500">
        Deal prices must be lower than original prices. SKUs are generated when you save.
      </p>
    </div>
  );
}
