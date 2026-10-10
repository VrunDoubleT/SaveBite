import { httpClient } from "@/shared/api/httpClient";
import { API_ENDPOINTS } from "@/shared/api/endpoints";
import type { ApiResponse } from "@/shared/types";
import type { StaffInvitation } from "../types/staffInvitation.types";

let invitationsRequest: Promise<StaffInvitation[]> | null = null;

export const staffInvitationApi = {
  async getMyInvitations(): Promise<StaffInvitation[]> {
    if (invitationsRequest) {
      return invitationsRequest;
    }

    invitationsRequest = httpClient
      .get<ApiResponse<StaffInvitation[]>>(
        API_ENDPOINTS.STAFF_INVITATIONS.ME,
      )
      .then((response) => response.data.data)
      .finally(() => {
        invitationsRequest = null;
      });

    return invitationsRequest;
  },

  async acceptInvitation(
    invitationId: string,
  ): Promise<void> {
    await httpClient.post(
      API_ENDPOINTS.STAFF_INVITATIONS.ACCEPT(invitationId),
    );
  },

  async declineInvitation(
    invitationId: string,
  ): Promise<void> {
    await httpClient.post(
      API_ENDPOINTS.STAFF_INVITATIONS.DECLINE(invitationId),
    );
  },
};