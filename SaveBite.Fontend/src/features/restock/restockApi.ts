import { httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
export interface ResaleItem {
  orderItemId: string;
  productName: string;
  variantName: string | null;
  imageUrl: string | null;
  flashDealId: string;
  unitPrice: number;
  quantity: number;
  status: string;
  resaleQuantity: number;
  unsellableQuantity: number;
}
export interface ResaleDecision {
  orderId: string;
  deadlineAt: string | null;
  canDecide: boolean;
  items: ResaleItem[];
}
export interface RestockTask {
  orderId: string;
  orderCode: string;
  orderStatus: string;
  customerName: string;
  customerAvatarUrl: string | null;
  customerPhone: string | null;
  cancelReason: string | null;
  cancelledAt: string | null;
  updatedAt: string;
  totalAmount: number;
  decision: ResaleDecision;
}
export interface RestockPage {
  items: RestockTask[];
  page: number;
  totalPages: number;
  totalItems: number;
}
export interface RestockSummary {
  pendingCount: number;
  nextDeadlineAt: string | null;
}
export const restockApi = {
  async summary(shopId: string, signal: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<RestockSummary>>(`/orders/shop/${shopId}/restock-summary`, {
        signal,
      })
    ).data.data;
  },
  async list(
    shopId: string,
    pendingOnly: boolean,
    page: number,
    signal: AbortSignal,
    flashDealId?: string,
  ) {
    return (
      await httpClient.get<ApiResponse<RestockPage>>(`/orders/shop/${shopId}/restock`, {
        signal,
        params: { pendingOnly, page, pageSize: 10, flashDealId },
      })
    ).data.data;
  },
};
