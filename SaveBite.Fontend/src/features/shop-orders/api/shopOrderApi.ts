import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { OrderRefund, ShopOrder, ShopOrderPage } from "../types/shopOrder.types";
const base = API_ENDPOINTS.ORDERS.SHOP;
export const shopOrderApi = {
  async ownerShop(signal?: AbortSignal) {
    return (await httpClient.get<ApiResponse<string>>(API_ENDPOINTS.ORDERS.MY_SHOP, { signal })).data
      .data;
  },
  async page(
    shopId: string,
    status: string,
    search: string,
    page = 1,
    signal?: AbortSignal,
    orderDate?: string,
  ) {
    const result = (
      await httpClient.get<ApiResponse<ShopOrderPage>>(base(shopId), {
        params: { status, search: search || undefined, page, pageSize: 10, ...(orderDate ? { orderDate } : {}) },
        signal,
      })
    ).data.data;
    const pagination = result?.page;
    if (!pagination || !Number.isInteger(pagination.page) || pagination.page < 1
      || !Number.isInteger(pagination.pageSize) || pagination.pageSize < 1
      || !Number.isInteger(pagination.totalItems) || pagination.totalItems < 0
      || !Number.isInteger(pagination.totalPages) || pagination.totalPages < 0) {
      throw new Error("Shop order pagination data is outdated or invalid. Restart the backend to load the updated page-based API, then retry.");
    }
    return result;
  },
  async pendingCount(shopId: string, signal?: AbortSignal) {
    const count = (await httpClient.get<ApiResponse<number>>(`${base(shopId)}/pending-count`, { signal })).data.data;
    if (!Number.isInteger(count) || count < 0) throw new Error("The pending order count response is invalid.");
    return count;
  },
  async detail(shopId: string, orderId: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<ShopOrder>>(
        `${base(shopId)}/${encodeURIComponent(orderId)}`,
        { signal },
      )
    ).data.data;
  },
  async action(shopId: string, orderId: string, action: string, body?: object) {
    return (
      await httpClient.post<ApiResponse<ShopOrder>>(
        `${base(shopId)}/${encodeURIComponent(orderId)}/${action}`,
        body ?? {},
      )
    ).data.data;
  },
  async simulateRemaining(shopId: string, orderId: string, paymentId: string, outcome: "paid" | "cancelled" | "expired") {
    await httpClient.post(`/payments/simulation/shops/${encodeURIComponent(shopId)}/${encodeURIComponent(orderId)}/remaining`, { paymentId, outcome });
  },
  async refund(orderId: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<OrderRefund | null>>(
        `/orders/${encodeURIComponent(orderId)}/refund`,
        { signal },
      )
    ).data.data;
  },
  async destination(
    orderId: string,
    body: { bankBin: string; bankName: string; accountNumber: string; accountHolder: string },
  ) {
    return (
      await httpClient.post<ApiResponse<OrderRefund>>(
        `/orders/${encodeURIComponent(orderId)}/refund/destination`,
        body,
      )
    ).data.data;
  },
  async verify(orderId: string, code: string) {
    return (
      await httpClient.post<ApiResponse<OrderRefund>>(
        `/orders/${encodeURIComponent(orderId)}/refund/verify`,
        { code },
      )
    ).data.data;
  },
};
