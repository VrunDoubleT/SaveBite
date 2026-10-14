export interface Option {
  id: string;
  name: string;
}
export interface ProductOptions {
  shop: Option | null;
  categories: Option[];
}
export interface ProductImage {
  id: string;
  url: string;
  source: "product" | "value";
  attributeId?: string;
  valueId?: string;
  attributeName?: string;
  value?: string;
}
export interface ProductValue {
  id: string;
  value: string;
  imageUrl?: string;
}
export interface ProductAttribute {
  id: string;
  name: string;
  values: ProductValue[];
}
export interface ProductVariant {
  id: string;
  sku?: string;
  originalPrice: number;
  dealPrice: number;
  isActive: boolean;
  valueIds: string[];
}
export interface ProductListItem {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  status: number;
  variantCount: number;
  minOriginalPrice?: number;
  minDealPrice?: number;
  maxDealPrice?: number;
  updatedAt: string;
  images: ProductImage[];
}
export interface Product {
  id: string;
  shopId: string;
  categoryId: string;
  categoryName: string;
  name: string;
  description?: string;
  status: number;
  createdAt: string;
  updatedAt: string;
  images: ProductImage[];
  attributes: ProductAttribute[];
  variants: ProductVariant[];
  latestReview?: { status: number; note?: string; reviewedAt?: string };
}
export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
export interface FlashDeal {
  id: string;
  status: number;
  saleStartTime: string;
  orderEndTime: string;
  shopClosingTime: string;
  createdAt: string;
  variants: {
    id: string;
    variantId: string;
    sku?: string;
    values: { attributeId: string; attributeName: string; valueId: string; value: string }[];
    originalPrice: number;
    dealPrice: number;
    discountPercent: number;
    totalQuantity: number;
    reservedQuantity: number;
    soldQuantity: number;
    availableQuantity: number;
    status: number;
  }[];
}
export interface DraftValue {
  id: string;
  value: string;
  image?: File;
}
export interface DraftAttribute {
  id: string;
  name: string;
  values: DraftValue[];
}
export interface Combination {
  key: string;
  ids: string[];
  label: string;
}
export interface PriceInput {
  originalPrice: string;
  dealPrice: string;
}
export type PriceMap = Record<string, PriceInput>;
export interface ProductListQuery {
  page: number;
  pageSize: number;
  search?: string;
  categoryId?: string;
  status?: string;
}

export interface ProductSummary {
  totalProducts: number;
  activeProducts: number;
  hiddenProducts: number;
  totalVariants: number;
}
