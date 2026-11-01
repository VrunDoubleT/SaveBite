import type { ShopOrder } from "@/features/shop-orders/types/shopOrder.types";
import { makeProducts, makeDeals, makeCart, makeOrders, makeRefunds } from "./factories/core";
import { object } from "./db";
function seedCore() {
  const products = makeProducts();
  const deals = makeDeals(products);
  return {
    products,
    deals,
    cart: makeCart(deals),
    orders: makeOrders(),
    refunds: makeRefunds(),
    remainingPayments: {} as Record<string, ShopOrder["payments"]>,
  };
}
type Core = ReturnType<typeof seedCore>;
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
      return value as unknown as Core;
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
