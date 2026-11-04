import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse, PaginatedResponse, PaginationParams } from "@/shared/types";
import type { Category, CategoryFormData } from "../types/adminCategory.types";

// Define actual data shape returned by C# API
interface BackendPagedCategory {
  items: Category[];
  page: number;
  pageSize: number;
  totalItems: number; // C# API returns this field
  totalPages: number;
}

export const categoryApi = {
  async getCategories(
    params?: PaginationParams & { search?: string; isActive?: boolean },
  ): Promise<PaginatedResponse<Category>> {
    const res = await httpClient.get<ApiResponse<BackendPagedCategory>>(
      API_ENDPOINTS.CATEGORIES.BASE,
      { params },
    );
    const data = res.data.data;

    // Map data to match team api.types.ts specification
    return {
      items: data.items,
      page: data.page,
      pageSize: data.pageSize,
      total: data.totalItems, // Map totalItems -> total
      totalPages: data.totalPages,
    };
  },
  async createCategory(data: CategoryFormData) {
    const res = await httpClient.post<ApiResponse<string>>(API_ENDPOINTS.CATEGORIES.BASE, data);
    return res.data;
  },
  async updateCategory(id: string, data: Partial<CategoryFormData>) {
    const res = await httpClient.put<ApiResponse<string>>(
      API_ENDPOINTS.CATEGORIES.DETAIL(id),
      data,
    );
    return res.data;
  },
  async toggleStatus(id: string, isActive: boolean) {
    const res = await httpClient.patch<ApiResponse<string>>(
      API_ENDPOINTS.CATEGORIES.STATUS(id),
      { isActive },
    );
    return res.data;
  },
  async deleteCategory(id: string) {
    const res = await httpClient.delete<ApiResponse<string>>(
      API_ENDPOINTS.CATEGORIES.DETAIL(id),
    );
    return res.data;
  },
};
