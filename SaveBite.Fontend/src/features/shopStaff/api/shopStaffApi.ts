import { httpClient } from "@/shared/api/httpClient";
import { API_ENDPOINTS } from "@/shared/api/endpoints";

import type { ApiResponse, PaginatedResponse } from "@/shared/types";

import type {
  ShopStaff,
  StaffActivityLog,
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

const activityLogRequests = new Map<string, Promise<StaffActivityLog[]>>();

export const shopStaffApi = {
  async getStaffList(): Promise<ShopStaff[]> {
    if (staffListRequest) {
      return staffListRequest;
    }

    staffListRequest = (async () => {
      const response = await httpClient.get<ApiResponse<PaginatedResponse<ShopStaff>>>(
        API_ENDPOINTS.OWNER_SHOPS.STAFFS,
        {
          params: {
            page: 1,
            pageSize: 50,
          },
        },
      );

      return response.data.data.items;
    })();

    try {
      return await staffListRequest;
    } finally {
      staffListRequest = null;
    }
  },

  async getStaffActivityLogs(staffId: string): Promise<StaffActivityLog[]> {
    const existingRequest = activityLogRequests.get(staffId);

    if (existingRequest) {
      return existingRequest;
    }

    const request = (async () => {
      const response = await httpClient.get<ApiResponse<PaginatedResponse<StaffActivityLog>>>(
        API_ENDPOINTS.OWNER_SHOPS.ACTIVITY_LOGS(staffId),
        {
          params: {
            page: 1,
            pageSize: 50,
          },
        },
      );

      return response.data.data.items;
    })();

    activityLogRequests.set(staffId, request);

    try {
      return await request;
    } finally {
      activityLogRequests.delete(staffId);
    }
  },

  async updateStaff(staffId: string, input: UpdateStaffInfoInput) {
    return httpClient.put<ApiResponse<null>>(
      API_ENDPOINTS.OWNER_SHOPS.STAFF_DETAIL(staffId),
      input,
    );
  },

  async removeStaff(staffId: string) {
    return httpClient.delete<ApiResponse<null>>(API_ENDPOINTS.OWNER_SHOPS.STAFF_DETAIL(staffId));
  },

  async inviteStaff(input: InviteStaffInput) {
    return httpClient.post<ApiResponse<StaffInvitation>>(
      API_ENDPOINTS.OWNER_SHOPS.INVITATIONS,
      input,
    );
  },

  async searchStaffCandidates(keyword: string): Promise<StaffCandidate[]> {
    const response = await httpClient.get<ApiResponse<PaginatedResponse<StaffCandidate>>>(
      API_ENDPOINTS.OWNER_SHOPS.STAFF_CANDIDATES,
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

  async getInvitations(): Promise<StaffInvitation[]> {
    if (invitationListRequest) {
      return invitationListRequest;
    }

    invitationListRequest = (async () => {
      const response = await httpClient.get<ApiResponse<PaginatedResponse<StaffInvitation>>>(
        API_ENDPOINTS.OWNER_SHOPS.INVITATIONS,
        {
          params: {
            page: 1,
            pageSize: 50,
          },
        },
      );

      return response.data.data.items;
    })();

    try {
      return await invitationListRequest;
    } finally {
      invitationListRequest = null;
    }
  },

  async revokeInvitation(invitationId: string) {
    return httpClient.delete<ApiResponse<null>>(
      API_ENDPOINTS.OWNER_SHOPS.INVITATION_DETAIL(invitationId),
    );
  },
};
