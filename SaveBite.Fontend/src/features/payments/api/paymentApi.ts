import type { CustomerPaymentPage, CustomerPaymentDetail } from "../types/paymentHistory.types";
import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";

export const paymentApi = {
  async page(page: number, status: string, signal?: AbortSignal): Promise<CustomerPaymentPage> {
    return (await httpClient.get<ApiResponse<CustomerPaymentPage>>("/payments", { params: { page, pageSize: 10, status }, signal })).data.data;
  },
  async detail(paymentId: string, signal?: AbortSignal): Promise<CustomerPaymentDetail> {
    return (await httpClient.get<ApiResponse<CustomerPaymentDetail>>(`/payments/${encodeURIComponent(paymentId)}`, { signal })).data.data;
  },
  async simulation(
    orderId: string,
    signal?: AbortSignal,
    paymentId?: string,
  ): Promise<CheckoutOrder> {
    return (
      await httpClient.get<ApiResponse<CheckoutOrder>>(
        `/payments/simulation/${encodeURIComponent(orderId)}`,
        { signal, params: { paymentId } },
      )
    ).data.data;
  },
  async simulate(
    orderId: string,
    outcome: "paid" | "cancelled" | "expired",
    paymentId?: string,
  ): Promise<CheckoutOrder> {
    return (
      await httpClient.post<ApiResponse<CheckoutOrder>>(
        `/payments/simulation/${encodeURIComponent(orderId)}`,
        { outcome, paymentId },
      )
    ).data.data;
  },
  async payment(orderId: string): Promise<CheckoutOrder> {
    return (
      await httpClient.post<ApiResponse<CheckoutOrder>>(API_ENDPOINTS.PAYMENTS.LINK(orderId))
    ).data.data;
  },
};
