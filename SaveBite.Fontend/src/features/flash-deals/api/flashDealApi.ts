import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";

import type {
  FlashDeal,
  NearbyFlashDealsParams,
} from "@/features/flash-deals/types/flashDeal.types";

export const flashDealApi = {
  async getNearbyDeals(
    params: NearbyFlashDealsParams,
  ): Promise<{ deals: FlashDeal[]; message?: string }> {
    const response = await httpClient.get<ApiResponse<FlashDeal[]>>(
      API_ENDPOINTS.FLASH_DEALS.NEARBY,
      {
        params,
      },
    );
    return {
      deals: response.data.data ?? [],
      message: response.data.message,
    };
  },

  async getDealsByShop(shopId: string): Promise<FlashDeal[]> {
    const response = await httpClient.get<ApiResponse<FlashDeal[]>>(
      API_ENDPOINTS.FLASH_DEALS.BY_SHOP(shopId),
    );
    return response.data.data ?? [];
  },

  async getDealById(id: string): Promise<FlashDeal> {
    const response = await httpClient.get<ApiResponse<FlashDeal>>(
      API_ENDPOINTS.FLASH_DEALS.DETAIL(id),
    );
    return response.data.data;
  },
};
