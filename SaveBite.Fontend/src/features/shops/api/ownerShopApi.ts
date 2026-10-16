import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";

import type { OwnerShop } from "../types/ownerShop.types";
import type { ShopPaymentConfig, UpdateShopPaymentConfigRequest } from "../types/ownerShop.types";
import type { ShopPaymentConfigView } from "../schemas/ownerShop.schema";

export type { OwnerShop } from "../types/ownerShop.types";
export type { ShopPaymentConfig, UpdateShopPaymentConfigRequest } from "../types/ownerShop.types";

export const ownerShopApi = {
  async getMyShop() {
    return httpClient.get<ApiResponse<OwnerShop>>(API_ENDPOINTS.SHOPS.ME);
  },

  async getMyPaymentConfig(): Promise<ShopPaymentConfigView> {
    const response = await httpClient.get<ApiResponse<ShopPaymentConfig>>(
      API_ENDPOINTS.SHOPS.PAYMENT_CONFIG,
    );

    return response.data.data;
  },

  async updateMyPaymentConfig(payload: UpdateShopPaymentConfigRequest) {
    return httpClient.put<ApiResponse<ShopPaymentConfig>>(
      API_ENDPOINTS.SHOPS.UPDATE_PAYMENT_CONFIG,
      payload,
    );
  },
};
