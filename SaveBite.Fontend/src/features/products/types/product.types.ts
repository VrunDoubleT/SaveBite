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
  activeWarning?: {
    id: string;
    requiredCorrection: string;
    dueAt: string;
    firstEditedAt?: string;
    dealBlocked: boolean;
  };
  latestReview?: {
    status: number;
    note?: string;
    reviewedAt?: string;
    revisionId?: string;
    warningId?: string | null;
    correctionSubmitted: boolean;
    submittedAt?: string;
  };
}
export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
  hasPrevious?: boolean;
  hasNext?: boolean;
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
  status?: number;
}

export interface ProductSummary {
  totalProducts: number;
  activeProducts: number;
  hiddenProducts: number;
  totalVariants: number;
}

export interface ProductListManagement extends ProductListItem {
  latestReview?: Product["latestReview"];
  shopId: string;
  shopName: string;
  createdAt: string;
}

export interface AttributeValueContent {
  attributeId: string;
  attributeName: string;
  valueId: string;
  value: string;
  imageUrl: string | null;
}

export interface ProductContentManagement extends Product {
  revisionHistory: ProductRevisionHistory[];
  warningComparison?: {
    warningId: string;
    reason: string;
    requiredCorrection: string;
    dueAt: string;
    firstEditedAt?: string;
    dealBlocked: boolean;
    before: RevisionContentSnapshot;
    after: RevisionContentSnapshot;
  };
  shopName: string;
}

export interface RemoveViolatingProduct {
  reason: string;
}

export interface RevisionContentSnapshot {
  name: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  images: string[];
  attributeValues: AttributeValueContent[] | null;
}
export interface ProductRevisionHistory {
  id: string;
  revisionNumber: number;
  isLatest: boolean;
  submittedByUserId: string;
  submittedByName: string;
  submittedAt: string;
  status: number;
  reviewNote: string | null;
  reviewedByUserId: string | null;
  reviewedByName: string | null;
  reviewedAt: string | null;
  warningId: string | null;
  warningReason: string | null;
  warningDueAt?: string | null;
  requiredCorrection: string | null;
  before: RevisionContentSnapshot | null;
  after: RevisionContentSnapshot;
}
