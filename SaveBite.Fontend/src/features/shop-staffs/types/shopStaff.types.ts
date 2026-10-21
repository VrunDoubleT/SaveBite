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
  actorName?: string;
  actorRole?: string;
  action: string;
  description: string;
  orderId?: string | null;
  order_id?: string | null;
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
