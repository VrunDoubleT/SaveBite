import { httpClient } from "@/shared/api/httpClient";
import { API_ENDPOINTS } from "@/shared/api/endpoints";
import type { ApiResponse } from "@/shared/types";
import type { StaffInvitation } from "../types/staffInvitation.types";

let invitationsRequest: Promise<StaffInvitation[]> | null = null;

export const staffInvitationApi = {
  async getMyInvitations(forceRefresh = false): Promise<StaffInvitation[]> {
    if (invitationsRequest && !forceRefresh) {
      return invitationsRequest;
    }

    const request = httpClient
      .get<ApiResponse<StaffInvitation[]>>(API_ENDPOINTS.STAFF_INVITATIONS.ME)
      .then((response) => response.data.data)
      .finally(() => {
        if (invitationsRequest === request) invitationsRequest = null;
      });

    invitationsRequest = request;
    return request;
  },

  async acceptInvitation(invitationId: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.STAFF_INVITATIONS.ACCEPT(invitationId));
  },

  async declineInvitation(invitationId: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.STAFF_INVITATIONS.DECLINE(invitationId));
  },
};
