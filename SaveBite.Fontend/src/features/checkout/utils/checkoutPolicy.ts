import type { CheckoutContext, CheckoutMethod, CheckoutQuote } from "../types/checkout.types";

export const checkoutTime = (value?: string) =>
  value
    ? new Intl.DateTimeFormat("en-GB", {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
        day: "2-digit",
        month: "2-digit",
        timeZone: "Asia/Bangkok",
      }).format(new Date(value))
    : "—";

export function restrictionMessage(context: CheckoutContext): string {
  switch (context.policy.restrictionCode) {
    case "LevelOnlineOnly":
      return `Level ${context.policy.levelRank ?? ""} · ${context.policy.levelName} requires full online payment. Deposits and Pay at shop are unavailable at this level.`;
    case "PermanentOfflineLock":
      return "Pay at shop is permanently disabled for your account. You must pay the full order amount online.";
    case "RecoveryRequired":
      return "Complete the required online orders to restore access to Pay at shop.";
    case "AboveDepositMaximum":
      return "Your order exceeds the maximum value eligible for a deposit at your current level. Full online payment is required.";
    default:
      return "The deposit policy for your current level is unavailable. This order requires full online payment.";
  }
}

export function checkoutSelection(
  context: CheckoutContext | null,
  selected: CheckoutMethod,
  quote: CheckoutQuote | null,
  now: number,
) {
  const method: CheckoutMethod = context?.policy.payAtShopAllowed ? selected : "OnlinePayment";
  const total = context?.shop.subtotal ?? 0;
  const dueOnline = method === "OnlinePayment" ? total : (context?.policy.requiredDeposit ?? 0);
  const remainingAtShop = method === "PayAtShop" ? total - dueOnline : 0;
  const expired =
    !quote || !Number.isFinite(Date.parse(quote.expiresAt)) || Date.parse(quote.expiresAt) <= now;
  const sameQuote = quote !== null && context !== null && context.shop.shopId === quote.shop.shopId;
  return {
    method,
    dueOnline,
    remainingAtShop,
    expired,
    canSubmit:
      sameQuote &&
      !expired &&
      context.shop.canCheckout &&
      (dueOnline === 0 || context.onlinePaymentAvailable),
  };
}

export function safePaymentUrl(value?: string): string | null {
  if (!value) return null;
  if (
    /^\/checkout\/simulation\?orderId=[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(?:&paymentId=[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})?$/i.test(
      value,
    )
  )
    return value;
  try {
    const url = new URL(value);
    return url.protocol === "https:" &&
      !url.username &&
      !url.password &&
      (url.hostname === "payos.vn" || url.hostname.endsWith(".payos.vn"))
      ? url.href
      : null;
  } catch {
    return null;
  }
}
