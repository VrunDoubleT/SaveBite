import { object, type Json } from "./db";
export function scenario(): string {
  return new URLSearchParams(window.location.search).get("mock") || "normal";
}
export function emptyLists(value: Json): Json {
  if (Array.isArray(value)) return [];
  if (!object(value)) return value;
  for (const [key, item] of Object.entries(value)) {
    if (Array.isArray(item)) value[key] = [];
    else if (object(item)) value[key] = emptyLists(item);
    else if (
      /^(totalItems|totalPages|total|unreadCount|newDealsCount|totalQuantity|totalFlashDeals)$/.test(
        key,
      )
    )
      value[key] = 0;
    else if (/^(hasMore|hasNext|hasPrevious|hasOlder|hasNewer)$/.test(key)) value[key] = false;
    else if (/^cursor[12]$/.test(key)) value[key] = "";
    else if (key === "nextCursor") value[key] = null;
  }
  return value;
}

/** Optional fixture index exposes each finite DTO enum without inventing extra wire fields. */
export function applyVariant(
  value: Json,
  fields: { path: string[]; values: (string | number)[] }[],
): Json {
  const selected = new URLSearchParams(location.search).get("variant");
  if (selected === null || !Number.isInteger(Number(selected))) return value;
  const variant = Math.max(0, Number(selected));
  function visit(node: Json, path: string[], options: (string | number)[], index = 0) {
    const [key, ...rest] = path;
    if (key === "*" && Array.isArray(node)) {
      node.forEach((item, i) => visit(item, rest, options, i));
      return;
    }
    if (!object(node) || !(key in node)) return;
    if (!rest.length) node[key] = options[(variant + index) % options.length];
    else visit(node[key], rest, options, index);
  }
  fields.forEach((field) => visit(value, field.path, field.values));
  return value;
}
