import type { Product } from "@/features/products/types/product.types";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";
import type { Cart, CartShop } from "@/features/cart/types/cart.types";
import type {
  CheckoutOrder,
  CheckoutContext,
  CheckoutQuote,
} from "@/features/checkout/types/checkout.types";
import type { Refund } from "@/features/refunds/types/refund.types";
import { response3 as productSeed } from "./products-productApi";
import { response2 as dealSeed } from "./flash-deals-flashDealApi";
import { response2 as orderSeed } from "./checkout-checkoutApi";
import { response0 as refundSeed } from "./refunds-refundApi";
import { past, future, person } from "./values";
const uid = (n: number) => `00000000-0000-4000-8000-${String(n + 1).padStart(12, "0")}`;
const names = [
  "Bánh mì rau củ",
  "Túi bánh cuối ngày",
  "Hộp rau tươi Đà Lạt",
  "Combo bánh mì và sữa hạt nguyên chất – phần ăn gia đình đặc biệt",
];
export function makeProducts(): Product[] {
  return Array.from({ length: 18 }, (_, n) => ({
    ...structuredClone(productSeed.data),
    id: uid(n),
    shopId: uid(0),
    categoryId: uid(n % 3),
    name: names[n % names.length],
    status: n % 3,
    variants: [
      {
        id: uid(n),
        sku: `BM-${n + 1}`,
        originalPrice: 50000,
        dealPrice: 25000 + n * 1000,
        isActive: true,
        valueIds: [uid(0)],
      },
    ],
    attributes: [{ id: uid(0), name: "Khẩu phần", values: [{ id: uid(0), value: "Tiêu chuẩn" }] }],
    images: [
      { id: uid(n), url: "/images/deals/banh-mi/bm-overview.jpg", source: "product" as const },
    ],
  }));
}
export function makeDeals(products: Product[]): FlashDeal[] {
  return products.map((p, n) => ({
    ...structuredClone(dealSeed.data),
    id: p.id,
    productId: p.id,
    productName: p.name,
    shopId: uid(0),
    shopName: "Bếp Xanh Sài Gòn",
    status: n % 6,
    saleStartTime: past,
    orderEndTime: future,
    shopClosingTime: future,
    minDealPrice: p.variants[0].dealPrice,
    maxOriginalPrice: 50000,
    maxDiscountPercent: 50,
    isPurchasable: n % 6 === 1,
    variants: [
      {
        id: p.id,
        variantId: p.id,
        variantName: "Tiêu chuẩn",
        originalPrice: 50000,
        dealPrice: p.variants[0].dealPrice,
        discountPercent: 50,
        totalQuantity: 30,
        soldQuantity: 5,
        availableQuantity: 25,
        status: 0,
        isPurchasable: true,
      },
    ],
  }));
}
export function makeCart(deals: FlashDeal[]): Cart {
  const shop: CartShop = {
    shopId: uid(0),
    shopName: "Bếp Xanh Sài Gòn",
    shopLogoUrl: null,
    shopAddress: "12 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh",
    latitude: 10.7769,
    longitude: 106.7009,
    items: deals.slice(1, 3).map((d) => ({
      id: d.id,
      flashDealId: d.id,
      flashDealVariantId: d.variants[0].id,
      productId: d.productId,
      productName: d.productName,
      variantName: "Tiêu chuẩn",
      imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      originalPrice: 50000,
      unitPrice: d.minDealPrice,
      quantity: 1,
      availableQuantity: 25,
      subtotal: d.minDealPrice,
      orderEndTime: future,
      canUpdateQuantity: true,
      isAvailable: true,
      unavailableReason: null,
    })),
    totalQuantity: 2,
    subtotal: 53000,
    canCheckout: true,
  };
  return { shops: [shop], totalQuantity: 2 };
}
export const orderStatuses = [
  "AwaitingPayment",
  "Pending",
  "Confirmed",
  "Preparing",
  "ReadyForPickup",
  "Completed",
  "Cancelled",
  "Rejected",
  "Expired",
  "NoShow",
];
export function makeOrders(): CheckoutOrder[] {
  return Array.from({ length: 20 }, (_, n) => ({
    ...structuredClone(orderSeed.data),
    id: uid(n),
    orderCode: `SB${1001 + n}`,
    shopId: uid(0),
    shopName: "Bếp Xanh Sài Gòn",
    status: orderStatuses[n % orderStatuses.length],
    paymentStatus: n % 10 === 0 ? "Pending" : "Succeeded",
    paymentMethod: n % 2 ? "PayAtShop" : "OnlinePayment",
    totalAmount: n === 19 ? 25000000 : 50000,
    paidAmount: n % 10 === 0 ? 0 : n === 19 ? 25000000 : 50000,
    outstandingAmount: n % 10 === 0 ? 50000 : 0,
    depositAmount: 0,
    requiredOnlineAmount: n === 19 ? 25000000 : 50000,
    remainingAtShop: 0,
    paymentExpiresAt: future,
    softDeadlineAt: future,
    hardDeadlineAt: future,
    checkoutUrl: `/checkout/simulation?orderId=${uid(n)}`,
    canCancel: n % 10 < 2,
    paymentReviewRequired: false,
    items: [
      {
        orderItemId: uid(n),
        flashDealVariantId: uid(1),
        flashDealId: uid(1),
        productName: names[n % names.length],
        imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
        variantName: "Tiêu chuẩn",
        unitPrice: n === 19 ? 12500000 : 25000,
        quantity: 2,
        subtotal: n === 19 ? 25000000 : 50000,
        canReview: n % 10 === 5,
        feedbackId: null,
        rating: null,
      },
    ],
  }));
}
export const refundStatuses = [
  "AwaitingCustomerDetails",
  "PendingVerification",
  "PendingReview",
  "AwaitingTransfer",
  "Transferred",
  "Rejected",
  "Withdrawn",
  "Expired",
];
export function makeRefunds(): Refund[] {
  return refundStatuses.map((status, n) => ({
    ...structuredClone(refundSeed.data),
    id: uid(n),
    orderId: uid(5),
    shopId: uid(0),
    shopName: "Bếp Xanh Sài Gòn",
    customerName: person(n),
    type: n % 2 ? "ShopCancellation" : "CustomerRequest",
    status,
    amount: 25000,
    transferredAmount: status === "Transferred" ? 25000 : 0,
    orderRefundedAmount: status === "Transferred" ? 25000 : 0,
    orderNetAmount: status === "Transferred" ? 25000 : 50000,
    isOverdue: false,
    requiresShopAction: ["PendingReview", "AwaitingTransfer"].includes(status),
    shopAction:
      status === "PendingReview"
        ? "ReviewRequired"
        : status === "AwaitingTransfer"
          ? "TransferRequired"
          : "Closed",
    bankBin: "970436",
    accountNumber: "0123456789",
    accountHolder: "NGUYEN MINH ANH",
    bankName: "Vietcombank",
  }));
}
export function checkoutContext(shop: CartShop): CheckoutContext {
  return {
    shop,
    isSimulatedPayment: true,
    onlinePaymentAvailable: true,
    policy: {
      levelId: uid(0),
      levelName: "Thành viên Xanh",
      levelRank: 1,
      isDefaultLevel: false,
      payAtShopAllowed: true,
      requiredDeposit: 0,
      freeOrderAmount: 100000,
      maxDepositOrderAmount: 500000,
      ranges: [
        { id: uid(0), fromOrderAmount: 100000, toOrderAmount: 500000, depositAmount: 20000 },
      ],
    },
  };
}
export function checkoutQuote(shop: CartShop): CheckoutQuote {
  return {
    ...checkoutContext(shop),
    id: uid(0),
    createdAt: past,
    expiresAt: future,
    pickup: {
      distanceKm: 1.2,
      etaMinutes: 10,
      bufferMinutes: 15,
      softDeadlineAt: future,
      hardDeadlineAt: future,
      dealEndsAt: future,
      bufferWasCapped: false,
    },
  };
}
