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
  /** Đánh dấu deal mới xuất hiện thông qua Cursor 2 */
  isNew?: boolean;
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
}

export interface FlashDealCursorResult {
  deals: FlashDeal[];
  cursor1?: string;
  cursor2?: string;
  hasOlder: boolean;
  hasNewer: boolean;
  /** Số deal mới (tạo sau cursor2 cũ) được prepend vào đầu kết quả mode=Older */
  prependedCount: number;
  /** Số deal mới phát hiện khi polling (mode=Newer), chưa được load vào trang */
  newDealsCount: number;
  message?: string;
}

export interface DealFilterState {
  distance: string[];
  price: string[];
  category: string[];
}