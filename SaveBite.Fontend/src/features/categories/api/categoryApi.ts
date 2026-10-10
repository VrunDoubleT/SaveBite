import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { Category } from "@/features/categories/types/category.types";

export const categoryApi = {
  async getCategories(): Promise<Category[]> {
    try {
      const response = await httpClient.get<ApiResponse<Category[]>>(
        API_ENDPOINTS.CATEGORIES.CATALOG,
      );
      return response.data.data ?? [];
    } catch {
      // Fallback to flash deals categories endpoint if needed
      const response = await httpClient.get<ApiResponse<Category[]>>(
        API_ENDPOINTS.CATEGORIES.CATALOG,
      );
      return response.data.data ?? [];
    }
  },
};
