export interface RealtimeEvent<T = unknown> {
  eventId: string;
  type: string;
  version: number;
  occurredAt: string;
  data: T;
  correlationId?: string | null;
}

export const REALTIME_EVENTS = {
  SHOP_PENDING_ORDERS_CHANGED: "shop.orders.pending.changed",
  CUSTOMER_ORDERS_CHANGED: "customer.orders.changed",
  SHOP_ORDERS_CHANGED: "shop.orders.changed",
  CART_CHANGED: "cart.changed",
} as const;
