import { httpClient } from "@/shared/api/httpClient";
import { API_ENDPOINTS } from "@/shared/api/endpoints";
import type { ApiResponse } from "@/shared/types";
import type { StaffInvitation } from "../types/staffInvitation.types";

export const staffInvitationApi = {
  async getMyInvitations(): Promise<StaffInvitation[]> {
    const response = await httpClient.get<ApiResponse<StaffInvitation[]>>(
      API_ENDPOINTS.STAFF.INVITATIONS,
    );

    return response.data.data;
  },

  async acceptInvitation(invitationId: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.STAFF.ACCEPT_INVITATION(invitationId));
  },

  async declineInvitation(invitationId: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.STAFF.DECLINE_INVITATION(invitationId));
  },
};
