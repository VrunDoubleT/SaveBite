import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { CheckoutContext, CheckoutGps, CheckoutMethod, CheckoutOrder, CheckoutQuote } from "../types/checkout.types";

export const checkoutApi = {
  async context(shopId: string, signal?: AbortSignal): Promise<CheckoutContext> {
    return (
      await httpClient.get<ApiResponse<CheckoutContext>>(API_ENDPOINTS.CHECKOUT.CONTEXT, {
        params: { shopId },
        signal,
      })
    ).data.data;
  },
  async quote(shopId: string, location: CheckoutGps): Promise<CheckoutQuote> {
    return (
      await httpClient.post<ApiResponse<CheckoutQuote>>(API_ENDPOINTS.CHECKOUT.QUOTES, {
        shopId,
        location,
      })
    ).data.data;
  },
  async place(
    quoteId: string,
    paymentMethod: CheckoutMethod,
    requestId: string,
  ): Promise<CheckoutOrder> {
    return (
      await httpClient.post<ApiResponse<CheckoutOrder>>(API_ENDPOINTS.CHECKOUT.PLACE, {
        quoteId,
        paymentMethod,
        requestId,
      })
    ).data.data;
  },
};
