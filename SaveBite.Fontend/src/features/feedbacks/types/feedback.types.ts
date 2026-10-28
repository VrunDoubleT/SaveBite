export interface FeedbackReply {
  id: string;
  content: string;
  createdAt: string;
  updatedAt?: string;
  repliedByName?: string;
}

export interface FeedbackItem {
  id: string;
  orderItemId: string;
  productId: string;
  productName: string;
  variantName?: string;
  rating: number;
  comment?: string;
  images?: string[];
  status: string | number;
  createdAt: string;
  updatedAt: string;
  reply?: FeedbackReply | null;
  orderId?: string;
  orderCode?: string;
  shopName?: string;
  shopLogoUrl?: string | null;
  imageUrl?: string | null;
  shopId?: string;
  customerName?: string;
  customerAvatarUrl?: string | null;
}

export interface CreateFeedbackInput {
  orderItemId: string;
  rating: number;
  comment?: string;
  images?: string[];
}

export interface UpdateFeedbackInput {
  rating: number;
  comment?: string;
  images?: string[];
}

export interface CreateFeedbackReplyInput {
  content: string;
}

export interface UpdateFeedbackReplyInput {
  content: string;
}

export interface ShopFeedbacksQueryParams {
  page?: number;
  pageSize?: number;
  rating?: number;
  hasReply?: boolean;
  shopId?: string;
}

export interface FeedbackHistoryResponse {
  items: FeedbackItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
