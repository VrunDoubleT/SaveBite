import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
export interface OrderRefund {
  id: string;
  status: string;
  amount: number;
  transferredAmount?: number;
  reason?: string;
  bankName?: string;
  accountNumber?: string;
  accountHolder?: string;
  confirmationExpiresAt?: string;
  destinationConfirmedAt?: string;
  transferReference?: string;
}
export interface ShopOrder {
  order: CheckoutOrder;
  customerName: string;
  customerPhone?: string;
  confirmationDeadlineAt?: string;
  isOwner: boolean;
  payments: {
    id: string;
    type: string;
    method: string;
    status: string;
    amount: number;
    paidAt?: string;
    checkoutUrl?: string;
    qrCode?: string;
    collectedAmount?: number;
  }[];
  history: {
    fromStatus?: string;
    toStatus: string;
    note?: string;
    createdAt: string;
    actorId?: string | null;
    actorRole?: string;
    actorName?: string;
  }[];
  refund?: OrderRefund;
}
export interface ShopOrderPage {
  page: { items: ShopOrder[]; page: number; pageSize: number; totalItems: number; totalPages: number; hasPrevious: boolean; hasNext: boolean };
  statusCounts: Record<string, number>;
}
