import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse, PaginatedResponse, PaginationParams } from "@/shared/types";
import type { AdminUser } from "../types/user.types";

// Định nghĩa khung dữ liệu thực tế C# trả về
interface BackendPagedUser {
  items: AdminUser[];
  page: number;
  pageSize: number;
  totalItems: number; // C# trả về trường này
  totalPages: number;
}

export const userApi = {
  async getUsers(
    params?: PaginationParams & { search?: string },
  ): Promise<PaginatedResponse<AdminUser>> {
    const res = await httpClient.get<ApiResponse<BackendPagedUser>>(API_ENDPOINTS.ADMIN.USERS, {
      params,
    });
    const data = res.data.data;

    // Map dữ liệu về đúng chuẩn api.types.ts của team
    return {
      items: data.items,
      page: data.page,
      pageSize: data.pageSize,
      total: data.totalItems, // Chuyển totalItems -> total
      totalPages: data.totalPages,
    };
  },
  async suspendAccount(id: string, reason: string) {
    const res = await httpClient.post<ApiResponse<string>>(
      `${API_ENDPOINTS.ADMIN.USERS}/${id}/suspend`,
      { reason },
    );
    return res.data;
  },
  async reactivateAccount(id: string, reason: string) {
    const res = await httpClient.post<ApiResponse<string>>(
      `${API_ENDPOINTS.ADMIN.USERS}/${id}/reactivate`,
      { reason },
    );
    return res.data;
  },
};
