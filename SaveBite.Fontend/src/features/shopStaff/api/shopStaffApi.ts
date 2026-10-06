import { httpClient } from "@/shared/api/httpClient";
import { API_ENDPOINTS } from "@/shared/api/endpoints";
import type { ApiResponse } from "@/shared/types";

import type {
  ShopStaff,
  StaffActivityLog,
  UpdateStaffInfoInput,
  InviteStaffInput,
  StaffInvitation,
} from "../types/shopStaff.types";

export const shopStaffApi = {
  async getStaffList(shopId: string) {
    return httpClient.get<ApiResponse<ShopStaff[]>>(
      API_ENDPOINTS.OWNER_SHOPS.STAFFS(shopId),
    );
  },

  async getStaffActivityLogs(
    shopId: string,
    staffId: string,
  ) {
    return httpClient.get<ApiResponse<StaffActivityLog[]>>(
      API_ENDPOINTS.OWNER_SHOPS.ACTIVITY_LOGS(
        shopId,
        staffId,
      ),
    );
  },

  async updateStaff(
    shopId: string,
    staffId: string,
    input: UpdateStaffInfoInput,
  ) {
    return httpClient.put<ApiResponse<null>>(
      API_ENDPOINTS.OWNER_SHOPS.STAFF_DETAIL(
        shopId,
        staffId,
      ),
      input,
    );
  },

  async removeStaff(
    shopId: string,
    staffId: string,
  ) {
    return httpClient.delete<ApiResponse<null>>(
      API_ENDPOINTS.OWNER_SHOPS.STAFF_DETAIL(
        shopId,
        staffId,
      ),
    );
  },

  async inviteStaff(
    shopId: string,
    input: InviteStaffInput,
  ) {
    return httpClient.post<ApiResponse<StaffInvitation>>(
      API_ENDPOINTS.OWNER_SHOPS.INVITATIONS(shopId),
      input,
    );
  },

  async getInvitations(shopId: string) {
    return httpClient.get<ApiResponse<StaffInvitation[]>>(
      API_ENDPOINTS.OWNER_SHOPS.INVITATIONS(shopId),
    );
  },

  async revokeInvitation(
    shopId: string,
    invitationId: string,
  ) {
    return httpClient.delete<ApiResponse<null>>(
      API_ENDPOINTS.OWNER_SHOPS.INVITATION_DETAIL(
        shopId,
        invitationId,
      ),
    );
  },
};