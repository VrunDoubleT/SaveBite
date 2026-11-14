import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { Cart, CartPage, CartSummary, CartMutationResponse } from "../types/cart.types";

export const cartApi = {
  async getPage(cursor?: string | null, signal?: AbortSignal): Promise<CartPage> {
    return (
      await httpClient.get<ApiResponse<CartPage>>(API_ENDPOINTS.CART.PAGE, {
        params: { cursor: cursor ?? undefined, pageSize: 10 },
        signal,
      })
    ).data.data;
  },
  async summary(signal?: AbortSignal): Promise<CartSummary> {
    return (await httpClient.get<ApiResponse<CartSummary>>(API_ENDPOINTS.CART.SUMMARY, { signal }))
      .data.data;
  },
  async addItem(flashDealVariantId: string, quantity: number): Promise<Cart> {
    return (
      await httpClient.post<ApiResponse<Cart>>(API_ENDPOINTS.CART.ITEMS, {
        flashDealVariantId,
        quantity,
      })
    ).data.data;
  },
  async updateQuantity(itemId: string, quantity: number): Promise<CartMutationResponse> {
    return (
      await httpClient.put<ApiResponse<CartMutationResponse>>(API_ENDPOINTS.CART.ITEM(itemId), {
        quantity,
      })
    ).data.data;
  },
  async removeItem(itemId: string): Promise<CartMutationResponse> {
    return (
      await httpClient.delete<ApiResponse<CartMutationResponse>>(API_ENDPOINTS.CART.ITEM(itemId))
    ).data.data;
  },
};
