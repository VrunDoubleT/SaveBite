export type ShopApplicationStatus =
  | "Draft"
  | "Pending"
  | "NeedsRevision"
  | "Approved"
  | "Rejected"
  | "Cancelled";

export type ShopDocumentType =
  | "BusinessLicense"
  | "FoodSafetyCertificate"
  | "Other";

export interface ShopApplicationDocument {
  id: string;
  documentType: ShopDocumentType;
  fileUrl: string;
  originalFileName: string;
  contentType: string;
  revisionNumber: number;
  isCurrent: boolean;
}

export interface ShopApplicationReviewLog {
  fromStatus: ShopApplicationStatus | null;
  toStatus: ShopApplicationStatus;
  revisionNumber: number;
  note: string | null;
  createdAt: string;
}

export interface ShopApplication {
  id: string;
  name: string;
  description: string | null;
  businessLicenseNo: string | null;
  addressLine: string;
  ward: string | null;
  district: string | null;
  city: string | null;
  latitude: number;
  longitude: number;
  logoUrl: string | null;
  coverImageUrl: string | null;
  openingTime: string | null;
  closingTime: string | null;
  status: ShopApplicationStatus;
  revisionNumber: number;
  createdAt: string;
  updatedAt: string;
  bankName: string;
  bankAccountNumber: string;
  bankAccountHolder: string;
  hasPayosConfiguration: boolean;
  documents: ShopApplicationDocument[];
  reviewLogs: ShopApplicationReviewLog[];
}

export interface ShopApplicationForm {
  name: string;
  description: string;
  businessLicenseNo: string;
  addressLine: string;
  ward: string;
  district: string;
  city: string;
  latitude: string;
  longitude: string;
  openingTime: string;
  closingTime: string;
  bankName: string;
  bankAccountNumber: string;
  bankAccountHolder: string;
  payosClientId: string;
  payosApiKey: string;
  payosChecksumKey: string;
}

export interface ShopDocumentUpload {
  type: ShopDocumentType;
  file: File;
}
