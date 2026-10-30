import type { ShopStatus } from "./ownerShop.types";

export interface ShopOwnerManagement {
  id: string;
  fullName: string;
  email: string | null;
  phoneNumber: string | null;
}

export interface ShopListManagement {
  id: string;
  name: string;
  description: string | null;
  status: ShopStatus;
  city: string | null;
  ownerUserId: string;
  ownerName: string | null;
  createdAt: string;
}

export interface ShopProfileDocument {
  id: string;
  documentType: string;
  fileUrl: string;
  originalFileName: string;
  contentType: string;
  uploadedAt: string;
}

export interface ShopStatusHistory {
  id: string;
  fromStatus: ShopStatus | null;
  toStatus: ShopStatus;
  reason: string | null;
  createdAt: string;
}

export interface ShopContentManagement extends ShopListManagement {
  currentStatusReason: string | null;
  statusHistory: ShopStatusHistory[];
  owner: ShopOwnerManagement | null;
  addressLine: string;
  ward: string | null;
  district: string | null;
  latitude: number;
  longitude: number;
  logoUrl: string | null;
  coverImageUrl: string | null;
  openingTime: string | null;
  closingTime: string | null;
  businessLicenseNo: string | null;
  documents: ShopProfileDocument[];
  updatedAt: string;
}

export interface UpdateShopModerationStatus {
  status: ShopStatus;
  reason?: string | null;
}
