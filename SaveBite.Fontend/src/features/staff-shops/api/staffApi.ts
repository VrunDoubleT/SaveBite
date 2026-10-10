import { httpClient } from "@/shared/api/httpClient";
import { API_ENDPOINTS } from "@/shared/api/endpoints";
import type { ApiResponse } from "@/shared/types";

import type {
  AssociatedShop,
  StaffShopDetail,
} from "../types/staff.types";

let associatedShopsRequest: Promise<AssociatedShop[]> | null = null;

export const staffApi = {
  async getAssociatedShops(): Promise<AssociatedShop[]> {
    if (associatedShopsRequest) {
      return associatedShopsRequest;
    }

    associatedShopsRequest = httpClient
      .get<ApiResponse<AssociatedShop[]>>(
        API_ENDPOINTS.STAFF_SHOPS.LIST,
      )
      .then((response) => response.data.data)
      .finally(() => {
        associatedShopsRequest = null;
      });

    return associatedShopsRequest;
  },

  async getShopDetail(shopId: string) {
    return httpClient.get<ApiResponse<StaffShopDetail>>(
      API_ENDPOINTS.STAFF_SHOPS.DETAIL(shopId),
    );
  },

  async leaveShop(shopId: string): Promise<void> {
    await httpClient.delete(
      API_ENDPOINTS.STAFF_SHOPS.DETAIL(shopId),
    );
  },
};