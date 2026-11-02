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
import { response0 as shopSeed } from "./shops-shopApi";
const uid = (n: number) => `00000000-0000-4000-8000-${String(n + 1).padStart(12, "0")}`;
const names = [
  "Bánh mì rau củ",
  "Túi bánh cuối ngày",
  "Hộp rau tươi Đà Lạt",
  "Combo bánh mì và sữa hạt nguyên chất – phần ăn gia đình đặc biệt",
];
const demoImages = [
  "/images/deals/banh-mi/bm-overview.jpg",
  "/images/deals/banh-mi/bm-thuong-khong-cay.jpg",
  "/images/deals/banh-mi/bm-thuong-cay-vua.jpg",
  "/images/deals/banh-mi/bm-lon-khong-cay.jpg",
  "/images/deals/banh-mi/bm-lon-cay-vua.jpg",
];
const demoOptions = [
  { size: "Thường", spice: "Không cay", extra: 0, original: 50000, stock: 12, sold: 8 },
  { size: "Thường", spice: "Cay vừa", extra: 2000, original: 50000, stock: 7, sold: 5 },
  { size: "Lớn", spice: "Không cay", extra: 9000, original: 65000, stock: 9, sold: 3 },
  { size: "Lớn", spice: "Cay vừa", extra: 11000, original: 65000, stock: 4, sold: 6 },
];
export function makeProducts(): Product[] {
  const products: Product[] = Array.from({ length: 18 }, (_, n) => ({
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
  })).map((product, n) => {
    if (n !== 1) return product;
    return {
      ...product,
      images: demoImages.map((url, i) => ({ id: uid(300 + i), url, source: "product" as const })),
      attributes: [
        {
          id: uid(200),
          name: "Size",
          values: [
            { id: uid(201), value: "Thường" },
            { id: uid(202), value: "Lớn" },
          ],
        },
        {
          id: uid(205),
          name: "Độ cay",
          values: [
            { id: uid(203), value: "Không cay" },
            { id: uid(204), value: "Cay vừa" },
          ],
        },
      ],
      variants: demoOptions.map((option, i) => ({
        id: i === 0 ? product.id : uid(100 + i),
        sku: `BM-2-${i + 1}`,
        originalPrice: option.original,
        dealPrice: 26000 + option.extra,
        isActive: true,
        valueIds: [
          uid(option.size === "Thường" ? 201 : 202),
          uid(option.spice === "Không cay" ? 203 : 204),
        ],
      })),
    };
  });
  const additionalNames = [
    "Túi bánh ngọt An Nhiên",
    "Bánh mì nguyên cám",
    "Giỏ rau củ Đà Lạt",
    "Hộp salad rau xanh",
  ];
  return products.concat(
    additionalNames.map((name, n) => ({
      ...structuredClone(products[0]),
      id: uid(1000 + n),
      shopId: uid(n < 2 ? 1 : 2),
      name,
      variants: [
        {
          ...structuredClone(products[0].variants[0]),
          id: uid(1100 + n),
          sku: `CART-${n + 1}`,
          originalPrice: 60000 + n * 5000,
          dealPrice: 30000 + n * 4000,
        },
      ],
      images: [{ id: uid(1200 + n), url: demoImages[n + 1], source: "product" as const }],
    })),
  );
}
export function makeDeals(products: Product[]): FlashDeal[] {
  return products.map((p, n) => ({
    ...structuredClone(dealSeed.data),
    id: p.id,
    productId: p.id,
    productName: p.name,
    shopId: p.shopId,
    shopName: shopSeed.data.items.find((shop) => shop.id === p.shopId)?.name ?? "Bếp Xanh Sài Gòn",
    status: n >= 18 ? 1 : n % 6,
    saleStartTime: past,
    orderEndTime: future,
    shopClosingTime: future,
    productImageUrl: p.images[0]?.url,
    productImageUrls: p.images.map((image) => image.url),
    minDealPrice: Math.min(...p.variants.map((variant) => variant.dealPrice)),
    maxOriginalPrice: Math.max(...p.variants.map((variant) => variant.originalPrice)),
    maxDiscountPercent: Math.max(
      ...p.variants.map((variant) =>
        Math.round((1 - variant.dealPrice / variant.originalPrice) * 100),
      ),
    ),
    isPurchasable: n >= 18 || n % 6 === 1,
    attributeGroups:
      n === 1
        ? p.attributes.map((attribute) => ({
            name: attribute.name,
            values: attribute.values.map((value) => value.value),
          }))
        : dealSeed.data.attributeGroups,
    variants: p.variants.map((variant, i) => ({
      id: variant.id,
      variantId: variant.id,
      sku: variant.sku,
      variantName: n === 1 ? `${demoOptions[i].size} · ${demoOptions[i].spice}` : "Tiêu chuẩn",
      imageUrl: n === 1 ? demoImages[i + 1] : p.images[0]?.url,
      attributes:
        n === 1 ? { Size: demoOptions[i].size, "Độ cay": demoOptions[i].spice } : undefined,
      originalPrice: variant.originalPrice,
      dealPrice: variant.dealPrice,
      discountPercent: Math.round((1 - variant.dealPrice / variant.originalPrice) * 100),
      totalQuantity: n === 1 ? demoOptions[i].stock + demoOptions[i].sold : 30,
      soldQuantity: n === 1 ? demoOptions[i].sold : 5,
      availableQuantity: n === 1 ? demoOptions[i].stock : 25,
      status: 0,
      isPurchasable: true,
    })),
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
  const others: CartShop[] = shopSeed.data.items.slice(1).map((store, n) => {
    const items = deals
      .filter((deal) => deal.shopId === store.id)
      .slice(0, 2)
      .map((deal, i) => {
        const variant = deal.variants[0];
        const quantity = n === 0 ? (i === 0 ? 2 : 1) : i === 0 ? 1 : 3;
        return {
          id: deal.id,
          flashDealId: deal.id,
          flashDealVariantId: variant.id,
          productId: deal.productId,
          productName: deal.productName,
          variantName: variant.variantName ?? "Tiêu chuẩn",
          imageUrl: variant.imageUrl ?? deal.productImageUrl ?? null,
          originalPrice: variant.originalPrice,
          unitPrice: variant.dealPrice,
          quantity,
          availableQuantity: variant.availableQuantity,
          subtotal: quantity * variant.dealPrice,
          orderEndTime: deal.orderEndTime,
          canUpdateQuantity: true,
          isAvailable: true,
          unavailableReason: null,
        };
      });
    return {
      shopId: store.id,
      shopName: store.name,
      shopLogoUrl: store.logoUrl ?? null,
      shopAddress: store.address,
      latitude: store.latitude,
      longitude: store.longitude,
      items,
      totalQuantity: items.reduce((sum, item) => sum + item.quantity, 0),
      subtotal: items.reduce((sum, item) => sum + item.subtotal, 0),
      canCheckout: true,
    };
  });
  const shops = [shop, ...others];
  return { shops, totalQuantity: shops.reduce((sum, store) => sum + store.totalQuantity, 0) };
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
    id: shop.shopId,
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
