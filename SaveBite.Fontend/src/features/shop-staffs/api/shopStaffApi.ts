import { httpClient } from "@/shared/api/httpClient";
import { API_ENDPOINTS } from "@/shared/api/endpoints";

import type { ApiResponse, PaginatedResponse } from "@/shared/types";

import type {
  ShopStaff,
  StaffActivityLogPage,
  StaffActivityLogQuery,
  UpdateStaffInfoInput,
  InviteStaffInput,
  StaffInvitation,
  StaffCandidate,
} from "../types/shopStaff.types";

// --------------------------------------------------
// Prevent duplicate concurrent requests
// --------------------------------------------------

let staffListRequest: Promise<ShopStaff[]> | null = null;

let invitationListRequest: Promise<StaffInvitation[]> | null = null;

const activityLogRequests = new Map<string, Promise<StaffActivityLogPage>>();

export const shopStaffApi = {
  async getStaffList(forceRefresh = false): Promise<ShopStaff[]> {
    if (staffListRequest && !forceRefresh) {
      return staffListRequest;
    }

    const request = (async () => {
      const response = await httpClient.get<ApiResponse<PaginatedResponse<ShopStaff>>>(
        API_ENDPOINTS.SHOP_STAFFS.LIST,
        {
          params: {
            page: 1,
            pageSize: 50,
          },
        },
      );

      return response.data.data.items;
    })();

    staffListRequest = request;
    try {
      return await request;
    } finally {
      if (staffListRequest === request) staffListRequest = null;
    }
  },

  // Server-side paging + optional date range (fromDate / toDate are inclusive, yyyy-MM-dd).
  async getStaffActivityLogs(
    staffId: string,
    query: StaffActivityLogQuery,
  ): Promise<StaffActivityLogPage> {
    // The same staff + same filters share one in-flight request.
    const key = `${staffId}|${query.page}|${query.pageSize}|${query.fromDate ?? ""}|${query.toDate ?? ""}`;
    const existingRequest = activityLogRequests.get(key);

    if (existingRequest) {
      return existingRequest;
    }

    const request = (async () => {
      const response = await httpClient.get<ApiResponse<StaffActivityLogPage>>(
        API_ENDPOINTS.SHOP_STAFFS.ACTIVITY_LOGS(staffId),
        {
          params: {
            page: query.page,
            pageSize: query.pageSize,
            // Empty values are omitted so the backend applies no date filter.
            fromDate: query.fromDate || undefined,
            toDate: query.toDate || undefined,
          },
        },
      );

      return response.data.data;
    })();

    activityLogRequests.set(key, request);

    try {
      return await request;
    } finally {
      activityLogRequests.delete(key);
    }
  },

  async updateStaff(staffId: string, input: UpdateStaffInfoInput) {
    return httpClient.put<ApiResponse<null>>(API_ENDPOINTS.SHOP_STAFFS.DETAIL(staffId), input);
  },

  async removeStaff(staffId: string) {
    return httpClient.delete<ApiResponse<null>>(API_ENDPOINTS.SHOP_STAFFS.DETAIL(staffId));
  },

  async inviteStaff(input: InviteStaffInput) {
    return httpClient.post<ApiResponse<StaffInvitation>>(
      API_ENDPOINTS.STAFF_INVITATIONS.BASE,
      input,
    );
  },

  async searchStaffCandidates(keyword: string): Promise<StaffCandidate[]> {
    const response = await httpClient.get<ApiResponse<PaginatedResponse<StaffCandidate>>>(
      API_ENDPOINTS.STAFF_INVITATIONS.CANDIDATES,
      {
        params: {
          keyword,
          page: 1,
          pageSize: 20,
        },
      },
    );

    return response.data.data.items;
  },

  async getInvitations(forceRefresh = false): Promise<StaffInvitation[]> {
    if (invitationListRequest && !forceRefresh) {
      return invitationListRequest;
    }

    const request = (async () => {
      const response = await httpClient.get<ApiResponse<PaginatedResponse<StaffInvitation>>>(
        API_ENDPOINTS.STAFF_INVITATIONS.BY_SHOP,
        {
          params: {
            page: 1,
            pageSize: 50,
          },
        },
      );

      return response.data.data.items;
    })();

    invitationListRequest = request;
    try {
      return await request;
    } finally {
      if (invitationListRequest === request) invitationListRequest = null;
    }
  },

  async revokeInvitation(invitationId: string) {
    return httpClient.delete<ApiResponse<null>>(
      API_ENDPOINTS.STAFF_INVITATIONS.DETAIL(invitationId),
    );
  },
};
