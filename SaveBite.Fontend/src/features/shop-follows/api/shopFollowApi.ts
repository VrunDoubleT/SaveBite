import { httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";

export type ShopNotificationStatus = "Enabled" | "Disabled";
export interface ShopFollow {
  shopId: string;
  shopName: string;
  logoUrl: string | null;
  addressLine: string;
  shopStatus: string;
  notificationStatus: ShopNotificationStatus;
  followedAt: string;
}
export interface FollowedShopsPage {
  items: ShopFollow[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
const followPath = (id: string) => `/shops/${encodeURIComponent(id)}/follow`;
export const shopFollowApi = {
  async list(page = 1, pageSize = 12, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<FollowedShopsPage>>("/me/followed-shops", {
        params: { page, pageSize },
        signal,
      })
    ).data.data;
  },
  // The current API exposes follow state through the paginated list only.
  async find(shopId: string, signal: AbortSignal): Promise<ShopFollow | null> {
    let page = 1;
    while (!signal.aborted) {
      const result = await shopFollowApi.list(page, 100, signal);
      const follow = result.items.find((item) => item.shopId === shopId);
      if (follow) return follow;
      if (page >= result.totalPages) return null;
      page += 1;
    }
    throw new DOMException("Request cancelled", "AbortError");
  },
  async follow(id: string) {
    return (await httpClient.put<ApiResponse<ShopFollow>>(followPath(id))).data.data;
  },
  async unfollow(id: string) {
    await httpClient.delete(followPath(id));
  },
  async settings(id: string, notificationStatus: ShopNotificationStatus) {
    return (
      await httpClient.put<ApiResponse<ShopFollow>>(`${followPath(id)}/notifications`, {
        notificationStatus,
      })
    ).data.data;
  },
};
