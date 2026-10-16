export type ShopStatus = "Active" | "Paused" | "Suspended" | string;

export interface ShopProfileDocument {
  id: string;
  documentType: "BusinessLicense" | "FoodSafetyCertificate" | "Other" | string;
  fileUrl: string;
  originalFileName: string;
  contentType: string;
  uploadedAt: string;
}

export interface OwnerShop {
  id: string;
  name: string;
  description?: string | null;
  addressLine?: string;
  ward?: string | null;
  district?: string | null;
  city?: string | null;
  latitude?: number;
  longitude?: number;
  logoUrl?: string | null;
  coverImageUrl?: string | null;
  openingTime?: string | null;
  closingTime?: string | null;
  isOpen?: boolean;
  status?: ShopStatus;
  businessLicenseNo?: string | null;
  documents?: ShopProfileDocument[];
  averageRating?: number;
  totalReviews?: number;
  ratingBreakdown?: Record<string, number>;
}

export interface UpdateBusinessInfo {
  name: string;
  description: string;
  addressLine: string;
  ward: string;
  district: string;
  city: string;
  latitude: number;
  longitude: number;
  openingTime: string | null;
  closingTime: string | null;
}
