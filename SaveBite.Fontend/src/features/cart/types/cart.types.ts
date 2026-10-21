export interface CartItem {
  id: string;
  flashDealId: string;
  flashDealVariantId: string;
  productId: string;
  productName: string;
  variantName: string;
  imageUrl: string | null;
  originalPrice: number;
  unitPrice: number;
  quantity: number;
  availableQuantity: number;
  subtotal: number;
  orderEndTime: string;
  canUpdateQuantity: boolean;
  isAvailable: boolean;
  unavailableReason: string | null;
}

export interface CartShop {
  shopId: string;
  shopName: string;
  shopLogoUrl: string | null;
  shopAddress: string;
  latitude?: number;
  longitude?: number;
  items: CartItem[];
  totalQuantity: number;
  subtotal: number;
  canCheckout: boolean;
}

export interface Cart {
  shops: CartShop[];
  totalQuantity: number;
}

export interface CursorPagedResult<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
  pageSize: number;
}
export interface CartSummary {
  totalQuantity: number;
  totalFlashDeals: number;
}
export interface CartPage extends CartSummary {
  page: CursorPagedResult<CartShop>;
}

export interface CartMutationResponse {
  shopId: string;
  shop: CartShop | null;
  summary: CartSummary;
}
