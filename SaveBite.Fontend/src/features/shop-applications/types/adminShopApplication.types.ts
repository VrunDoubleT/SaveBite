import type { ShopApplication } from "@/features/shop-applications/types/shopApplication.types";

export type { ShopApplication };
export type ShopApplicationReviewDecision = "Approved" | "Rejected" | "NeedsRevision";
export type ShopApplicationAdminFilter =
  "All" | "Pending" | "Approved" | "Rejected" | "Cancelled" | "NeedsRevision";
