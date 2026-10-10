import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";

import type {
  FlashDeal,
  FlashDealCursorParams,
  FlashDealCursorResult,
} from "@/features/flash-deals/types/flashDeal.types";

export const flashDealApi = {
  async getNearbyDeals(
    params: FlashDealCursorParams,
  ): Promise<FlashDealCursorResult> {
    const response = await httpClient.get<ApiResponse<FlashDealCursorResult>>(
      API_ENDPOINTS.FLASH_DEALS.NEARBY,
      {
        params,
      },
    );
    const data = response.data.data;
    return {
      deals: data?.deals ?? [],
      cursor1: data?.cursor1,
      cursor2: data?.cursor2,
      hasOlder: data?.hasOlder ?? false,
      hasNewer: data?.hasNewer ?? false,
      prependedCount: data?.prependedCount ?? 0,
      newDealsCount: data?.newDealsCount ?? 0,
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

  async getCategories(): Promise<string[]> {
    try {
      const response = await httpClient.get<ApiResponse<{ name: string }[]>>(
        API_ENDPOINTS.CATEGORIES.CATALOG,
      );
      return (response.data.data ?? []).map((c) => c.name);
    } catch {
      return [];
    }
  },
};
