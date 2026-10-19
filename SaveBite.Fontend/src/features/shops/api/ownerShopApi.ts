import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";

import type {
  OwnerShop,
  ShopPaymentConfig,
  UpdateBusinessInfo,
  UpdateShopPaymentConfigRequest,
} from "../types/ownerShop.types";
import type { ShopPaymentConfigView } from "../schemas/ownerShop.schema";

export type { OwnerShop } from "../types/ownerShop.types";
export type { ShopPaymentConfig, UpdateShopPaymentConfigRequest } from "../types/ownerShop.types";

export const ownerShopApi = {
  async getMyShop() {
    return httpClient.get<ApiResponse<OwnerShop>>(API_ENDPOINTS.SHOPS.ME);
  },

  async getMyShopProfile() {
    const response = await httpClient.get<ApiResponse<OwnerShop>>(API_ENDPOINTS.SHOPS.ME);
    return response.data.data;
  },

  async updateBusinessInfo(input: UpdateBusinessInfo) {
    return httpClient.put<ApiResponse<null>>(API_ENDPOINTS.SHOPS.BUSINESS_INFO, input);
  },

  async updateImages(input: { logo?: File | null; coverImage?: File | null }) {
    const formData = new FormData();
    if (input.logo) formData.append("Logo", input.logo);
    if (input.coverImage) formData.append("CoverImage", input.coverImage);
    return httpClient.put<ApiResponse<null>>(API_ENDPOINTS.SHOPS.IMAGES, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
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