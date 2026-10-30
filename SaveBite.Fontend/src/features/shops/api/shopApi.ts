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
import type {
  ShopContentManagement,
  ShopListManagement,
  UpdateShopModerationStatus,
} from "../types/shopManagement.types";
import type { ShopStatus } from "../types/ownerShop.types";
import type { ModerationActionResult } from "@/shared/types/moderationAction.types";

export const shopApi = {
  async getNearbyShops(params: NearbyShopsRequest): Promise<{
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

  // Shop Management
  async getShopsForManagement(
    params: { keyword?: string; page: number; pageSize: number },
    signal?: AbortSignal,
  ) {
    const response = await httpClient.get<
      ApiResponse<{
        items: ShopListManagement[];
        page: number;
        pageSize: number;
        totalItems: number;
        totalPages: number;
      }>
    >(API_ENDPOINTS.SHOPS.ADMIN_LIST, {
      params,
      signal,
    });

    return response.data.data;
  },

  // Shop Content Management
  async getShopContentForManagement(
    id: string,
    signal?: AbortSignal,
  ): Promise<ShopContentManagement> {
    const response = await httpClient.get<ApiResponse<ShopContentManagement>>(
      API_ENDPOINTS.SHOPS.ADMIN_CONTENT(id),
      {
        signal,
      },
    );

    return response.data.data;
  },

  // Shop Moderation Status
  async updateModerationStatus(
    shopId: string,
    status: ShopStatus,
    reason?: string | null,
  ): Promise<ModerationActionResult> {
    const request: UpdateShopModerationStatus = {
      status,
      ...(status !== "Active" && { reason: reason?.trim() }),
    };
    const response = await httpClient.patch<ApiResponse<ModerationActionResult>>(
      API_ENDPOINTS.SHOPS.MODERATION_STATUS(shopId),
      request,
    );
    return response.data.data;
  },
};
