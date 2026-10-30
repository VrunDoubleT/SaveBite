import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  FlashDeal,
  Page,
  Product,
  ProductContentManagement,
  ProductListItem,
  ProductListManagement,
  ProductListQuery,
  ProductOptions,
  ProductSummary,
  RemoveViolatingProduct,
} from "../types/product.types";
import type { ModerationActionResult } from "@/shared/types/moderationAction.types";

// Override the shared JSON default so the browser supplies the multipart boundary.
const uploadConfig = { headers: { "Content-Type": undefined }, timeout: 120_000 };
export const productApi = {
  async summary(signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<ProductSummary>>(API_ENDPOINTS.PRODUCTS.SUMMARY, { signal })
    ).data.data;
  },
  async options(signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<ProductOptions>>(API_ENDPOINTS.PRODUCTS.OPTIONS, { signal })
    ).data.data;
  },
  async list(params: ProductListQuery, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<Page<ProductListItem>>>(API_ENDPOINTS.PRODUCTS.BASE, {
        params,
        signal,
      })
    ).data.data;
  },
  async get(id: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<Product>>(API_ENDPOINTS.PRODUCTS.DETAIL(id), { signal })
    ).data.data;
  },
  async create(body: FormData) {
    return (
      await httpClient.post<ApiResponse<Product>>(API_ENDPOINTS.PRODUCTS.BASE, body, uploadConfig)
    ).data.data;
  },
  async update(id: string, body: { name: string; description: string; categoryId: string }) {
    return (await httpClient.put<ApiResponse<Product>>(API_ENDPOINTS.PRODUCTS.DETAIL(id), body))
      .data.data;
  },
  async remove(id: string) {
    await httpClient.delete(API_ENDPOINTS.PRODUCTS.DETAIL(id));
  },
  async images(id: string, body: FormData) {
    return (
      await httpClient.put<ApiResponse<Product>>(
        API_ENDPOINTS.PRODUCTS.IMAGES(id),
        body,
        uploadConfig,
      )
    ).data.data;
  },
  async addValues(id: string, body: FormData) {
    return (
      await httpClient.post<ApiResponse<Product>>(
        API_ENDPOINTS.PRODUCTS.ATTRIBUTE_VALUES(id),
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
        API_ENDPOINTS.PRODUCTS.VALUE_IMAGE(id, valueId),
        body,
        uploadConfig,
      )
    ).data.data;
  },
  async deals(id: string, page: number, status?: string, signal?: AbortSignal) {
    return (
      await httpClient.get<ApiResponse<Page<FlashDeal>>>(API_ENDPOINTS.PRODUCTS.FLASH_DEALS(id), {
        params: { page, pageSize: 10, status: status || undefined },
        signal,
      })
    ).data.data;
  },
  async updateVariant(
    productId: string,
    variantId: string,
    body: { originalPrice: number; dealPrice: number; isActive: boolean },
  ) {
    return (
      await httpClient.put<ApiResponse<Product>>(
        API_ENDPOINTS.PRODUCTS.VARIANTS(productId, variantId),
        body,
      )
    ).data.data;
  },
  async updateStatus(productId: string, status: number) {
    return (
      await httpClient.patch<ApiResponse<Product>>(API_ENDPOINTS.PRODUCTS.STATUS(productId), {
        status,
      })
    ).data.data;
  },

  // Product Management
  async getProductsForManagement(
    params: {
      keyword?: string;
      page: number;
      pageSize: number;
    },
    signal?: AbortSignal,
  ): Promise<Page<ProductListManagement>> {
    const response = await httpClient.get<ApiResponse<Page<ProductListManagement>>>(
      API_ENDPOINTS.PRODUCTS.ADMIN_LIST,
      {
        params,
        signal,
      },
    );

    return response.data.data;
  },

  // Product Content Management
  async getProductContentForManagement(
    id: string,
    signal?: AbortSignal,
  ): Promise<ProductContentManagement> {
    const response = await httpClient.get<ApiResponse<ProductContentManagement>>(
      API_ENDPOINTS.PRODUCTS.ADMIN_CONTENT(id),
      {
        signal,
      },
    );

    return response.data.data;
  },

  // Product Violation Management
  async removeForViolation(productId: string, reason: string): Promise<ModerationActionResult> {
    const request: RemoveViolatingProduct = { reason };
    const response = await httpClient.delete<ApiResponse<ModerationActionResult>>(
      API_ENDPOINTS.PRODUCTS.VIOLATION(productId),
      { data: request },
    );
    return response.data.data;
  },

  // Product Content Management - Approve Content
  async approveContent(productId: string, revisionId: string): Promise<ModerationActionResult> {
    const response = await httpClient.post<ApiResponse<ModerationActionResult>>(
      API_ENDPOINTS.PRODUCTS.APPROVE_CONTENT(productId, revisionId),
    );
    return response.data.data;
  },
};
