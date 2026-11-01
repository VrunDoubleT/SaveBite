export interface FeedbackReply {
  id: string;
  content: string;
  createdAt: string;
}

export interface FeedbackItem {
  id: string;
  orderItemId: string;
  productId: string;
  productName: string;
  variantName?: string;
  rating: number;
  comment?: string;
  status: string | number;
  createdAt: string;
  updatedAt: string;
  reply?: FeedbackReply | null;
  orderId?: string;
  orderCode?: string;
  shopName?: string;
  shopLogoUrl?: string | null;
  imageUrl?: string | null;
}

export interface CreateFeedbackInput {
  orderItemId: string;
  rating: number;
  comment?: string;
}

export interface UpdateFeedbackInput {
  rating: number;
  comment?: string;
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
