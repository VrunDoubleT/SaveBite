import {
  Activity,
  Ban,
  CheckCircle2,
  EyeOff,
  Eye,
  PencilLine,
  PlusCircle,
  Trash2,
  Store,
  Package,
  ShoppingBag,
  CreditCard,
  Banknote,
  Users,
  UserCog,
  Shield,
  ShieldAlert,
  ShieldCheck,
  Award,
  Tags,
  Zap,
  MessageSquare,
  MailCheck,
  KeyRound,
  Receipt,
  CalendarClock,
  FileArchive,
  Download,
  Clock,
  AlertTriangle,
  type LucideIcon,
} from "lucide-react";
import type { Tone } from "@/shared/components/analytics";

const VN_TIME_ZONE = "Asia/Ho_Chi_Minh";

/** Turns FLASH_DEAL_STOPPED or UpdateTrustLevelBenefits into "Flash deal stopped" / "Update trust level benefits". */
function humanizeAuditAction(action: string): string {
  const words = action.includes("_")
    ? action.split("_")
    : action.replace(/([a-z0-9])([A-Z])/g, "$1 $2").split(" ");
  const sentence = words
    .filter(Boolean)
    .map((w) => w.toLowerCase())
    .join(" ");
  return sentence.charAt(0).toUpperCase() + sentence.slice(1);
}

export function shortId(id: string): string {
  return id.length > 14 ? `${id.slice(0, 8)}…${id.slice(-4)}` : id;
}

/** yyyy-MM-dd of the entry in Vietnam time, used to group the list by day. */
export function auditDayKey(iso: string): string {
  return new Date(iso).toLocaleDateString("en-CA", { timeZone: VN_TIME_ZONE });
}

export function formatAuditDayHeading(iso: string): string {
  // English heading, e.g. "Friday 23 October 2026", still on the Vietnam calendar day.
  return new Date(iso).toLocaleDateString("en-GB", {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: VN_TIME_ZONE,
  });
}

export function formatAuditTime(iso: string): string {
  return new Date(iso).toLocaleTimeString("en-GB", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
    timeZone: VN_TIME_ZONE,
  });
}

export interface AuditActionStyle {
  tone: Tone;
  icon: LucideIcon;
}

interface AuditActionVisual extends AuditActionStyle {
  label: string;
}

// Exact action mappings take precedence over the fallback for unknown future actions.
const ACTION_VISUALS: Record<string, AuditActionVisual> = {};
function actions(names: string[], icon: LucideIcon, tone: Tone) {
  for (const name of names) ACTION_VISUALS[name] = { icon, tone, label: humanizeAuditAction(name) };
}
actions(["CATEGORY_CREATED", "CATEGORY_UPDATED", "CATEGORY_ACTIVATED"], Tags, "primary");
actions(["CATEGORY_DEACTIVATED", "CATEGORY_DELETED"], Tags, "danger");
actions(
  [
    "SHOP_CREATE",
    "SHOP_UPDATE",
    "SHOP_STATUS_UPDATED",
    "SHOP_APPLICATION_REVIEWED",
    "SHOP_APPLICATION_SUBMITTED",
    "SHOP_APPLICATION_APPROVED",
  ],
  Store,
  "info",
);
actions(
  [
    "SHOP_DELETE",
    "SHOP_WARNING_ISSUED",
    "SHOP_APPLICATION_REJECTED",
    "SHOP_APPLICATION_NEEDS_REVISION",
  ],
  Store,
  "danger",
);
actions(["PRODUCT_CREATE", "PRODUCT_UPDATE", "PRODUCT_CONTENT_APPROVED"], Package, "primary");
actions(
  [
    "PRODUCT_DELETE",
    "PRODUCT_REMOVED_VIOLATION",
    "PRODUCT_WARNING_ISSUED",
    "PRODUCT_WARNING_DEADLINE_BLOCKED",
  ],
  Package,
  "danger",
);
actions(
  ["FLASH_DEAL_CREATE", "FLASH_DEAL_CREATED", "FLASH_DEAL_UPDATE", "FLASH_DEAL_STATUS_ON_SALE"],
  Zap,
  "primary",
);
actions(
  ["FLASH_DEAL_DELETE", "FLASH_DEAL_STATUS_STOPPED", "FLASH_DEAL_STATUS_EXPIRED"],
  Zap,
  "danger",
);
actions(["FEEDBACK_HIDDEN"], EyeOff, "neutral");
actions(["FEEDBACK_RESTORED"], Eye, "primary");
actions(["PRODUCT_FEEDBACK_CREATE", "PRODUCT_FEEDBACK_UPDATE"], MessageSquare, "info");
actions(
  ["SUSPEND_CUSTOMER_FEATURES", "SUSPEND_CUSTOMER_ACCESS", "SUSPEND_ACCOUNT"],
  ShieldAlert,
  "danger",
);
actions(["REACTIVATE_CUSTOMER_FEATURES", "REACTIVATE_ACCOUNT"], ShieldCheck, "primary");
actions(["USER_ROLE_CHANGED", "USER_UPDATE"], UserCog, "info");
actions(
  ["STAFF_INVITATION_UPDATE", "STAFF_INVITATION_CREATE", "SHOP_STAFF_CREATE", "SHOP_STAFF_UPDATE"],
  Users,
  "info",
);
actions(
  ["UPDATE_SCHEDULED_FEE_CONFIGURATION", "CREATE_SCHEDULED_FEE_CONFIGURATION"],
  CalendarClock,
  "info",
);
actions(["CANCEL_SCHEDULED_FEE_CONFIGURATION"], CalendarClock, "danger");
actions(["UPDATE_FEE_CONFIGURATION", "GENERATE_MONTHLY_PLATFORM_FEE_STATEMENTS"], Receipt, "info");
actions(["PROCESS_OVERDUE_PLATFORM_FEES"], Receipt, "danger");
actions(["RECORD_EXTERNAL_PLATFORM_FEE_PAYMENT"], Banknote, "primary");
actions(["PLATFORM_PAYOS_CONFIG_UPDATED", "SHOP_PAYMENT_CONFIG_UPDATED"], CreditCard, "info");
actions(
  [
    "CREATE_TRUST_LEVEL",
    "UPDATE_TRUST_LEVEL_INFO",
    "UPDATE_TRUST_LEVEL_BENEFITS",
    "UPDATE_TRUST_LEVEL_REQUIREMENT",
    "USER_TRUST_LEVEL_LEVEL_CHANGED",
  ],
  Award,
  "info",
);
actions(["DEACTIVATE_TRUST_LEVEL"], Award, "danger");
actions(["REACTIVATE_TRUST_LEVEL"], Award, "primary");
actions(
  ["CREATE_PENALTY_RULE", "UPDATE_PENALTY_RULE", "USER_TRUST_SCORE_NO_SHOW"],
  ShieldAlert,
  "danger",
);
actions(["DELETE_PENALTY_RULE"], Trash2, "danger");
actions(
  ["UPDATE_TRUST_SCORE_RULE", "UPDATE_TRUST_EARNING_RULE", "USER_TRUST_SCORE_ORDER_COMPLETED"],
  Shield,
  "info",
);
actions(["RESOLVE_EMAIL_DELIVERY"], MailCheck, "primary");
actions(
  [
    "REFRESH_TOKEN_CREATE",
    "REFRESH_TOKEN_UPDATE",
    "REFRESH_TOKEN_ROTATED",
    "USER_REVOKE_REFRESH_TOKENS",
    "REVOKE_REFRESH_TOKENS",
  ],
  KeyRound,
  "neutral",
);
actions(["AUDIT_LOG_ARCHIVE_CREATED", "AUDIT_LOG_ARCHIVE_TEST_CREATED"], FileArchive, "primary");
actions(["AUDIT_LOG_ARCHIVE_DOWNLOADED"], Download, "info");
actions(["AUDIT_LOG_ARCHIVE_TEST_DELETED"], Trash2, "danger");
actions(
  [
    "REFUND_REQUESTED",
    "REFUND_DESTINATION_PROVIDED",
    "REFUND_DESTINATION_CONFIRMED",
    "REFUND_APPROVED",
    "REFUND_TRANSFERRED",
  ],
  Banknote,
  "info",
);
actions(["REFUND_REJECTED", "REFUND_WITHDRAWN", "REFUND_DEADLINE_EXPIRED"], Banknote, "danger");
actions(["PAYMENT_VERIFIED"], CreditCard, "primary");
actions(["PAYMENT_PENDING"], Clock, "neutral");
actions(
  ["PAYMENT_FAILED", "PAYMENT_CANCELLED", "PAYMENT_EXPIRED", "PAYMENT_VERIFIED_REVIEW_REQUIRED"],
  CreditCard,
  "danger",
);
actions(
  ["ORDER_CREATED", "ORDER_CREATE", "ORDER_STATUS_COMPLETED", "ORDER_STATUS_ACCEPTED"],
  ShoppingBag,
  "primary",
);
actions(
  [
    "ORDER_STATUS_NO_SHOW",
    "ORDER_STATUS_CANCELLED",
    "ORDER_STATUS_REJECTED",
    "ORDER_STATUS_EXPIRED",
  ],
  ShoppingBag,
  "danger",
);

export function auditActionVisual(action: string): AuditActionVisual {
  const key = action.replace(/([a-z0-9])([A-Z])/g, "$1_$2").toUpperCase();
  if (ACTION_VISUALS[key]) return ACTION_VISUALS[key];
  const words = key.split("_");
  const negative = words.some((w) =>
    [
      "DEACTIVATED",
      "DEACTIVATE",
      "SUSPENDED",
      "SUSPEND",
      "CANCELLED",
      "CANCEL",
      "REJECTED",
      "REJECT",
      "FAILED",
      "BLOCKED",
      "REMOVED",
      "DELETED",
      "DELETE",
      "EXPIRED",
    ].includes(w),
  );
  const tone: Tone = negative ? "danger" : "info";
  // Generated entity/status actions share the icon for their business function.
  const families: [string, LucideIcon][] = [
    ["ORDER_", ShoppingBag],
    ["REFUND_", Banknote],
    ["PAYMENT_", CreditCard],
    ["PLATFORM_FEE_", Receipt],
    ["SHOP_", Store],
    ["PRODUCT_", Package],
    ["FLASH_DEAL_", Zap],
    ["STAFF_", Users],
    ["USER_TRUST_", Shield],
    ["TRUST_", Award],
    ["AUDIT_LOG_ARCHIVE_", FileArchive],
    ["USER_", UserCog],
    ["CATEGORY_", Tags],
  ];
  for (const [prefix, icon] of families)
    if (key.startsWith(prefix)) return { icon, tone, label: humanizeAuditAction(action) };
  if (words.some((w) => ["DELETE", "DELETED", "REMOVE", "REMOVED"].includes(w)))
    return { icon: Trash2, tone: "danger", label: humanizeAuditAction(action) };
  if (words.some((w) => ["CREATE", "CREATED", "ADD", "ADDED"].includes(w)))
    return { icon: PlusCircle, tone: "primary", label: humanizeAuditAction(action) };
  if (words.some((w) => ["UPDATE", "UPDATED", "CHANGED", "EDIT", "CONFIG"].includes(w)))
    return { icon: PencilLine, tone, label: humanizeAuditAction(action) };
  if (words.some((w) => ["APPROVED", "REACTIVATED", "RESTORED"].includes(w)))
    return { icon: CheckCircle2, tone: "primary", label: humanizeAuditAction(action) };
  if (negative) return { icon: Ban, tone: "danger", label: humanizeAuditAction(action) };
  if (key.includes("WARNING"))
    return { icon: AlertTriangle, tone: "danger", label: humanizeAuditAction(action) };
  return { icon: Activity, tone: "neutral", label: humanizeAuditAction(action) };
}

export function auditActionStyle(action: string): AuditActionStyle {
  return auditActionVisual(action);
}

export function formatAuditAction(action: string): string {
  return auditActionVisual(action).label;
}
