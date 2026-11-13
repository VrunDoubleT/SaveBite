import type { LucideIcon } from "lucide-react";
import {
  AlertCircle,
  AlertOctagon,
  AlertTriangle,
  BadgeCheck,
  Ban,
  Banknote,
  Bell,
  Building2,
  CheckCheck,
  CheckCircle2,
  CircleAlert,
  Clock,
  CreditCard,
  FileEdit,
  FileText,
  FileWarning,
  Hourglass,
  MessageSquareQuote,
  MessageSquareX,
  PackageCheck,
  Receipt,
  RefreshCw,
  Shield,
  ShieldAlert,
  ShieldCheck,
  ShoppingBag,
  Star,
  Store,
  TimerOff,
  Trash2,
  UserCheck,
  UserCog,
  UserMinus,
  UserPlus,
  Users,
  UserX,
  XCircle,
  Zap,
} from "lucide-react";

/**
 * Master registry of notification types across the SaveBite platform.
 */
export const NOTIFICATION_TYPES = {
  // Order lifecycle
  ORDER_CREATED: "OrderCreated",
  ORDER_ACCEPTED: "OrderAccepted",
  ORDER_REJECTED: "OrderRejected",
  ORDER_COMPLETED: "OrderCompleted",
  ORDER_CANCELLED_BY_CUSTOMER: "OrderCancelledByCustomer",
  ORDER_PAYMENT_EXPIRED: "OrderPaymentExpired",
  ORDER_NO_SHOW: "OrderNoShow",
  PICKUP_REMINDER: "PickupReminder",
  PICKUP_DEADLINE_APPROACHING: "PickupDeadlineApproaching",

  // Payment
  PAYMENT_SUCCEEDED: "PaymentSucceeded",
  PAYMENT_FAILED: "PaymentFailed",

  // Refund
  REFUND_REQUEST_CREATED: "RefundRequestCreated",
  REFUND_DESTINATION_REQUIRED: "RefundDestinationRequired",
  REFUND_OVERDUE: "RefundOverdue",
  REFUND_APPROVED: "RefundApproved",
  REFUND_REJECTED: "RefundRejected",
  REFUND_TRANSFERRED: "RefundTransferred",

  // Staff & shop members
  STAFF_INVITATION_RECEIVED: "StaffInvitationReceived",
  STAFF_INVITATION_ACCEPTED: "StaffInvitationAccepted",
  STAFF_INVITATION_DECLINED: "StaffInvitationDeclined",
  STAFF_INVITATION_EXPIRED: "StaffInvitationExpired",
  STAFF_JOINED_SHOP: "StaffJoinedShop",
  STAFF_STATUS_CHANGED: "StaffStatusChanged",
  STAFF_LEFT_SHOP: "StaffLeftShop",

  // Shop application & registration
  SHOP_APPLICATION_SUBMITTED: "ShopApplicationSubmitted",
  SHOP_APPLICATION_APPROVED: "ShopApplicationApproved",
  SHOP_APPLICATION_REJECTED: "ShopApplicationRejected",
  SHOP_APPLICATION_NEEDS_REVISION: "ShopApplicationNeedsRevision",
  SHOP_STATUS_CHANGED: "ShopStatusChanged",

  // Deals & products
  FLASH_DEAL_AVAILABLE: "FlashDealAvailable",
  PRODUCT_NEEDS_REVISION: "ProductNeedsRevision",
  PRODUCT_CONTENT_APPROVED: "ProductContentApproved",
  PRODUCT_REMOVED: "ProductRemoved",

  // Feedback & reviews
  FEEDBACK_RECEIVED: "FeedbackReceived",
  FEEDBACK_REPLIED: "FeedbackReplied",
  FEEDBACK_REMOVED_BY_ADMIN: "FeedbackRemovedByAdmin",

  // Trust & penalties
  TRUST_SCORE_CHANGED: "TrustScoreChanged",
  TRUST_LEVEL_CHANGED: "TrustLevelChanged",
  NO_SHOW_PENALTY_CREATED: "NoShowPenaltyCreated",
  ACCOUNT_STATUS_CHANGED: "AccountStatusChanged",

  // Platform fees
  PLATFORM_FEE_STATEMENT_ISSUED: "PlatformFeeStatementIssued",
  PLATFORM_FEE_DUE_SOON: "PlatformFeeDueSoon",
  PLATFORM_FEE_OVERDUE: "PlatformFeeOverdue",
  PLATFORM_FEE_PAID: "PlatformFeePaid",
  PLATFORM_FEE_SCHEDULE_ACTIVATED: "PlatformFeeScheduleActivated",
  PLATFORM_FEE_RATE_CHANGED: "PlatformFeeRateChanged",
} as const;

export type NotificationType = (typeof NOTIFICATION_TYPES)[keyof typeof NOTIFICATION_TYPES];

/**
 * Numeric ID to string key mapping for backward-compatible integer enum payloads.
 */
export const NOTIFICATION_TYPE_BY_ID: Record<number, NotificationType> = {
  1: NOTIFICATION_TYPES.ORDER_CREATED,
  2: NOTIFICATION_TYPES.ORDER_ACCEPTED,
  3: NOTIFICATION_TYPES.ORDER_REJECTED,
  4: NOTIFICATION_TYPES.PICKUP_REMINDER,
  5: NOTIFICATION_TYPES.ORDER_COMPLETED,
  6: NOTIFICATION_TYPES.PAYMENT_SUCCEEDED,
  7: NOTIFICATION_TYPES.PAYMENT_FAILED,
  8: NOTIFICATION_TYPES.REFUND_REQUEST_CREATED,
  9: NOTIFICATION_TYPES.REFUND_APPROVED,
  10: NOTIFICATION_TYPES.REFUND_REJECTED,
  11: NOTIFICATION_TYPES.REFUND_TRANSFERRED,
  12: NOTIFICATION_TYPES.STAFF_INVITATION_RECEIVED,
  13: NOTIFICATION_TYPES.STAFF_INVITATION_ACCEPTED,
  14: NOTIFICATION_TYPES.SHOP_APPLICATION_APPROVED,
  15: NOTIFICATION_TYPES.SHOP_APPLICATION_REJECTED,
  16: NOTIFICATION_TYPES.SHOP_APPLICATION_NEEDS_REVISION,
  17: NOTIFICATION_TYPES.FLASH_DEAL_AVAILABLE,
  18: NOTIFICATION_TYPES.TRUST_SCORE_CHANGED,
  19: NOTIFICATION_TYPES.NO_SHOW_PENALTY_CREATED,
  20: NOTIFICATION_TYPES.PRODUCT_NEEDS_REVISION,
  21: NOTIFICATION_TYPES.ORDER_NO_SHOW,
  22: NOTIFICATION_TYPES.STAFF_INVITATION_DECLINED,
  23: NOTIFICATION_TYPES.STAFF_INVITATION_EXPIRED,
  24: NOTIFICATION_TYPES.STAFF_JOINED_SHOP,
  25: NOTIFICATION_TYPES.FEEDBACK_RECEIVED,
  26: NOTIFICATION_TYPES.FEEDBACK_REPLIED,
  27: NOTIFICATION_TYPES.PLATFORM_FEE_STATEMENT_ISSUED,
  28: NOTIFICATION_TYPES.PRODUCT_CONTENT_APPROVED,
  29: NOTIFICATION_TYPES.SHOP_STATUS_CHANGED,
  30: NOTIFICATION_TYPES.ACCOUNT_STATUS_CHANGED,
  31: NOTIFICATION_TYPES.FEEDBACK_REMOVED_BY_ADMIN,
  32: NOTIFICATION_TYPES.PLATFORM_FEE_DUE_SOON,
  33: NOTIFICATION_TYPES.PLATFORM_FEE_OVERDUE,
  34: NOTIFICATION_TYPES.PLATFORM_FEE_PAID,
  35: NOTIFICATION_TYPES.ORDER_CANCELLED_BY_CUSTOMER,
  36: NOTIFICATION_TYPES.ORDER_PAYMENT_EXPIRED,
  37: NOTIFICATION_TYPES.STAFF_STATUS_CHANGED,
  38: NOTIFICATION_TYPES.STAFF_LEFT_SHOP,
  39: NOTIFICATION_TYPES.PRODUCT_REMOVED,
  40: NOTIFICATION_TYPES.TRUST_LEVEL_CHANGED,
  41: NOTIFICATION_TYPES.PICKUP_DEADLINE_APPROACHING,
  42: NOTIFICATION_TYPES.SHOP_APPLICATION_SUBMITTED,
  43: NOTIFICATION_TYPES.REFUND_DESTINATION_REQUIRED,
  44: NOTIFICATION_TYPES.REFUND_OVERDUE,
  45: NOTIFICATION_TYPES.PLATFORM_FEE_SCHEDULE_ACTIVATED,
  46: NOTIFICATION_TYPES.PLATFORM_FEE_RATE_CHANGED,
};

export interface NotificationVisualConfig {
  Icon: LucideIcon;
  bg: string;
  label: string;
}

/**
 * Dedicated visual presentation (icon, badge styling, label) for each notification type.
 */
export const NOTIFICATION_VISUALS: Record<NotificationType, NotificationVisualConfig> = {
  // Orders
  [NOTIFICATION_TYPES.ORDER_CREATED]: {
    Icon: ShoppingBag,
    bg: "bg-emerald-100 text-emerald-700",
    label: "Order placed",
  },
  [NOTIFICATION_TYPES.ORDER_ACCEPTED]: {
    Icon: CheckCircle2,
    bg: "bg-teal-100 text-teal-700",
    label: "Order accepted",
  },
  [NOTIFICATION_TYPES.ORDER_REJECTED]: {
    Icon: XCircle,
    bg: "bg-rose-100 text-rose-700",
    label: "Order rejected",
  },
  [NOTIFICATION_TYPES.ORDER_COMPLETED]: {
    Icon: PackageCheck,
    bg: "bg-emerald-100 text-emerald-700",
    label: "Order completed",
  },
  [NOTIFICATION_TYPES.ORDER_CANCELLED_BY_CUSTOMER]: {
    Icon: Ban,
    bg: "bg-rose-100 text-rose-700",
    label: "Order cancelled",
  },
  [NOTIFICATION_TYPES.ORDER_PAYMENT_EXPIRED]: {
    Icon: TimerOff,
    bg: "bg-amber-100 text-amber-700",
    label: "Payment window expired",
  },
  [NOTIFICATION_TYPES.ORDER_NO_SHOW]: {
    Icon: UserX,
    bg: "bg-rose-100 text-rose-700",
    label: "Order no-show",
  },
  [NOTIFICATION_TYPES.PICKUP_REMINDER]: {
    Icon: Clock,
    bg: "bg-amber-100 text-amber-700",
    label: "Pickup reminder",
  },
  [NOTIFICATION_TYPES.PICKUP_DEADLINE_APPROACHING]: {
    Icon: Hourglass,
    bg: "bg-amber-100 text-amber-700",
    label: "Pickup deadline approaching",
  },

  // Payments
  [NOTIFICATION_TYPES.PAYMENT_SUCCEEDED]: {
    Icon: CreditCard,
    bg: "bg-blue-100 text-blue-700",
    label: "Payment received",
  },
  [NOTIFICATION_TYPES.PAYMENT_FAILED]: {
    Icon: CircleAlert,
    bg: "bg-rose-100 text-rose-700",
    label: "Payment failed",
  },

  // Refunds
  [NOTIFICATION_TYPES.REFUND_OVERDUE]: {
    Icon: AlertTriangle,
    bg: "bg-rose-100 text-rose-700",
    label: "Mandatory refund overdue",
  },
  [NOTIFICATION_TYPES.REFUND_DESTINATION_REQUIRED]: {
    Icon: CreditCard,
    bg: "bg-amber-100 text-amber-700",
    label: "Refund details required",
  },
  [NOTIFICATION_TYPES.REFUND_REQUEST_CREATED]: {
    Icon: RefreshCw,
    bg: "bg-purple-100 text-purple-700",
    label: "Refund requested",
  },
  [NOTIFICATION_TYPES.REFUND_APPROVED]: {
    Icon: BadgeCheck,
    bg: "bg-emerald-100 text-emerald-700",
    label: "Refund approved",
  },
  [NOTIFICATION_TYPES.REFUND_REJECTED]: {
    Icon: ShieldAlert,
    bg: "bg-rose-100 text-rose-700",
    label: "Refund rejected",
  },
  [NOTIFICATION_TYPES.REFUND_TRANSFERRED]: {
    Icon: Banknote,
    bg: "bg-emerald-100 text-emerald-700",
    label: "Refund transferred",
  },

  // Staff & shop invitations
  [NOTIFICATION_TYPES.STAFF_INVITATION_RECEIVED]: {
    Icon: UserPlus,
    bg: "bg-indigo-100 text-indigo-700",
    label: "Staff invitation received",
  },
  [NOTIFICATION_TYPES.STAFF_INVITATION_ACCEPTED]: {
    Icon: UserCheck,
    bg: "bg-emerald-100 text-emerald-700",
    label: "Staff invitation accepted",
  },
  [NOTIFICATION_TYPES.STAFF_INVITATION_DECLINED]: {
    Icon: UserMinus,
    bg: "bg-neutral-100 text-neutral-700",
    label: "Staff invitation declined",
  },
  [NOTIFICATION_TYPES.STAFF_INVITATION_EXPIRED]: {
    Icon: UserX,
    bg: "bg-neutral-100 text-neutral-700",
    label: "Staff invitation expired",
  },
  [NOTIFICATION_TYPES.STAFF_JOINED_SHOP]: {
    Icon: Users,
    bg: "bg-indigo-100 text-indigo-700",
    label: "Staff joined shop",
  },
  [NOTIFICATION_TYPES.STAFF_STATUS_CHANGED]: {
    Icon: UserCog,
    bg: "bg-purple-100 text-purple-700",
    label: "Staff status updated",
  },
  [NOTIFICATION_TYPES.STAFF_LEFT_SHOP]: {
    Icon: UserMinus,
    bg: "bg-neutral-100 text-neutral-700",
    label: "Staff left shop",
  },

  // Shop registration & status
  [NOTIFICATION_TYPES.SHOP_APPLICATION_SUBMITTED]: {
    Icon: Store,
    bg: "bg-blue-100 text-blue-700",
    label: "New shop application submitted",
  },
  [NOTIFICATION_TYPES.SHOP_APPLICATION_APPROVED]: {
    Icon: Store,
    bg: "bg-emerald-100 text-emerald-700",
    label: "Shop application approved",
  },
  [NOTIFICATION_TYPES.SHOP_APPLICATION_REJECTED]: {
    Icon: Store,
    bg: "bg-rose-100 text-rose-700",
    label: "Shop application rejected",
  },
  [NOTIFICATION_TYPES.SHOP_APPLICATION_NEEDS_REVISION]: {
    Icon: FileEdit,
    bg: "bg-amber-100 text-amber-700",
    label: "Shop application revision needed",
  },
  [NOTIFICATION_TYPES.SHOP_STATUS_CHANGED]: {
    Icon: Building2,
    bg: "bg-amber-100 text-amber-700",
    label: "Shop status updated",
  },

  // Deals & products
  [NOTIFICATION_TYPES.FLASH_DEAL_AVAILABLE]: {
    Icon: Zap,
    bg: "bg-amber-100 text-amber-700",
    label: "Flash deal available",
  },
  [NOTIFICATION_TYPES.PRODUCT_NEEDS_REVISION]: {
    Icon: FileWarning,
    bg: "bg-amber-100 text-amber-700",
    label: "Product revision needed",
  },
  [NOTIFICATION_TYPES.PRODUCT_CONTENT_APPROVED]: {
    Icon: CheckCheck,
    bg: "bg-emerald-100 text-emerald-700",
    label: "Product approved",
  },
  [NOTIFICATION_TYPES.PRODUCT_REMOVED]: {
    Icon: Trash2,
    bg: "bg-rose-100 text-rose-700",
    label: "Product removed",
  },

  // Reviews & feedback
  [NOTIFICATION_TYPES.FEEDBACK_RECEIVED]: {
    Icon: Star,
    bg: "bg-amber-100 text-amber-700",
    label: "Review received",
  },
  [NOTIFICATION_TYPES.FEEDBACK_REPLIED]: {
    Icon: MessageSquareQuote,
    bg: "bg-blue-100 text-blue-700",
    label: "Review replied",
  },
  [NOTIFICATION_TYPES.FEEDBACK_REMOVED_BY_ADMIN]: {
    Icon: MessageSquareX,
    bg: "bg-rose-100 text-rose-700",
    label: "Review removed by moderation",
  },

  // Trust & penalties
  [NOTIFICATION_TYPES.TRUST_SCORE_CHANGED]: {
    Icon: Shield,
    bg: "bg-cyan-100 text-cyan-700",
    label: "Trust score updated",
  },
  [NOTIFICATION_TYPES.TRUST_LEVEL_CHANGED]: {
    Icon: ShieldCheck,
    bg: "bg-blue-100 text-blue-700",
    label: "Trust level updated",
  },
  [NOTIFICATION_TYPES.NO_SHOW_PENALTY_CREATED]: {
    Icon: AlertTriangle,
    bg: "bg-rose-100 text-rose-700",
    label: "Penalty recorded",
  },
  [NOTIFICATION_TYPES.ACCOUNT_STATUS_CHANGED]: {
    Icon: ShieldAlert,
    bg: "bg-rose-100 text-rose-700",
    label: "Account status updated",
  },

  // Platform fees
  [NOTIFICATION_TYPES.PLATFORM_FEE_STATEMENT_ISSUED]: {
    Icon: FileText,
    bg: "bg-blue-100 text-blue-700",
    label: "Platform fee statement issued",
  },
  [NOTIFICATION_TYPES.PLATFORM_FEE_DUE_SOON]: {
    Icon: AlertCircle,
    bg: "bg-amber-100 text-amber-700",
    label: "Platform fee due soon",
  },
  [NOTIFICATION_TYPES.PLATFORM_FEE_OVERDUE]: {
    Icon: AlertOctagon,
    bg: "bg-rose-100 text-rose-700",
    label: "Platform fee overdue",
  },
  [NOTIFICATION_TYPES.PLATFORM_FEE_SCHEDULE_ACTIVATED]: {
    Icon: Receipt,
    bg: "bg-blue-100 text-blue-700",
    label: "Fee schedule activated",
  },
  [NOTIFICATION_TYPES.PLATFORM_FEE_RATE_CHANGED]: {
    Icon: Receipt,
    bg: "bg-amber-100 text-amber-700",
    label: "Commission rate changed",
  },
  [NOTIFICATION_TYPES.PLATFORM_FEE_PAID]: {
    Icon: Receipt,
    bg: "bg-emerald-100 text-emerald-700",
    label: "Platform fee paid",
  },
};

const DEFAULT_NOTIFICATION_VISUAL: NotificationVisualConfig = {
  Icon: Bell,
  bg: "bg-primary-100 text-primary-700",
  label: "Notification",
};

/**
 * Resolves the visual presentation (icon, badge style, label) for any notification type.
 */
export function getNotificationVisual(
  type: number | string | null | undefined,
): NotificationVisualConfig {
  if (type === null || type === undefined) {
    return DEFAULT_NOTIFICATION_VISUAL;
  }

  // Handle numeric id lookup
  if (
    typeof type === "number" ||
    (!Number.isNaN(Number(type)) && !Number.isNaN(parseFloat(String(type))))
  ) {
    const numId = Number(type);
    const mappedType = NOTIFICATION_TYPE_BY_ID[numId];
    if (mappedType && NOTIFICATION_VISUALS[mappedType]) {
      return NOTIFICATION_VISUALS[mappedType];
    }
  }

  const strKey = String(type).trim();

  // Exact key lookup
  if (strKey in NOTIFICATION_VISUALS) {
    return NOTIFICATION_VISUALS[strKey as NotificationType];
  }

  // Case-insensitive lookup
  const lowerKey = strKey.toLowerCase();
  for (const [key, visual] of Object.entries(NOTIFICATION_VISUALS)) {
    if (key.toLowerCase() === lowerKey) {
      return visual;
    }
  }

  return DEFAULT_NOTIFICATION_VISUAL;
}
