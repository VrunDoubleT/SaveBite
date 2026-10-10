import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { ShopApplication } from "../types/adminShopApplication.types";
import type { ShopApplicationReviewDecision } from "../types/adminShopApplication.types";

export const adminShopApplicationApi = {
  async getAll(): Promise<ShopApplication[]> {
    const response = await httpClient.get<ApiResponse<ShopApplication[]>>(
      API_ENDPOINTS.ADMIN_SHOP_APPLICATION.BASE,
    );
    return response.data.data ?? [];
  },

  async getById(id: ShopApplication["id"]): Promise<ShopApplication> {
    const response = await httpClient.get<ApiResponse<ShopApplication>>(
      API_ENDPOINTS.ADMIN_SHOP_APPLICATION.DETAIL(id),
    );
    return response.data.data;
  },

  async review(
    id: ShopApplication["id"],
    decision: ShopApplicationReviewDecision,
    note: string | null,
  ): Promise<ShopApplication> {
    const response = await httpClient.post<ApiResponse<ShopApplication>>(
      API_ENDPOINTS.ADMIN_SHOP_APPLICATION.REVIEW(id),
      { decision, note },
    );
    return response.data.data;
  },
};
