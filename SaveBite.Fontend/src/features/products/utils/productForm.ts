import { getApiErrorMessage, getApiValidationErrors } from "@/shared/api";
import type {
  Combination,
  DraftAttribute,
  PriceMap,
  ProductAttribute,
} from "../types/product.types";

export const LIMITS = {
  attributes: 3,
  values: 20,
  variants: 100,
  images: 10,
  fileBytes: 10 * 1024 * 1024,
  requestBytes: 127 * 1024 * 1024,
};
export const money = (value?: number) =>
  value == null
    ? "—"
    : new Intl.NumberFormat("en-US", {
        style: "currency",
        currency: "VND",
        maximumFractionDigits: 2,
      }).format(value);
export const dateTime = (value: string) =>
  new Intl.DateTimeFormat("en-US", { dateStyle: "short", timeStyle: "short" }).format(
    new Date(value),
  );
export const combinationKey = (ids: string[]) => [...ids].sort().join(":") || "default";
export function combinations(attributes: { values: { id: string; value: string }[] }[]): {
  count: number;
  rows: Combination[];
} {
  const count = attributes.reduce((n, a) => n * a.values.length, 1);
  if (count > LIMITS.variants || count === 0) return { count, rows: [] };
  let rows: { ids: string[]; names: string[] }[] = [{ ids: [], names: [] }];
  for (const attribute of attributes)
    rows = rows.flatMap((row) =>
      attribute.values.map((value) => ({
        ids: [...row.ids, value.id],
        names: [...row.names, value.value || "Unnamed"],
      })),
    );
  return {
    count,
    rows: rows.map((row) => ({
      key: combinationKey(row.ids),
      ids: row.ids,
      label: row.names.join(" / ") || "Default",
    })),
  };
}
export function validateAttributes(drafts: DraftAttribute[], existing: ProductAttribute[] = []) {
  if (drafts.length > LIMITS.attributes) return "You can add up to 3 attributes.";
  const names = drafts.map((a) => a.name.trim().toLocaleLowerCase("vi"));
  if (names.some((n) => !n || n.length > 100))
    return "Attribute names must contain 1–100 characters.";
  if (new Set(names).size !== names.length) return "Attribute names must be unique.";
  for (const a of drafts) {
    const old = existing.find((x) => x.id === a.id)?.values ?? [];
    const values = [...old, ...a.values].map((v) => v.value.trim().toLocaleLowerCase("vi"));
    if (!values.length || values.length > LIMITS.values)
      return `Attribute ${a.name} needs 1–20 values.`;
    if (values.some((v) => !v || v.length > 100))
      return `Values for ${a.name} must contain 1–100 characters.`;
    if (new Set(values).size !== values.length) return `Values for ${a.name} must be unique.`;
  }
  return "";
}
export function validatePrices(rows: Combination[], prices: PriceMap) {
  if (!rows.length) return "Add attribute values to create variants.";
  for (const row of rows) {
    const p = prices[row.key];
    if (!p || !/^\d+(\.\d{1,2})?$/.test(p.originalPrice) || !/^\d+(\.\d{1,2})?$/.test(p.dealPrice))
      return `Enter both prices with up to 2 decimal places for ${row.label}.`;
    const original = Number(p.originalPrice),
      deal = Number(p.dealPrice);
    if (original <= 0 || original >= 10_000_000_000 || deal < 0 || deal >= original)
      return `Original price must be positive and deal price must be lower for ${row.label}.`;
  }
  return "";
}
export function fileError(files: File[]) {
  if (files.some((f) => !["image/jpeg", "image/png", "image/webp"].includes(f.type)))
    return "Only JPG, PNG, and WebP images are supported.";
  if (files.some((f) => f.size === 0 || f.size > LIMITS.fileBytes))
    return "Each image must be nonempty and no larger than 10 MB.";
  if (files.reduce((total, f) => total + f.size, 0) > LIMITS.requestBytes)
    return "Total image size must be below 127 MB.";
  return "";
}
export function appendAttributes(form: FormData, attributes: DraftAttribute[], adding = false) {
  const included = adding ? attributes.filter((a) => a.values.length > 0) : attributes;
  included.forEach((a, i) => {
    form.append(
      `Attributes[${i}].${adding ? "AttributeId" : "Name"}`,
      adding ? a.id : a.name.trim(),
    );
    a.values.forEach((v, j) => {
      const prefix = `Attributes[${i}].Values[${j}]`;
      form.append(`${prefix}.Id`, v.id);
      form.append(`${prefix}.Value`, v.value.trim());
      if (v.image) form.append(`${prefix}.Image`, v.image);
    });
  });
}
export function appendPrices(form: FormData, rows: Combination[], prices: PriceMap) {
  rows.forEach((row, i) => {
    row.ids.forEach((id, j) => form.append(`Variants[${i}].ValueIds[${j}]`, id));
    form.append(`Variants[${i}].OriginalPrice`, prices[row.key].originalPrice);
    form.append(`Variants[${i}].DealPrice`, prices[row.key].dealPrice);
  });
}
export const describeError = (error: unknown) => {
  const messages = [...new Set(Object.values(getApiValidationErrors(error)))];
  return messages.length ? messages.join(" ") : getApiErrorMessage(error);
};
