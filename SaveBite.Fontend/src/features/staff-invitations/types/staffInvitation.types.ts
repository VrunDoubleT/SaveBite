export type StaffInvitationStatus = "Pending" | "Accepted" | "Declined" | "Cancelled" | "Expired";

export interface StaffInvitation {
  id: string;
  shopId: string;
  shopName: string;
  shopLogoUrl?: string | null;
  shopCoverImageUrl?: string | null;
  shopAddress?: string | null;
  invitedUserId: string;
  invitedUserName: string;
  invitedUserEmail: string;
  status: StaffInvitationStatus;
  createdAt: string;
  expiresAt: string;
}
