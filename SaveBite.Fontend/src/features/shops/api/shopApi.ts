import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  NearbyShop,
  NearbyShopsRequest,
  ShopProfile,
  StoreReviewsQueryRequest,
  StoreReviewsSummary,
} from "@/features/shops/types/shop.types";

export const shopApi = {
  async getNearbyShops(
    params: NearbyShopsRequest,
  ): Promise<{ shops: NearbyShop[]; message?: string }> {
    const response = await httpClient.get<ApiResponse<NearbyShop[]>>(
      API_ENDPOINTS.SHOPS.NEARBY,
      { params },
    );
    return {
      shops: response.data.data ?? [],
      message: response.data.message,
    };
  },

  async getShopProfile(id: string): Promise<ShopProfile> {
    const response = await httpClient.get<ApiResponse<ShopProfile>>(
      API_ENDPOINTS.SHOPS.PROFILE(id),
    );
    return response.data.data;
  },

  async getStoreReviews(
    id: string,
    params?: StoreReviewsQueryRequest,
  ): Promise<StoreReviewsSummary> {
    const response = await httpClient.get<ApiResponse<StoreReviewsSummary>>(
      API_ENDPOINTS.SHOPS.REVIEWS(id),
      { params },
    );
    return response.data.data;
  },
};
