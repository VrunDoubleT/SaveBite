import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse, PaginatedResponse, PaginationParams } from "@/shared/types";
import type { AdminUser, UpdateUserStatusRequest, UserDetailsResponse } from "../types/user.types";

interface BackendPagedUser {
  items: AdminUser[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export const userApi = {
  async getUsers(
    params?: PaginationParams & { search?: string },
  ): Promise<PaginatedResponse<AdminUser>> {
    const res = await httpClient.get<ApiResponse<BackendPagedUser>>(API_ENDPOINTS.USERS.BASE, {
      params,
    });
    const data = res.data.data;
    return {
      items: data.items,
      page: data.page,
      pageSize: data.pageSize,
      total: data.totalItems,
      totalPages: data.totalPages,
    };
  },

  // chi tiết User lịch sử Log
  async getUserDetails(id: string): Promise<UserDetailsResponse> {
    const res = await httpClient.get<ApiResponse<UserDetailsResponse>>(
      API_ENDPOINTS.USERS.DETAIL(id),
    );
    return res.data.data;
  },

  // Suspend và Reactivate Account Customer
  async updateStatus(id: string, data: UpdateUserStatusRequest) {
    const res = await httpClient.patch<ApiResponse<string>>(
      API_ENDPOINTS.USERS.STATUS(id),
      data,
    );
    return res.data;
  },
};
