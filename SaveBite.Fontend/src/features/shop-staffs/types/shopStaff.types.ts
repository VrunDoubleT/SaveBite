export type ShopStaffStatus = "Active" | "Suspended" | "Removed";

export interface ShopStaff {
  id: string;

  userId: string;

  userEmail: string;

  displayName: string;

  staffNickname: string | null;

  status: ShopStaffStatus;

  joinedAt: string;
}

export interface StaffActivityLog {
  id: string;
  actionBy: string;
  action: string;
  description: string;
  createdAt: string;
}

export interface UpdateStaffInfoInput {
  displayName: string;
  staffNickname: string | null;
  status: "Active" | "Suspended";
}

export interface InviteStaffInput {
  userId: string;
}

export interface StaffCandidate {
  userId: string;
  fullName: string;
  email: string;
}

export interface StaffInvitation {
  id: string;
  shopId: string;
  shopName: string;
  invitedUserId: string;
  invitedUserName: string;
  invitedUserEmail: string;
  status: string;
  createdAt: string;
  expiresAt: string;
}