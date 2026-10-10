import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";

import type { OwnerShop } from "../types/ownerShop.types";

export type { OwnerShop } from "../types/ownerShop.types";

export const shopApi = {
  async getMyShop() {
    return httpClient.get<ApiResponse<OwnerShop>>(API_ENDPOINTS.SHOPS.ME);
  },
};
