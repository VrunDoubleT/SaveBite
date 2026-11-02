import type { ShopOrder } from "@/features/shop-orders/types/shopOrder.types";
import { makeProducts, makeDeals, makeCart, makeOrders, makeRefunds } from "./factories/core";
import { object } from "./db";
function seedCore() {
  const products = makeProducts();
  const deals = makeDeals(products);
  return {
    products,
    deals,
    mediaSeedVersion: 1,
    cart: makeCart(deals),
    orders: makeOrders(),
    refunds: makeRefunds(),
    remainingPayments: {} as Record<string, ShopOrder["payments"]>,
  };
}
type Core = ReturnType<typeof seedCore>;
/** Upgrade only the featured demo's media/options; retain the rest of the session. */
function migrateMediaSeed(saved: Core): Core {
  if (saved.mediaSeedVersion === 1) return saved;
  const fresh = seedCore();
  const demo = fresh.deals[1];
  const deal = saved.deals.find((item) => item.id === demo.id);
  const product = saved.products.find((item) => item.id === demo.productId);
  if (deal)
    Object.assign(deal, {
      productImageUrl: demo.productImageUrl,
      productImageUrls: demo.productImageUrls,
      attributeGroups: demo.attributeGroups,
      variants: demo.variants,
      minDealPrice: demo.minDealPrice,
      maxOriginalPrice: demo.maxOriginalPrice,
      maxDiscountPercent: demo.maxDiscountPercent,
    });
  if (product)
    Object.assign(product, {
      images: fresh.products[1].images,
      attributes: fresh.products[1].attributes,
      variants: fresh.products[1].variants,
    });
  saved.mediaSeedVersion = 1;
  sessionStorage.setItem("savebite-mock-core-v1", JSON.stringify(saved));
  return saved;
}

function load(): Core {
  try {
    const value: unknown = JSON.parse(sessionStorage.getItem("savebite-mock-core-v1") || "null");
    if (
      object(value) &&
      Array.isArray(value.products) &&
      Array.isArray(value.orders) &&
      Array.isArray(value.deals) &&
      Array.isArray(value.refunds) &&
      object(value.cart)
    )
      return migrateMediaSeed(value as unknown as Core);
  } catch {
    /* Invalid session seeds again. */
  }
  return seedCore();
}
export const core = load();
core.remainingPayments ??= {};
export function saveCore() {
  sessionStorage.setItem("savebite-mock-core-v1", JSON.stringify(core));
}
