import { response14 as adminProductSeed } from "../factories/products-productApi";
import type {
  FlashDeal,
  ShopFlashDealDetail,
  ShopFlashDealListItem,
  ShopEligibleProduct,
} from "@/features/flash-deals/types/flashDeal.types";
import type {
  Product,
  ProductListItem,
  ProductListManagement,
  ProductContentManagement,
} from "@/features/products/types/product.types";
import type { ShopOrder } from "@/features/shop-orders/types/shopOrder.types";
import type { CartItem } from "@/features/cart/types/cart.types";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { core, saveCore } from "../coreState";
import { id, type Row } from "../db";
import { checkoutContext, checkoutQuote, orderStatuses } from "../factories/core";
import { future, past } from "../factories/values";
import { emit } from "../realtime";
import { role } from "../auth";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
export interface CommerceResponse {
  data: unknown;
  status?: number;
}
function recalc() {
  for (const shop of core.cart.shops) {
    shop.items.forEach((item) => {
      item.subtotal = item.quantity * item.unitPrice;
    });
    shop.totalQuantity = shop.items.reduce((s, i) => s + i.quantity, 0);
    shop.subtotal = shop.items.reduce((s, i) => s + i.subtotal, 0);
  }
  core.cart.shops = core.cart.shops.filter((shop) => shop.items.length > 0);
  core.cart.totalQuantity = core.cart.shops.reduce((s, i) => s + i.totalQuantity, 0);
}
function changed(orderId?: string) {
  saveCore();
  emit(REALTIME_EVENTS.CART_CHANGED, {});
  emit(REALTIME_EVENTS.CUSTOMER_ORDERS_CHANGED, { orderId: orderId || id(0) });
  emit(REALTIME_EVENTS.SHOP_ORDERS_CHANGED, { shopId: id(0), orderId });
  emit(REALTIME_EVENTS.SHOP_PENDING_ORDERS_CHANGED, { shopId: id(0) });
}
const countStatuses = () =>
  Object.fromEntries(
    orderStatuses.map((status) => [status, core.orders.filter((o) => o.status === status).length]),
  );
function shopOrder(order: CheckoutOrder): ShopOrder {
  return {
    order,
    customerName: "Nguyễn Minh Anh",
    customerPhone: "0901234567",
    confirmationDeadlineAt: future,
    isOwner: role() === "storeOwner",
    payments: [
      ...(core.remainingPayments[order.id] || []),
      {
        id: order.id,
        type: "FullPayment",
        method: order.paymentMethod,
        status: order.paymentStatus,
        amount: order.paidAmount || 0,
        paidAt: past,
        checkoutUrl: order.checkoutUrl,
        qrCode: "MOCK-SAVEBITE-PAYMENT",
      },
    ],
    history: [
      { toStatus: order.status, createdAt: past, actorName: "Nguyễn Minh Anh", actorRole: "user" },
    ],
  };
}

function shopDeal(deal: FlashDeal): ShopFlashDealDetail {
  return {
    id: deal.id,
    shopId: deal.shopId,
    productId: deal.productId,
    productName: deal.productName,
    description: deal.description,
    categoryName: deal.categoryName,
    primaryImageUrl: deal.productImageUrl,
    productImageUrls: deal.productImageUrls || [],
    saleStartTime: deal.saleStartTime,
    orderEndTime: deal.orderEndTime,
    shopClosingTime: deal.shopClosingTime,
    status: deal.status,
    createdAt: deal.createdAt,
    updatedAt: past,
    variants: deal.variants.map((v) => ({
      id: v.id,
      variantId: v.variantId,
      sku: v.sku,
      variantName: v.variantName,
      imageUrl: v.imageUrl,
      attributes: v.attributes,
      originalPrice: v.originalPrice,
      dealPrice: v.dealPrice,
      discountPercent: v.discountPercent,
      totalQuantity: v.totalQuantity,
      reservedQuantity: 0,
      soldQuantity: v.soldQuantity,
      availableQuantity: v.availableQuantity,
      status: v.status,
    })),
  };
}
function dealList(deal: FlashDeal): ShopFlashDealListItem {
  return {
    id: deal.id,
    productId: deal.productId,
    productName: deal.productName,
    productImageUrl: deal.productImageUrl,
    categoryName: deal.categoryName,
    saleStartTime: deal.saleStartTime,
    orderEndTime: deal.orderEndTime,
    shopClosingTime: deal.shopClosingTime,
    status: deal.status,
    minDealPrice: deal.minDealPrice,
    maxOriginalPrice: deal.maxOriginalPrice,
    maxDiscountPercent: deal.maxDiscountPercent,
    totalQuantity: 30,
    reservedQuantity: 0,
    soldQuantity: 5,
    availableQuantity: 25,
    createdAt: deal.createdAt,
  };
}
function productList(p: Product): ProductListItem {
  return {
    id: p.id,
    categoryId: p.categoryId,
    categoryName: p.categoryName,
    name: p.name,
    status: p.status,
    variantCount: p.variants.length,
    minOriginalPrice: p.variants[0]?.originalPrice || 0,
    minDealPrice: p.variants[0]?.dealPrice || 0,
    maxDealPrice: p.variants[0]?.dealPrice || 0,
    updatedAt: p.updatedAt,
    images: p.images,
  };
}
function eligible(p: Product): ShopEligibleProduct {
  return {
    id: p.id,
    name: p.name,
    description: p.description,
    categoryName: p.categoryName,
    imageUrl: p.images[0]?.url,
    variants: p.variants.map((v) => ({
      id: v.id,
      sku: v.sku,
      variantName: "Tiêu chuẩn",
      originalPrice: v.originalPrice,
      imageUrl: p.images[0]?.url,
    })),
  };
}
export function commerce(
  method: string,
  path: string,
  input: Row,
  query: URLSearchParams,
): CommerceResponse | undefined {
  if (path === "/orders/my-shop") return { data: id(0) };
  if (path === "/cart/summary")
    return {
      data: {
        totalQuantity: core.cart.totalQuantity,
        totalFlashDeals: core.cart.shops.reduce(
          (s, shop) => s + new Set(shop.items.map((i) => i.flashDealId)).size,
          0,
        ),
      },
    };
  if (path === "/cart/page")
    return {
      data: {
        totalQuantity: core.cart.totalQuantity,
        totalFlashDeals: core.cart.shops.reduce((s, x) => s + x.items.length, 0),
        page: { items: core.cart.shops, nextCursor: null, hasMore: false, pageSize: 5 },
      },
    };
  if (path === "/cart/items" && method === "POST") {
    const deal = core.deals.find((d) => d.variants.some((v) => v.id === input.flashDealVariantId));
    if (!deal) return { data: { message: "Flash deal not found" }, status: 404 };
    let shop = core.cart.shops[0];
    if (!shop) {
      const context = checkoutContext({
        shopId: id(0),
        shopName: deal.shopName,
        shopLogoUrl: null,
        shopAddress: deal.shopAddress,
        items: [],
        totalQuantity: 0,
        subtotal: 0,
        canCheckout: true,
      });
      shop = context.shop;
      core.cart.shops.push(shop);
    }
    const quantity = Math.max(1, Math.min(25, Number(input.quantity) || 1));
    const item = shop.items.find((i) => i.flashDealVariantId === input.flashDealVariantId);
    if (item) item.quantity = Math.min(25, item.quantity + quantity);
    else {
      const row: CartItem = {
        id: deal.id,
        flashDealId: deal.id,
        flashDealVariantId: deal.variants[0].id,
        productId: deal.productId,
        productName: deal.productName,
        variantName: "Tiêu chuẩn",
        imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
        originalPrice: 50000,
        unitPrice: deal.minDealPrice,
        quantity,
        availableQuantity: 25,
        subtotal: quantity * deal.minDealPrice,
        orderEndTime: future,
        canUpdateQuantity: true,
        isAvailable: true,
        unavailableReason: null,
      };
      shop.items.push(row);
    }
    recalc();
    changed();
    return { data: core.cart };
  }
  if (path.startsWith("/cart/items/")) {
    const itemId = path.split("/").at(-1);
    const shop = core.cart.shops.find((s) => s.items.some((i) => i.id === itemId));
    if (!shop) return { data: { message: "Cart item not found" }, status: 404 };
    if (method === "DELETE") shop.items = shop.items.filter((i) => i.id !== itemId);
    else {
      const item = shop.items.find((i) => i.id === itemId);
      if (item)
        item.quantity = Math.max(1, Math.min(item.availableQuantity, Number(input.quantity) || 1));
    }
    recalc();
    changed();
    return {
      data: {
        shopId: shop.shopId,
        shop: shop.items.length ? shop : null,
        summary: {
          totalQuantity: core.cart.totalQuantity,
          totalFlashDeals: core.cart.shops.reduce((s, x) => s + x.items.length, 0),
        },
      },
    };
  }
  if (path === "/checkout/context" || path === "/checkout/quotes") {
    const shop =
      core.cart.shops.find((s) => s.shopId === (input.shopId || query.get("shopId"))) ||
      core.cart.shops[0];
    if (!shop) return { data: { message: "Cart is empty" }, status: 404 };
    return { data: path.endsWith("quotes") ? checkoutQuote(shop) : checkoutContext(shop) };
  }
  if (path === "/checkout/orders" && method === "POST") {
    const shop = core.cart.shops[0];
    if (!shop) return { data: { message: "Cart is empty" }, status: 409 };
    const order: CheckoutOrder = {
      id: id(core.orders.length + 1),
      orderCode: `SB${1001 + core.orders.length}`,
      shopId: shop.shopId,
      shopName: shop.shopName,
      shopAddress: shop.shopAddress,
      createdAt: new Date().toISOString(),
      paymentMethod: input.paymentMethod === "PayAtShop" ? "PayAtShop" : "OnlinePayment",
      status: input.paymentMethod === "PayAtShop" ? "Pending" : "AwaitingPayment",
      paymentStatus: input.paymentMethod === "PayAtShop" ? "Unpaid" : "Pending",
      totalAmount: shop.subtotal,
      paidAmount: 0,
      outstandingAmount: shop.subtotal,
      depositAmount: 0,
      requiredOnlineAmount: input.paymentMethod === "PayAtShop" ? 0 : shop.subtotal,
      remainingAtShop: input.paymentMethod === "PayAtShop" ? shop.subtotal : 0,
      paymentExpiresAt: future,
      softDeadlineAt: future,
      hardDeadlineAt: future,
      canCancel: true,
      paymentReviewRequired: false,
      items: shop.items.map((item) => ({
        orderItemId: item.id,
        flashDealVariantId: item.flashDealVariantId,
        flashDealId: item.flashDealId,
        productName: item.productName,
        variantName: item.variantName,
        unitPrice: item.unitPrice,
        quantity: item.quantity,
        subtotal: item.subtotal,
      })),
    };
    order.checkoutUrl = `/checkout/simulation?orderId=${order.id}`;
    core.orders.unshift(order);
    core.cart.shops.shift();
    recalc();
    changed(order.id);
    return { data: order };
  }
  if (path === "/orders" || path === "/orders/page")
    return {
      data:
        path === "/orders"
          ? core.orders
          : {
              page: { items: core.orders, nextCursor: null, hasMore: false, pageSize: 10 },
              statusCounts: countStatuses(),
            },
    };
  const orderMatch = path.match(/^\/orders\/([^/]+)(?:\/cancellations)?$/);
  if (orderMatch) {
    const order = core.orders.find((o) => o.id === orderMatch[1]);
    if (!order) return { data: { message: "Order not found" }, status: 404 };
    if (method === "POST") {
      order.status = "Cancelled";
      order.canCancel = false;
      changed(order.id);
    }
    return { data: order };
  }
  const remainingSimulation = path.match(
    /^\/payments\/simulation\/shops\/[^/]+\/([^/]+)\/remaining$/,
  );
  if (remainingSimulation) {
    const order = core.orders.find((o) => o.id === remainingSimulation[1]);
    if (!order) return { data: { message: "Order not found" }, status: 404 };
    if (input.outcome === "paid") {
      order.paidAmount = order.totalAmount;
      order.outstandingAmount = 0;
      order.remainingAtShop = 0;
      order.paymentStatus = "Succeeded";
      for (const payment of core.remainingPayments[order.id] || []) {
        payment.status = "Succeeded";
        payment.paidAt = new Date().toISOString();
        payment.collectedAmount = payment.amount;
      }
    }
    changed(order.id);
    return { data: null };
  }
  const shopMatch = path.match(/^\/orders\/shops\/([^/]+)(?:\/([^/]+))?(?:\/([^/]+))?$/);
  if (shopMatch) {
    if (shopMatch[2] === "pending-count")
      return { data: core.orders.filter((o) => o.status === "Pending").length };
    if (!shopMatch[2])
      return {
        data: {
          page: {
            items: core.orders.map(shopOrder),
            page: 1,
            pageSize: 10,
            totalItems: core.orders.length,
            totalPages: 2,
            hasPrevious: false,
            hasNext: true,
          },
          statusCounts: countStatuses(),
        },
      };
    const order = core.orders.find((o) => o.id === shopMatch[2]);
    if (!order) return { data: { message: "Order not found" }, status: 404 };
    if (method === "POST") {
      if (shopMatch[3] === "remaining-payment") {
        const paymentId = id(2000 + core.orders.indexOf(order));
        core.remainingPayments[order.id] = [
          {
            id: paymentId,
            type: "Remaining",
            method: "OnlinePayment",
            status: "Pending",
            amount: order.outstandingAmount || order.totalAmount,
            checkoutUrl: `/checkout/simulation?orderId=${order.id}&paymentId=${paymentId}`,
            qrCode: "MOCK-SAVEBITE-REMAINING-PAYMENT",
            collectedAmount: 0,
          },
        ];
      }
      const actions: Record<string, string> = {
        confirm: "Confirmed",
        confirmations: "Confirmed",
        reject: "Rejected",
        rejections: "Rejected",
        prepare: "Preparing",
        preparations: "Preparing",
        ready: "ReadyForPickup",
        "ready-for-pickup": "ReadyForPickup",
        complete: "Completed",
        completions: "Completed",
        cancel: "Cancelled",
        cancellations: "Cancelled",
        "no-show": "NoShow",
      };
      order.status =
        shopMatch[3] === "preparation" && typeof input.status === "string"
          ? input.status
          : actions[shopMatch[3]] || order.status;
      if (order.status === "Completed") {
        order.paidAmount = order.totalAmount;
        order.outstandingAmount = 0;
        order.remainingAtShop = 0;
        order.paymentStatus = "Succeeded";
        order.completedAt = new Date().toISOString();
      }
      changed(order.id);
    }
    return { data: shopOrder(order) };
  }
  const payment = path.match(/^\/payments\/(?:simulation\/|orders\/)([^/]+)(?:\/payment-link)?$/);
  if (payment) {
    const order = core.orders.find((o) => o.id === payment[1]);
    if (!order) return { data: { message: "Order not found" }, status: 404 };
    if (method === "POST" && "outcome" in input) {
      if (!input.paymentId)
        order.status =
          input.outcome === "paid"
            ? "Pending"
            : input.outcome === "expired"
              ? "Expired"
              : "Cancelled";
      order.paymentStatus =
        input.outcome === "paid"
          ? "Succeeded"
          : input.outcome === "expired"
            ? "Expired"
            : "Cancelled";
      order.paidAmount = input.outcome === "paid" ? order.totalAmount : 0;
      order.outstandingAmount = input.outcome === "paid" ? 0 : order.totalAmount;
      if (input.outcome === "paid") {
        order.remainingAtShop = 0;
        if (input.paymentId)
          for (const value of core.remainingPayments[order.id] || [])
            if (value.id === input.paymentId) {
              value.status = "Succeeded";
              value.collectedAmount = value.amount;
              value.paidAt = new Date().toISOString();
            }
      }
      changed(order.id);
    }
    return { data: order };
  }
  if (path === "/products/options")
    return {
      data: {
        shop: { id: id(0), name: "Bếp Xanh Sài Gòn" },
        categories: [
          { id: id(0), name: "Bánh mì" },
          { id: id(1), name: "Rau củ" },
          { id: id(2), name: "Bánh ngọt" },
        ],
      },
    };
  if (path === "/products/summary")
    return {
      data: {
        totalProducts: core.products.length,
        activeProducts: core.products.filter((p) => p.status === 0).length,
        hiddenProducts: core.products.filter((p) => p.status !== 0).length,
        totalVariants: core.products.reduce((s, p) => s + p.variants.length, 0),
      },
    };
  if (path === "/products" && method === "GET")
    return {
      data: {
        items: core.products.map(productList),
        page: 1,
        pageSize: 10,
        totalItems: core.products.length,
        totalPages: 2,
      },
    };
  if (path === "/products/admin") {
    const items: ProductListManagement[] = core.products.map((p) => ({
      ...productList(p),
      shopId: p.shopId,
      shopName: "Bếp Xanh Sài Gòn",
      createdAt: p.createdAt,
      latestReview: p.latestReview,
    }));
    return {
      data: {
        items,
        page: 1,
        pageSize: 10,
        totalItems: items.length,
        totalPages: Math.ceil(items.length / 10),
      },
    };
  }
  const adminProduct = path.match(/^\/products\/admin\/([^/]+)$/);
  if (adminProduct) {
    const product = core.products.find((p) => p.id === adminProduct[1]);
    if (!product) return { data: { message: "Product not found" }, status: 404 };
    const data: ProductContentManagement = {
      ...structuredClone(adminProductSeed.data),
      ...product,
      shopName: "Bếp Xanh Sài Gòn",
    };
    return { data };
  }
  const productMatch = path.match(
    /^\/products\/([^/]+)(?:\/(status|images|attribute-values|values|variants))?(?:\/([^/]+))?(?:\/image)?$/,
  );
  if (productMatch && !["admin", "summary", "options"].includes(productMatch[1])) {
    const product = core.products.find((p) => p.id === productMatch[1]);
    if (!product) return { data: { message: "Product not found" }, status: 404 };
    if (method === "DELETE") core.products = core.products.filter((p) => p.id !== product.id);
    else if (method !== "GET") {
      for (const k of ["name", "description", "categoryId"] as const)
        if (typeof input[k] === "string") product[k] = input[k];
      if (typeof input.status === "number") product.status = input.status;
      const variant = product.variants.find((v) => v.id === productMatch[3]);
      if (variant) {
        if (typeof input.originalPrice === "number") variant.originalPrice = input.originalPrice;
        if (typeof input.dealPrice === "number") variant.dealPrice = input.dealPrice;
        if (typeof input.isActive === "boolean") variant.isActive = input.isActive;
      }
    }
    saveCore();
    return { data: product };
  }
  if (path === "/products" && method === "POST") {
    const template = core.products[0];
    if (!template) return;
    const product = {
      ...structuredClone(template),
      id: id(core.products.length + 1),
      name: String(input.name || input.Name || "Sản phẩm mới"),
    };
    core.products.unshift(product);
    saveCore();
    return { data: product };
  }
  if (path === "/flash-deals" && method === "POST") {
    const product = core.products.find((p) => p.id === input.productId);
    if (!product) return { data: { message: "Product not found" }, status: 404 };
    const template = core.deals[0];
    if (!template) return;
    const deal = {
      ...structuredClone(template),
      id: id(core.deals.length + 1),
      productId: product.id,
      productName: product.name,
      status: input.isImmediate ? 1 : 0,
      orderEndTime: typeof input.orderEndTime === "string" ? input.orderEndTime : future,
      shopClosingTime: typeof input.shopClosingTime === "string" ? input.shopClosingTime : future,
    };
    core.deals.unshift(deal);
    saveCore();
    return {
      data: shopDeal(deal),
    };
  }
  if (path === "/flash-deals" && method === "GET")
    return {
      data: {
        deals: core.deals,
        cursor1: "0",
        cursor2: "0",
        hasOlder: true,
        hasNewer: false,
        prependedCount: 0,
        newDealsCount: 0,
      },
    };
  if (/^\/shops\/[^/]+\/flash-deals$/.test(path)) return { data: core.deals };
  if (path === "/flash-deals/shop")
    return {
      data: {
        items: core.deals.map(dealList),
        page: 1,
        pageSize: 10,
        totalItems: core.deals.length,
      },
    };
  if (path === "/flash-deals/eligible-products")
    return {
      data: core.products.map(eligible),
    };
  const dealMatch = path.match(/^\/flash-deals\/(?:shop\/)?([^/]+)(?:\/(hides|cancellations))?$/);
  if (dealMatch) {
    const deal = core.deals.find((d) => d.id === dealMatch[1]);
    if (!deal) return { data: { message: "Flash deal not found" }, status: 404 };
    if (dealMatch[2] === "hides") deal.status = method === "DELETE" ? 1 : 2;
    if (dealMatch[2] === "cancellations") deal.status = 5;
    if (method === "PUT") {
      for (const k of ["saleStartTime", "orderEndTime", "shopClosingTime"] as const)
        if (typeof input[k] === "string") deal[k] = input[k];
    }
    saveCore();
    if (path.includes("/shop/") || method === "PUT")
      return {
        data: shopDeal(deal),
      };
    return { data: deal };
  }
  if (/^\/orders\/[^/]+\/refunds\/requests$/.test(path) && method === "POST") {
    const orderId = path.split("/")[2];
    const order = core.orders.find((o) => o.id === orderId);
    const template = core.refunds[0];
    if (!order || !template) return { data: { message: "Order not found" }, status: 404 };
    const refund = {
      ...structuredClone(template),
      id: id(core.refunds.length + 1),
      orderId: order.id,
      orderCode: order.orderCode,
      status: "AwaitingCustomerDetails",
      requiresShopAction: false,
    };
    core.refunds.unshift(refund);
    saveCore();
    return { data: refund };
  }
  if (path === "/refunds" || path === "/refunds/shop")
    return {
      data: {
        items: core.refunds,
        page: 1,
        pageSize: 10,
        totalItems: core.refunds.length,
        totalPages: 1,
      },
    };
  if (path === "/refunds/shop/pending-count")
    return { data: core.refunds.filter((r) => r.requiresShopAction).length };
  if (path === "/refunds/pending-details-count")
    return { data: core.refunds.filter((r) => r.status === "AwaitingCustomerDetails").length };
  if (/^\/orders\/(?:shop\/)?[^/]+\/refunds$/.test(path))
    return { data: core.refunds.filter((r) => r.orderId === path.split("/").at(-2)) };
  const refund = path.match(
    /^\/refunds\/(?:shop\/)?([^/]+)(?:\/(destination(?:\/confirm)?|withdraw|approve|reject|transfer))?$/,
  );
  if (refund) {
    const value = core.refunds.find((r) => r.id === refund[1]);
    if (!value) return { data: { message: "Refund not found" }, status: 404 };
    if (method === "POST") {
      const states: Record<string, string> = {
        destination: "PendingVerification",
        "destination/confirm": "PendingReview",
        withdraw: "Withdrawn",
        approve: "AwaitingTransfer",
        reject: "Rejected",
        transfer: "Transferred",
      };
      value.status = states[refund[2]] || value.status;
      if (value.status === "Transferred") {
        value.transferredAmount = value.amount;
        value.transferredAt = new Date().toISOString();
      }
      if (refund[2] === "destination/confirm")
        value.destinationConfirmedAt = new Date().toISOString();
      if (refund[2] === "approve" || refund[2] === "reject")
        value.reviewedAt = new Date().toISOString();
      if (typeof input.reason === "string") value.reason = input.reason;
      for (const k of [
        "bankBin",
        "bankName",
        "accountNumber",
        "accountHolder",
        "transferReference",
      ] as const)
        if (typeof input[k] === "string") value[k] = input[k];
      value.requiresShopAction = ["PendingReview", "AwaitingTransfer"].includes(value.status);
      saveCore();
    }
    return { data: value };
  }
  return;
}
