import type { AxiosResponse } from "axios";
import { httpClient } from "@/shared/api/httpClient";
import { API_ENDPOINTS } from "@/shared/api/endpoints";
import type { ApiResponse } from "@/shared/types";

import type { AssociatedShop, StaffShopDetail } from "../types/staff.types";
import { useAuthStore } from "@/shared/stores/authStore";

let associatedShopsRequest: Promise<AssociatedShop[]> | null = null;

const shopDetailRequests = new Map<string, Promise<AxiosResponse<ApiResponse<StaffShopDetail>>>>();

export const staffApi = {
  async getAssociatedShops(forceRefresh = false): Promise<AssociatedShop[]> {
    if (associatedShopsRequest && !forceRefresh) {
      return associatedShopsRequest;
    }

    const request = httpClient
      .get<ApiResponse<AssociatedShop[]>>(API_ENDPOINTS.STAFF_SHOPS.LIST)
      .then((response) => response.data.data)
      .finally(() => {
        if (associatedShopsRequest === request) associatedShopsRequest = null;
      });

    associatedShopsRequest = request;
    return request;
  },

  async getShopDetail(shopId: string): Promise<AxiosResponse<ApiResponse<StaffShopDetail>>> {
    const existingRequest = shopDetailRequests.get(shopId);
    if (existingRequest) {
      return existingRequest;
    }

    const request = httpClient
      .get<ApiResponse<StaffShopDetail>>(API_ENDPOINTS.STAFF_SHOPS.DETAIL(shopId))
      .finally(() => {
        shopDetailRequests.delete(shopId);
      });

    shopDetailRequests.set(shopId, request);
    return request;
  },

  async leaveShop(shopId: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.STAFF_SHOPS.DETAIL(shopId));
    await useAuthStore.getState().refreshUser();
  },
};
