export interface Refund {
  id: string;
  orderId: string;
  orderCode: string;
  shopId: string;
  shopName: string;
  shopLogoUrl?: string;
  customerName?: string;
  customerAvatarUrl?: string;
  customerPhone?: string;
  customerLevelName?: string;
  customerLevelRank?: number;
  requiresShopAction?: boolean;
  shopAction?: "WaitingCustomer" | "Closed" | "DeadlinePassed" | "ReviewRequired" | "TransferRequired";
  type: "CustomerRequest" | "ShopCancellation";
  status: string;
  amount: number;
  transferredAmount: number;
  reason?: string;
  rejectReason?: string;
  receiverMethod?: "BankAccount" | "QrImage";
  receiverQrImageUrl?: string;
  bankBin?: string;
  bankName?: string;
  accountNumber?: string;
  accountHolder?: string;
  confirmationExpiresAt?: string;
  customerDetailsDeadlineAt?: string;
  destinationConfirmedAt?: string;
  reviewDeadlineAt?: string;
  reviewedAt?: string;
  isOverdue: boolean;
  transferReference?: string;
  transferredAt?: string;
  createdAt: string;
  evidenceUrls: string[];
  transferProofUrls: string[];
  history: { id: string; toStatus: string; actorRole: string; actorName: string; decision: string; note?: string; createdAt: string; proofUrl?: string }[];
}
export interface RefundBank {
  receiverMethod?: "BankAccount" | "QrImage";
  qrImageUrl?: string;
  bankBin: string;
  bankName: string;
  accountNumber: string;
  accountHolder: string;
}

export interface RefundPage { items: Refund[]; page: number; pageSize: number; totalItems: number; totalPages: number; }
export interface RefundImage { url: string; bankBin?: string; accountNumber?: string; accountHolder?: string; }
export interface RefundQr { refundId: string; amount: number; bankBin: string; accountNumber: string; accountHolder: string; transferContent: string; imageUrl: string; }
