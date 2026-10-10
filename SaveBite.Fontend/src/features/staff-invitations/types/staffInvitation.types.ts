export type StaffInvitationStatus =
  | "Pending"
  | "Accepted"
  | "Declined"
  | "Cancelled";


export interface StaffInvitation {
  id: string;
  shopId: string;
  shopName: string;
  invitedUserId: string;
  invitedUserName: string;
  invitedUserEmail: string;
  status: StaffInvitationStatus;
  createdAt: string;
  expiresAt: string;
}