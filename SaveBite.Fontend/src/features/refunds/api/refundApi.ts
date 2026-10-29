import { httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { Refund, RefundBank, RefundPage, RefundImage, RefundQr } from "../types/refund.types";
const path = (id: string) => `/refunds/${encodeURIComponent(id)}`;
export const refundApi = {
  async detail(id: string, signal?: AbortSignal) {
    return (await httpClient.get<ApiResponse<Refund>>(path(id), { signal })).data.data;
  },
  async shopPendingCount(signal?: AbortSignal) {
    return (await httpClient.get<ApiResponse<number>>(`/refunds/shop/pending-count`, { signal }))
      .data.data;
  },
  async shopDetail(refundId: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<Refund>>(`/refunds/shop/${encodeURIComponent(refundId)}`, {
        signal,
      })
    ).data.data;
  },
  async upload(
    orderId: string,
    file: File,
    purpose: "request-evidence" | "destination-qr" | "transfer-proof",
  ) {
    const body = new FormData();
    body.append("file", file);
    body.append("purpose", purpose);
    const url =
      purpose === "transfer-proof"
        ? `/orders/shop/${encodeURIComponent(orderId)}/refunds/images`
        : `/orders/${encodeURIComponent(orderId)}/refunds/images`;
    return (
      await httpClient.post<ApiResponse<RefundImage>>(url, body, {
        headers: { "Content-Type": undefined },
        timeout: 120_000,
      })
    ).data.data;
  },
  async shopPage(page: number, status: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<RefundPage>>(`/refunds/shop`, {
        params: { page, pageSize: 10, status },
        signal,
      })
    ).data.data;
  },
  async shopOrder(orderId: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<Refund[]>>(
        `/orders/shop/${encodeURIComponent(orderId)}/refunds`,
        { signal },
      )
    ).data.data;
  },
  async review(id: string, approve: boolean, reason?: string) {
    return (
      await httpClient.post<ApiResponse<Refund>>(
        `/refunds/shop/${encodeURIComponent(id)}/${approve ? "approve" : "reject"}`,
        approve ? undefined : { reason },
      )
    ).data.data;
  },
  async qr(id: string) {
    return (
      await httpClient.post<ApiResponse<RefundQr>>(`/refunds/shop/${encodeURIComponent(id)}/vietqr`)
    ).data.data;
  },
  async transfer(id: string, transferReference: string, transferredAt?: string, proofUrl?: string) {
    return (
      await httpClient.post<ApiResponse<Refund>>(
        `/refunds/shop/${encodeURIComponent(id)}/transfer`,
        { transferReference, transferredAt, proofUrl },
      )
    ).data.data;
  },
  async pendingDetailsCount(signal?: AbortSignal) {
    return (await httpClient.get<ApiResponse<number>>("/refunds/pending-details-count", { signal }))
      .data.data;
  },
  async page(page: number, status: string, type: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<RefundPage>>("/refunds", {
        params: { page, pageSize: 10, status, type },
        signal,
      })
    ).data.data;
  },
  async forOrder(orderId: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<Refund[]>>(
        `/orders/${encodeURIComponent(orderId)}/refunds`,
        { signal },
      )
    ).data.data;
  },
  async request(
    orderId: string,
    items: { orderItemId: string; quantity: number; reason: string; evidenceUrls: string[] }[],
  ) {
    return (
      await httpClient.post<ApiResponse<Refund>>(
        `/orders/${encodeURIComponent(orderId)}/refunds/requests`,
        { items },
      )
    ).data.data;
  },
  async destination(id: string, bank: RefundBank) {
    return (await httpClient.post<ApiResponse<Refund>>(`${path(id)}/destination`, bank)).data.data;
  },
  async confirm(id: string, code: string) {
    return (await httpClient.post<ApiResponse<Refund>>(`${path(id)}/destination/confirm`, { code }))
      .data.data;
  },
  async withdraw(id: string) {
    return (await httpClient.post<ApiResponse<Refund>>(`${path(id)}/withdraw`)).data.data;
  },
};
