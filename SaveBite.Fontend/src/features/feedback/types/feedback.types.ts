export type FeedbackStatus = "Visible" | "HiddenByCustomer" | "HiddenByAdmin";

export type Feedback = {
  id: string;
  productId: string;
  productName: string;
  shopName: string;

  userId: string;
  customerName: string;
  customerPhone: string | null;
  customerEmail: string;

  orderItemId: string;
  rating: number;
  comment: string | null;
  status: FeedbackStatus;

  currentReason: string | null;
  createdAt: string;
  hiddenAt: string | null;
};

export type FeedbackPage = {
  items: Feedback[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};
