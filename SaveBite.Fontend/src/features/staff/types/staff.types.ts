export type ShopStaffStatus =
  | "Active"
  | "Suspended"
  | "Removed";

export interface AssociatedShop {
  id: string;
  shopId: string;
  shopName: string;
  status: ShopStaffStatus;
  joinedAt: string;
}

export interface StaffShop {
  id: string;
  name: string;
  description: string | null;

  addressLine: string;
  ward: string | null;
  district: string | null;
  city: string | null;

  logoUrl: string | null;
  coverImageUrl: string | null;

  openingTime: string | null;
  closingTime: string | null;

  status: string;
}

export interface StaffInfo {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  staffNickname: string | null;
  status: ShopStaffStatus;
  joinedAt: string;
}

export interface StaffShopDetail {
  shop: StaffShop;
  staff: StaffInfo;
}