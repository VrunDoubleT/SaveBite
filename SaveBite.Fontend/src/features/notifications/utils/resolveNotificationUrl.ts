import type { NotificationItem } from "../types/notification.types";

// ShopStatusChanged (NotificationType = 29) is always sent to the shop owner.
const SHOP_STATUS_CHANGED = 29;
const LEGACY_STAFF_WORKSPACE_URL = "/account/workspace";
const OWNER_SHOP_PROFILE_URL = "/store-owner/profile";

/**
 * FIX-27: notifications created before the backend fix still point shop-status changes
 * (e.g. "shop locked") to the staff workspace list, which is not the owner's workspace.
 * Send the owner to their own shop profile instead.
 */
export function resolveNotificationUrl(item: NotificationItem): string | null {
  const url = item.actionUrl;
  if (!url) return null;
  const isShopStatus = item.type === SHOP_STATUS_CHANGED || item.type === "ShopStatusChanged";
  if (isShopStatus && url === LEGACY_STAFF_WORKSPACE_URL) return OWNER_SHOP_PROFILE_URL;
  return url;
}
