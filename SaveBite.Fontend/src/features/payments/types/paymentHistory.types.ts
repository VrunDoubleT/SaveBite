import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";

export interface CustomerPayment {
  id: string;
  orderId: string;
  orderCode: string;
  shopName: string;
  shopLogoUrl?: string;
  type: "Full" | "Deposit" | "Remaining";
  method: string;
  status: string;
  amount: number;
  collectedAmount: number;
  createdAt: string;
  paidAt?: string;
}

export interface CustomerPaymentPage {
  items: CustomerPayment[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface CustomerPaymentDetail {
  payment: CustomerPayment;
  order: CheckoutOrder;
}
