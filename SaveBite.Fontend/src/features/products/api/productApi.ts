import { httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  FlashDeal,
  Page,
  Product,
  ProductListItem,
  ProductListQuery,
  ProductOptions,
  ProductSummary,
} from "../types/product.types";

const base = "/store-owner/products";
// Override the shared JSON default so the browser supplies the multipart boundary.
const uploadConfig = { headers: { "Content-Type": undefined }, timeout: 120_000 };
export const productApi = {
  async summary(signal?: AbortSignal) {
    return (await httpClient.get<ApiResponse<ProductSummary>>(`${base}/summary`, { signal })).data
      .data;
  },
  async options(signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<ProductOptions>>("/store-owner/product-options", { signal })
    ).data.data;
  },
  async list(params: ProductListQuery, signal?: AbortSignal) {
    return (await httpClient.get<ApiResponse<Page<ProductListItem>>>(base, { params, signal })).data
      .data;
  },
  async get(id: string, signal?: AbortSignal) {
    return (await httpClient.get<ApiResponse<Product>>(`${base}/${id}`, { signal })).data.data;
  },
  async create(body: FormData) {
    return (await httpClient.post<ApiResponse<Product>>(base, body, uploadConfig)).data.data;
  },
  async update(id: string, body: { name: string; description: string; categoryId: string }) {
    return (await httpClient.put<ApiResponse<Product>>(`${base}/${id}`, body)).data.data;
  },
  async remove(id: string) {
    await httpClient.delete(`${base}/${id}`);
  },
  async images(id: string, body: FormData) {
    return (await httpClient.put<ApiResponse<Product>>(`${base}/${id}/images`, body, uploadConfig))
      .data.data;
  },
  async addValues(id: string, body: FormData) {
    return (
      await httpClient.post<ApiResponse<Product>>(
        `${base}/${id}/attribute-values`,
        body,
        uploadConfig,
      )
    ).data.data;
  },
  async valueImage(id: string, valueId: string, image: File) {
    const body = new FormData();
    body.append("Image", image);
    return (
      await httpClient.put<ApiResponse<Product>>(
        `${base}/${id}/values/${valueId}/image`,
        body,
        uploadConfig,
      )
    ).data.data;
  },
  async deals(id: string, page: number, status: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<Page<FlashDeal>>>(`${base}/${id}/flash-deals`, {
        params: { page, pageSize: 10, status: status || undefined },
        signal,
      })
    ).data.data;
  },
};
