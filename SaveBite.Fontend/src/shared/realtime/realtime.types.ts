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
  SHOP_STATUS_CHANGED: "shop.status.changed",
  CART_CHANGED: "cart.changed",
  NOTIFICATION_RECEIVED: "notification.received",
  NOTIFICATION_READ: "notification.read",
  NOTIFICATION_DELETED: "notification.deleted",
  FEEDBACK_CHANGED: "feedback.changed",
} as const;
