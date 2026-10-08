export interface NearbyShop {
  id: string;
  name: string;
  description?: string | null;
  address: string;
  latitude: number;
  longitude: number;
  distanceInKm: number;
  logoUrl?: string | null;
  coverImageUrl?: string | null;
  openingTime?: string | null;
  closingTime?: string | null;
  isOpen: boolean;
  averageRating: number;
  totalReviews: number;
}

export interface ShopProfile {
  id: string;
  name: string;
  description?: string | null;
  addressLine: string;
  ward?: string | null;
  district?: string | null;
  city?: string | null;
  latitude: number;
  longitude: number;
  logoUrl?: string | null;
  coverImageUrl?: string | null;
  openingTime?: string | null;
  closingTime?: string | null;
  isOpen: boolean;
  averageRating: number;
  totalReviews: number;
  ratingBreakdown: Record<number, number>;
}

export interface StoreReviewReply {
  id: string;
  content: string;
  repliedByName: string;
  createdAt: string;
}

export interface StoreReview {
  id: string;
  userId: string;
  userFullName: string;
  userAvatarUrl?: string | null;
  userTier?: string;
  rating: number;
  comment?: string | null;
  productId: string;
  productName: string;
  productImageUrl?: string | null;
  variantName?: string | null;
  createdAt: string;
  reply?: StoreReviewReply | null;
  images?: string[];
}

export interface StoreReviewsSummary {
  averageRating: number;
  totalReviews: number;
  ratingBreakdown: Record<number, number>;
  reviews: {
    items: StoreReview[];
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasPrevious: boolean;
    hasNext: boolean;
  };
}

export interface NearbyShopsRequest {
  latitude?: number;
  longitude?: number;
  radiusInKm?: number;
  keyword?: string;
  onlyOpen?: boolean;
}

export interface StoreReviewsQueryRequest {
  page?: number;
  pageSize?: number;
  rating?: number;
}
