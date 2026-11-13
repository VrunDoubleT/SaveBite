export interface RealtimeEvent<T = unknown> {
  eventId: string;
  type: string;
  version: number;
  occurredAt: string;
  data: T;
  correlationId?: string | null;
}

export const REALTIME_EVENTS = {
  CUSTOMER_STATUS_CHANGED: "customer.status.changed",
  STAFF_INVITATIONS_CHANGED: "staff.invitations.changed",
  SHOP_STAFF_CHANGED: "shop.staff.changed",
  USER_ROLE_CHANGED: "user.role.changed",
  SHOP_APPLICATION_REVIEWED: "shop.application.reviewed",
  SHOP_PENDING_ORDERS_CHANGED: "shop.orders.pending.changed",
  CUSTOMER_ORDERS_CHANGED: "customer.orders.changed",
  SHOP_ORDERS_CHANGED: "shop.orders.changed",
  SHOP_STATUS_CHANGED: "shop.status.changed",
  PLATFORM_FEE_CHANGED: "platform-fee.changed",
  CART_CHANGED: "cart.changed",
  NOTIFICATION_RECEIVED: "notification.received",
  NOTIFICATION_READ: "notification.read",
  NOTIFICATION_DELETED: "notification.deleted",
  FEEDBACK_CHANGED: "feedback.changed",
  TRUST_LEVEL_CHANGED: "trust.level.changed",
} as const;
