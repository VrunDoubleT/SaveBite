import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { CheckoutOrder, OrderHistoryPage } from "@/features/checkout/types/checkout.types";

export const orderApi = {
  async orderPage(
    status: string,
    cursor?: string | null,
    signal?: AbortSignal,
  ): Promise<OrderHistoryPage> {
    return (
      await httpClient.get<ApiResponse<OrderHistoryPage>>(API_ENDPOINTS.ORDERS.PAGE, {
        params: { status, cursor: cursor ?? undefined, pageSize: 10 },
        signal,
      })
    ).data.data;
  },
  async orders(signal?: AbortSignal): Promise<CheckoutOrder[]> {
    return (
      await httpClient.get<ApiResponse<CheckoutOrder[]>>(API_ENDPOINTS.ORDERS.BASE, { signal })
    ).data.data;
  },
  async order(orderId: string, signal?: AbortSignal): Promise<CheckoutOrder> {
    return (
      await httpClient.get<ApiResponse<CheckoutOrder>>(API_ENDPOINTS.ORDERS.DETAIL(orderId), {
        signal,
      })
    ).data.data;
  },
  async cancel(orderId: string): Promise<CheckoutOrder> {
    return (
      await httpClient.post<ApiResponse<CheckoutOrder>>(API_ENDPOINTS.ORDERS.CANCEL(orderId))
    ).data.data;
  },
};
