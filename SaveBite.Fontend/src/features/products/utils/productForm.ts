import { getApiErrorMessage, getApiValidationErrors } from "@/shared/api";
import { MIN_AMOUNT_VND, MIN_AMOUNT_TEXT } from "@/shared/config/money";
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
export function attributeErrors(drafts: DraftAttribute[], existing: ProductAttribute[] = []) {
  const names: Record<string, string> = {};
  const values: Record<string, string> = {};
  const blocks: Record<string, string> = {};
  const normalize = (value: string) => value.trim().toLocaleLowerCase("vi");
  for (const attribute of drafts) {
    const name = normalize(attribute.name);
    if (!name) names[attribute.id] = "Please enter an attribute name.";
    else if (name.length > 100)
      names[attribute.id] = "Attribute name must not exceed 100 characters.";
    else if (drafts.some((other) => other.id !== attribute.id && normalize(other.name) === name))
      names[attribute.id] = "Attribute names must be unique.";
    const saved = existing.find((a) => a.id === attribute.id)?.values ?? [];
    const allValues = [...saved, ...attribute.values];
    if (!allValues.length) blocks[attribute.id] = "Add at least one attribute value.";
    else if (allValues.length > LIMITS.values)
      blocks[attribute.id] = "You can add up to 20 values per attribute.";
    for (const value of attribute.values) {
      const text = normalize(value.value);
      if (!text) values[value.id] = "Please enter an attribute value.";
      else if (text.length > 100)
        values[value.id] = "Attribute value must not exceed 100 characters.";
      else if (allValues.some((other) => other.id !== value.id && normalize(other.value) === text))
        values[value.id] = "Values within this attribute must be unique.";
    }
  }
  return { names, values, blocks };
}
export function validateAttributes(drafts: DraftAttribute[], existing: ProductAttribute[] = []) {
  if (drafts.length > LIMITS.attributes) return "You can add up to 3 attributes.";
  const errors = attributeErrors(drafts, existing);
  return (
    Object.values(errors.names)[0] ||
    Object.values(errors.blocks)[0] ||
    Object.values(errors.values)[0] ||
    ""
  );
}
export const PRODUCT_PRICE_STEP = 1000;
export function validateProductPrice(value: string) {
  if (!value.trim()) return "Please enter a price.";
  if (!/^\d+$/.test(value.trim())) return "Enter a whole VND amount.";
  const amount = Number(value);
  if (amount < MIN_AMOUNT_VND) return `Price must be at least ${MIN_AMOUNT_TEXT} VND.`;
  if (amount >= 10_000_000_000) return "Price must be less than 10,000,000,000 VND.";
  if (amount % PRODUCT_PRICE_STEP !== 0) return "Price must be a multiple of 1,000 VND.";
  return "";
}
export function validatePrices(rows: Combination[], prices: PriceMap) {
  if (!rows.length) return "Add attribute values to create variants.";
  for (const row of rows) {
    const p = prices[row.key];
    const origStr = String(p?.originalPrice ?? "").trim();
    const dealStr = String(p?.dealPrice ?? "").trim();
    if (!origStr || !dealStr)
      return `Please enter both original price and deal price for ${row.label}.`;
    const priceError = validateProductPrice(origStr) || validateProductPrice(dealStr);
    if (priceError) return `${row.label}: ${priceError}`;
    const original = Number(origStr),
      deal = Number(dealStr);
    if (deal >= original) return `Deal price must be lower than original price for ${row.label}.`;
  }
  return "";
}
export function fileError(files: File[]) {
  if (files.some((f) => !["image/jpeg", "image/png", "image/webp"].includes(f.type)))
    return "Only JPG, PNG, and WebP images are supported.";
  if (files.some((f) => f.size === 0 || f.size > LIMITS.fileBytes))
    return "Each image must be nonempty and no larger than 10 MB.";
  return "";
}
export function appendAttributes(
  form: FormData,
  attributes: DraftAttribute[],
  adding = false,
  includeImages = true,
) {
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
      if (includeImages && v.image) form.append(`${prefix}.Image`, v.image);
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
