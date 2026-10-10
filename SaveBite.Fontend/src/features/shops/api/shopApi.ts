import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  NearbyShop,
  NearbyShopsPagedResponse,
  NearbyShopsRequest,
  ShopProfile,
  StoreReviewsQueryRequest,
  StoreReviewsSummary,
} from "@/features/shops/types/shop.types";

export const shopApi = {
  async getNearbyShops(
    params: NearbyShopsRequest,
  ): Promise<{
    shops: NearbyShop[];
    totalItems: number;
    totalPages: number;
    page: number;
    pageSize: number;
    message?: string;
  }> {
    const response = await httpClient.get<ApiResponse<NearbyShopsPagedResponse>>(
      API_ENDPOINTS.SHOPS.NEARBY,
      { params },
    );
    const data = response.data.data;
    return {
      shops: data?.items ?? [],
      totalItems: data?.totalItems ?? 0,
      totalPages: data?.totalPages ?? 0,
      page: data?.page ?? 1,
      pageSize: data?.pageSize ?? 6,
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
