export interface FlashDealVariant {
  id: string;
  variantId: string;
  sku?: string;
  variantName?: string;
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
  status: number;
  minDealPrice: number;
  maxOriginalPrice: number;
  maxDiscountPercent: number;
  variants: FlashDealVariant[];
}

export interface NearbyFlashDealsParams {
  latitude: number;
  longitude: number;
  radiusInKm?: number;
}

export interface DealFilterState {
  distance: string[];
  price: string[];
  category: string[];
}