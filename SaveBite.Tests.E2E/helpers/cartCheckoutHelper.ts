import type { APIRequestContext } from "@playwright/test";
import { API_URL, loginViaApi, TEST_USERS } from "./authHelper.js";

export interface ActiveDealInfo {
  id: string;
  productName: string;
  shopId: string;
  shopName: string;
  variantId: string;
  unitPrice: number;
  availableQuantity: number;
}

interface CartItemDto {
  id: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

interface CartShopDto {
  shopId: string;
  shopName: string;
  items: CartItemDto[];
}

interface CartPageDto {
  shops: CartShopDto[];
  nextCursor?: string | null;
  hasMore?: boolean;
}

interface FlashDealVariantApiDto {
  id: string;
  dealPrice: number;
  availableQuantity: number;
  isPurchasable?: boolean;
}

interface FlashDealApiDto {
  id: string;
  productName: string;
  shopId: string;
  shopName: string;
  isPurchasable?: boolean;
  variants: FlashDealVariantApiDto[];
}

let cachedCustomerToken: { token: string; expiresAt: number } | null = null;

async function getCustomerToken(request: APIRequestContext): Promise<string> {
  const now = Date.now();
  if (cachedCustomerToken && cachedCustomerToken.expiresAt > now + 60_000) {
    return cachedCustomerToken.token;
  }
  const tokens = await loginViaApi(
    request,
    TEST_USERS.customer.email,
    TEST_USERS.customer.password,
  );
  cachedCustomerToken = { token: tokens.accessToken, expiresAt: now + 15 * 60_000 };
  return tokens.accessToken;
}

/**
 * Xóa sạch tất cả các món trong giỏ hàng của Customer qua API
 */
export async function clearCustomerCartViaApi(
  request: APIRequestContext,
  accessToken?: string,
): Promise<void> {
  const token = accessToken ?? (await getCustomerToken(request));
  const headers = { Authorization: `Bearer ${token}` };

  let cursor: string | null | undefined = undefined;
  for (let guard = 0; guard < 10; guard++) {
    const url = cursor
      ? `${API_URL}/cart/page?pageSize=20&cursor=${encodeURIComponent(cursor)}`
      : `${API_URL}/cart/page?pageSize=20`;
    const res = await request.get(url, { headers });
    if (!res.ok()) break;

    const body = (await res.json()) as any;
    const shops = body.data?.page?.items ?? body.data?.shops ?? [];
    if (shops.length === 0) break;

    for (const shop of shops) {
      for (const item of shop.items ?? []) {
        await request.delete(`${API_URL}/cart/items/${encodeURIComponent(item.id)}`, { headers });
      }
    }

    const hasMore = body.data?.page?.hasMore ?? body.data?.hasMore ?? false;
    const nextCursor = body.data?.page?.nextCursor ?? body.data?.nextCursor;
    if (!hasMore || !nextCursor) break;
    cursor = nextCursor;
  }
}

/**
 * Thêm một sản phẩm vào giỏ hàng của Customer qua API
 */
export async function addItemToCartViaApi(
  request: APIRequestContext,
  variantId: string,
  quantity: number = 1,
  accessToken?: string,
): Promise<void> {
  const token = accessToken ?? (await getCustomerToken(request));
  const headers = { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };

  const res = await request.post(`${API_URL}/cart/items`, {
    headers,
    data: { flashDealVariantId: variantId, quantity },
  });
  if (!res.ok()) {
    throw new Error(`Failed to add item to cart via API (${res.status()}): ${await res.text()}`);
  }
}

/**
 * Tìm một Flash Deal đang mở bán, có hàng tồn sẵn để test
 * Đảm bảo deal thuộc shop mà customer KHÔNG phải là chủ shop hay nhân viên (tránh 403 Forbidden)
 */
export async function getActiveDealForCheckout(
  request: APIRequestContext,
): Promise<ActiveDealInfo | null> {
  const token = await getCustomerToken(request);
  const authHeaders = { Authorization: `Bearer ${token}` };

  // 1. Lấy danh sách shop mà customer này đang làm nhân viên để loại trừ
  const excludedShopIds = new Set<string>();
  try {
    const staffRes = await request.get(`${API_URL}/shop-staff/me/shops`, { headers: authHeaders });
    if (staffRes.ok()) {
      const staffJson = await staffRes.json();
      for (const item of staffJson.data ?? []) {
        if (item.shopId) excludedShopIds.add(String(item.shopId).toLowerCase());
        if (item.id) excludedShopIds.add(String(item.id).toLowerCase());
      }
    }
  } catch {
    // ignore
  }

  const url = `${API_URL}/flash-deals?latitude=10.012452&longitude=105.732432&radiusInKm=50&limit=30`;
  const res = await request.get(url, { headers: authHeaders });
  if (!res.ok()) return null;

  const json = (await res.json()) as any;
  const deals = json.data?.deals ?? [];

  for (const deal of deals) {
    if (!deal.isPurchasable) continue;
    if (deal.shopId && excludedShopIds.has(String(deal.shopId).toLowerCase())) continue;

    const variant = deal.variants?.find(
      (v: any) => (v.isPurchasable ?? true) && v.availableQuantity > 0,
    );
    if (!variant) continue;

    // Dry-run: test-add 1 món qua API để chắc chắn Customer mua được (không dính IsShopOperator 403)
    try {
      const testAdd = await request.post(`${API_URL}/cart/items`, {
        headers: { ...authHeaders, "Content-Type": "application/json" },
        data: { flashDealVariantId: variant.id, quantity: 1 },
      });

      if (testAdd.ok()) {
        // Dọn sạch giỏ hàng ngay sau khi test thử
        await clearCustomerCartViaApi(request, token);

        return {
          id: deal.id,
          productName: deal.productName,
          shopId: deal.shopId,
          shopName: deal.shopName,
          variantId: variant.id,
          unitPrice: variant.dealPrice,
          availableQuantity: variant.availableQuantity,
        };
      }
    } catch {
      // Nếu không add được thì thử deal tiếp theo
    }
  }

  // Fallback nếu không có deal nào qua được dry-run
  for (const deal of deals) {
    if (!deal.isPurchasable) continue;
    if (deal.shopId && excludedShopIds.has(String(deal.shopId).toLowerCase())) continue;
    const variant = deal.variants?.find(
      (v: any) => (v.isPurchasable ?? true) && v.availableQuantity > 0,
    );
    if (variant) {
      return {
        id: deal.id,
        productName: deal.productName,
        shopId: deal.shopId,
        shopName: deal.shopName,
        variantId: variant.id,
        unitPrice: variant.dealPrice,
        availableQuantity: variant.availableQuantity,
      };
    }
  }

  return null;
}
