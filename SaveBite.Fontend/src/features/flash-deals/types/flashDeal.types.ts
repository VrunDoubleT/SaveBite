export interface DealAttributeGroup {
  name: string;
  values: string[];
}

export interface FlashDealVariant {
  id: string;
  variantId: string;
  sku?: string;
  variantName?: string;
  imageUrl?: string;
  attributes?: Record<string, string>;
  originalPrice: number;
  dealPrice: number;
  discountPercent: number;
  totalQuantity: number;
  soldQuantity: number;
  availableQuantity: number;
  status: number;
  isPurchasable?: boolean;
}

export interface FlashDeal {
  id: string;
  shopId: string;
  shopName: string;
  shopLogoUrl?: string;
  shopAddress: string;
  distanceInKm?: number;
  productId: string;
  productName: string;
  categoryName?: string;
  description?: string;
  productImageUrl?: string;
  productImageUrls?: string[];
  saleStartTime: string;
  orderEndTime: string;
  shopClosingTime: string;
  createdAt: string;
  status: number;
  minDealPrice: number;
  maxOriginalPrice: number;
  maxDiscountPercent: number;
  attributeGroups?: DealAttributeGroup[];
  variants: FlashDealVariant[];
  /** Marks a deal as newly published via Cursor 2 */
  isNew?: boolean;
  isPurchasable?: boolean;
}

export type CursorMode = "Initial" | "Older" | "Newer";

export interface FlashDealCursorParams {
  latitude?: number;
  longitude?: number;
  radiusInKm?: number;
  cursor1?: string;
  cursor2?: string;
  mode?: CursorMode;
  limit?: number;
  category?: string;
  distance?: string;
  price?: string;
  shopName?: string;
  keyword?: string;
}

export interface FlashDealCursorResult {
  deals: FlashDeal[];
  cursor1?: string;
  cursor2?: string;
  hasOlder: boolean;
  hasNewer: boolean;
  /** Number of new deals (created after previous cursor2) prepended to mode=Older results */
  prependedCount: number;
  /** Number of new deals discovered during polling (mode=Newer), not yet loaded into view */
  newDealsCount: number;
  message?: string;
}

export interface DealFilterState {
  distance: string[];
  price: string[];
  category: string[];
}

export const FlashDealStatusEnum = {
  Pending: 0,
  OnSale: 1,
  Hidden: 2,
  SoldOut: 3,
  Closed: 4,
  Cancelled: 5,
} as const;

export type FlashDealStatusEnum = (typeof FlashDealStatusEnum)[keyof typeof FlashDealStatusEnum];

export const FlashDealVariantStatusEnum = {
  Active: 0,
  Hide: 1,
} as const;

export type FlashDealVariantStatusEnum = (typeof FlashDealVariantStatusEnum)[keyof typeof FlashDealVariantStatusEnum];

export interface ShopFlashDealListItem {
  id: string;
  productId: string;
  productName: string;
  productImageUrl?: string;
  categoryName?: string;
  saleStartTime: string;
  orderEndTime: string;
  shopClosingTime: string;
  status: FlashDealStatusEnum | number;
  minDealPrice: number;
  maxOriginalPrice: number;
  maxDiscountPercent: number;
  totalQuantity: number;
  reservedQuantity: number;
  soldQuantity: number;
  availableQuantity: number;
  createdAt: string;
}

export interface ShopFlashDealVariantDetail {
  id: string;
  variantId: string;
  sku?: string;
  variantName?: string;
  imageUrl?: string;
  attributes?: Record<string, string>;
  originalPrice: number;
  dealPrice: number;
  discountPercent: number;
  totalQuantity: number;
  reservedQuantity: number;
  soldQuantity: number;
  availableQuantity: number;
  status: number;
}

export interface ShopFlashDealDetail {
  id: string;
  shopId: string;
  productId: string;
  productName: string;
  description?: string;
  categoryName?: string;
  primaryImageUrl?: string;
  productImageUrls: string[];
  saleStartTime: string;
  orderEndTime: string;
  shopClosingTime: string;
  status: FlashDealStatusEnum | number;
  createdAt: string;
  updatedAt: string;
  variants: ShopFlashDealVariantDetail[];
}

export interface ShopEligibleProductVariant {
  id: string;
  sku?: string;
  variantName: string;
  originalPrice: number;
  imageUrl?: string;
}

export interface ShopEligibleProduct {
  id: string;
  name: string;
  description?: string;
  categoryName?: string;
  imageUrl?: string;
  variants: ShopEligibleProductVariant[];
}

export interface CreateFlashDealVariantPayload {
  variantId: string;
  dealPrice: number;
  totalQuantity: number;
}

export interface CreateFlashDealPayload {
  productId: string;
  saleStartTime: string;
  orderEndTime: string;
  shopClosingTime: string;
  variants: CreateFlashDealVariantPayload[];
}

export interface UpdateFlashDealVariantPayload {
  variantId: string;
  dealPrice: number;
  totalQuantity: number;
  status?: FlashDealVariantStatusEnum | number;
}

export interface UpdateFlashDealPayload {
  saleStartTime: string;
  orderEndTime: string;
  shopClosingTime: string;
  variants: UpdateFlashDealVariantPayload[];
}

export interface ShopFlashDealQueryParams {
  status?: number;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}