import { object, type Json, type Row } from "../db";
function normalize(value: unknown) {
  return String(value ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replaceAll("đ", "d")
    .toLowerCase();
}
function text(row: Json) {
  return object(row)
    ? Object.values(row)
        .filter((value) => typeof value === "string")
        .join(" ")
    : String(row);
}
export function filtered(items: Json[], params: URLSearchParams) {
  let result = items.filter((row) => {
    const query = params.get("search") || params.get("keyword");
    if (query && !normalize(text(row)).includes(normalize(query))) return false;
    for (const key of [
      "status",
      "type",
      "rating",
      "isActive",
      "categoryId",
      "eventType",
      "isRead",
      "shopId",
      "hasReply",
      "onlyOpen",
      "onlyWithFlashDeals",
    ]) {
      const expected = params.get(key);
      if (!expected || /^(all|-1)$/i.test(expected) || !object(row)) continue;
      const source = object(row.order) && key === "status" ? row.order : row;
      const actual =
        key === "hasReply"
          ? !!row.reply
          : key === "onlyOpen"
            ? row.isOpen
            : key === "onlyWithFlashDeals"
              ? Number(row.flashDealCount) > 0
              : source[key];
      if (actual !== undefined && normalize(actual) !== normalize(expected)) return false;
    }
    if (object(row)) {
      if (params.get("isUnreadOnly") === "true" && row.isRead === true) return false;
      for (const [queryKey, field] of [
        ["year", "billingYear"],
        ["month", "billingMonth"],
      ]) {
        const expected = params.get(queryKey);
        if (expected && row[field] !== undefined && Number(row[field]) !== Number(expected))
          return false;
      }
      const categories = params.get("category")?.split(",");
      if (
        categories?.length &&
        !categories.some((category) => normalize(category) === normalize(row.categoryName))
      )
        return false;
      const shopName = params.get("shopName");
      if (shopName && normalize(row.shopName) !== normalize(shopName)) return false;
      const distance = Number(row.distanceInKm ?? 1);
      const radius = params.get("radiusInKm");
      if (radius && distance > Number(radius)) return false;
      const distanceFilter = params.get("distance");
      if (
        (distanceFilter === "Under 2km" && distance >= 2) ||
        (distanceFilter === "Under 5km" && distance >= 5) ||
        (distanceFilter === "Over 5km - 10km" && (distance <= 5 || distance > 10))
      )
        return false;
      const price = Number(row.minDealPrice ?? row.unitPrice ?? 0),
        priceFilter = params.get("price");
      if (
        (priceFilter === "Under 30k" && price >= 30000) ||
        (priceFilter === "30k - 50k" && (price < 30000 || price > 50000)) ||
        (priceFilter === "Over 50k" && price <= 50000)
      )
        return false;

      const date = String(row.createdAt ?? row.saleStartTime ?? "").slice(0, 10);
      const from = params.get("fromDate"),
        to = params.get("toDate"),
        orderDate = params.get("orderDate");
      if (date && ((from && date < from) || (to && date > to) || (orderDate && date !== orderDate)))
        return false;
    }
    return true;
  });
  const sort = params.get("sortBy") || params.get("sort");
  if (sort) {
    const direction = /desc/i.test(params.get("sortOrder") || params.get("direction") || sort)
      ? -1
      : 1;
    const key = sort.replace(/[-_](asc|desc)$/i, "");
    result = result.toSorted((a, b) => {
      const av = object(a) ? a[key] : a,
        bv = object(b) ? b[key] : b;
      return (
        direction *
        (typeof av === "number" && typeof bv === "number"
          ? av - bv
          : normalize(av).localeCompare(normalize(bv)))
      );
    });
  }
  return result;
}
export function paginate(value: Json, params: URLSearchParams): Json {
  if (Array.isArray(value)) return filtered(value, params);
  if (!object(value)) return value;
  const key = Array.isArray(value.items) ? "items" : Array.isArray(value.deals) ? "deals" : null;
  if (key) {
    const items = filtered(value[key] as Json[], params),
      size = Math.max(
        1,
        Math.min(
          100,
          Number(params.get("pageSize") || params.get("limit") || value.pageSize || 10) || 10,
        ),
      );
    const cursor = Math.max(0, Number(params.get("cursor") || params.get("cursor1") || 0) || 0);
    const page = Math.max(1, Number(params.get("page") || 1) || 1),
      offset = cursor || (page - 1) * size;
    value[key] = items.slice(offset, offset + size);
    const fields: Row = {
      page,
      pageSize: size,
      total: items.length,
      totalItems: items.length,
      totalPages: Math.ceil(items.length / size),
      hasPrevious: offset > 0,
      hasPreviousPage: offset > 0,
      hasNext: offset + size < items.length,
      hasNextPage: offset + size < items.length,
      hasMore: offset + size < items.length,
      nextCursor: offset + size < items.length ? String(offset + size) : null,
      cursor1: String(offset + size),
      cursor2: String(offset),
      hasOlder: offset + size < items.length,
      hasNewer: false,
    };
    for (const field of Object.keys(value)) if (field in fields) value[field] = fields[field];
    return value;
  }
  for (const [field, item] of Object.entries(value))
    if (object(item)) value[field] = paginate(item, params);
  return value;
}
