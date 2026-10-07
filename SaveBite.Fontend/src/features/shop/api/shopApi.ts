import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";

export interface OwnerShop {
  id: string;
  name: string;
}

export const shopApi = {
  async getMyShop() {
    return httpClient.get<ApiResponse<OwnerShop>>(API_ENDPOINTS.OWNER_SHOPS.ME);
  },
};
