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

  // --- Store Owner Flash Deals APIs ---

  async getShopFlashDeals(
    params: import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealQueryParams,
  ): Promise<{
    items: import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealListItem[];
    totalItems: number;
    page: number;
    pageSize: number;
  }> {
    const response = await httpClient.get<
      ApiResponse<{
        items: import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealListItem[];
        totalItems: number;
        page: number;
        pageSize: number;
      }>
    >(API_ENDPOINTS.FLASH_DEALS.SHOP_LIST, { params });
    return (
      response.data.data ?? {
        items: [],
        totalItems: 0,
        page: 1,
        pageSize: 10,
      }
    );
  },

  async getShopFlashDealDetail(
    id: string,
    params?: { shopId?: string },
  ): Promise<import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealDetail> {
    const response = await httpClient.get<
      ApiResponse<import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealDetail>
    >(API_ENDPOINTS.FLASH_DEALS.SHOP_DETAIL(id), { params });
    return response.data.data;
  },

  async getEligibleProducts(): Promise<
    import("@/features/flash-deals/types/flashDeal.types").ShopEligibleProduct[]
  > {
    const response = await httpClient.get<
      ApiResponse<import("@/features/flash-deals/types/flashDeal.types").ShopEligibleProduct[]>
    >(API_ENDPOINTS.FLASH_DEALS.ELIGIBLE_PRODUCTS);
    return response.data.data ?? [];
  },

  async createFlashDeal(
    payload: import("@/features/flash-deals/types/flashDeal.types").CreateFlashDealPayload,
  ): Promise<import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealDetail> {
    const response = await httpClient.post<
      ApiResponse<import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealDetail>
    >(API_ENDPOINTS.FLASH_DEALS.CREATE, payload);
    return response.data.data;
  },

  async updateFlashDeal(
    id: string,
    payload: import("@/features/flash-deals/types/flashDeal.types").UpdateFlashDealPayload,
  ): Promise<import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealDetail> {
    const response = await httpClient.put<
      ApiResponse<import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealDetail>
    >(API_ENDPOINTS.FLASH_DEALS.UPDATE(id), payload);
    return response.data.data;
  },

  async cancelFlashDeal(id: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.FLASH_DEALS.CANCEL(id));
  },

  async hideFlashDeal(id: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.FLASH_DEALS.HIDE(id));
  },

  async unhideFlashDeal(id: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.FLASH_DEALS.UNHIDE(id));
  },
};
