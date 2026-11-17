import {
  chromium,
  type APIRequestContext,
  type Browser,
  type BrowserContext,
  type Page,
  type TestInfo,
} from "@playwright/test";
import { LoginPage } from "../pages/LoginPage.js";
import { API_URL, loginViaApi, TEST_USERS } from "./authHelper.js";

export interface DemoShopAndDeal {
  shopId: string;
  shopName: string;
  latitude: number;
  longitude: number;
  dealId: string;
  productName: string;
}

interface ShopProfileDto {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
}

interface FlashDealVariantDto {
  id: string;
  availableQuantity: number;
  isPurchasable?: boolean;
}

interface FlashDealDto {
  id: string;
  productName: string;
  orderEndTime: string;
  status: number | string;
  isPurchasable?: boolean;
  variants: FlashDealVariantDto[];
}

interface CartPageDto {
  shops?: Array<{
    shopId: string;
    items?: Array<{ id: string }>;
  }>;
  nextCursor?: string | null;
  hasMore?: boolean;
}

/**
 * Tìm shop của Store Owner (owner@gmail.com), lấy tọa độ shop + 1 Flash Deal đang bán,
 * đồng thời dọn sạch giỏ hàng cũ của shop đó cho Customer để luồng checkout luôn sạch.
 */
export async function prepareRealtimeOrderDemoData(
  request: APIRequestContext,
): Promise<DemoShopAndDeal> {
  const ownerTokens = await loginViaApi(
    request,
    TEST_USERS.storeOwner.email,
    TEST_USERS.storeOwner.password,
  );
  const ownerHeaders = { Authorization: `Bearer ${ownerTokens.accessToken}` };

  const myShopRes = await request.get(`${API_URL}/orders/my-shop`, {
    headers: ownerHeaders,
  });
  if (!myShopRes.ok()) {
    throw new Error(
      `Cannot fetch Store Owner shop (${myShopRes.status()}): ${await myShopRes.text()}`,
    );
  }
  const myShopJson = (await myShopRes.json()) as { data?: string };
  const shopId = myShopJson.data;
  if (!shopId) {
    throw new Error("Store Owner account has no shopId returned from /api/orders/my-shop.");
  }

  const profileRes = await request.get(`${API_URL}/shops/${encodeURIComponent(shopId)}`, {
    headers: ownerHeaders,
  });
  if (!profileRes.ok()) {
    throw new Error(`Cannot fetch shop profile (${profileRes.status()})`);
  }
  const profileJson = (await profileRes.json()) as { data?: ShopProfileDto };
  const profile = profileJson.data;
  if (!profile) {
    throw new Error("Shop profile payload is missing data.");
  }

  const dealsRes = await request.get(
    `${API_URL}/shops/${encodeURIComponent(shopId)}/flash-deals`,
    { headers: ownerHeaders },
  );
  if (!dealsRes.ok()) {
    throw new Error(`Cannot fetch shop flash deals (${dealsRes.status()})`);
  }
  const dealsJson = (await dealsRes.json()) as { data?: FlashDealDto[] };
  const deals = dealsJson.data ?? [];

  const minExpiry = Date.now() + 20 * 60_000;
  const activeDeal = deals.find((deal) => {
    const isOnSale =
      deal.status === 1 || String(deal.status).toLowerCase() === "onsale";
    const endTimeMs = Date.parse(deal.orderEndTime);
    const hasStock = deal.variants?.some(
      (v) => v.availableQuantity > 0 && v.isPurchasable !== false,
    );
    return (
      isOnSale &&
      deal.isPurchasable !== false &&
      Number.isFinite(endTimeMs) &&
      endTimeMs > minExpiry &&
      hasStock
    );
  });

  if (!activeDeal) {
    throw new Error(
      `Shop "${profile.name}" (${shopId}) has no active purchasable Flash Deal (>20m left, stock > 0).`,
    );
  }

  // Dọn giỏ hàng cũ thuộc shop này của Customer để đảm bảo đặt đúng 1 sản phẩm sạch
  const customerTokens = await loginViaApi(
    request,
    TEST_USERS.customer.email,
    TEST_USERS.customer.password,
  );
  const customerHeaders = { Authorization: `Bearer ${customerTokens.accessToken}` };

  let cursor: string | null | undefined = undefined;
  for (let guard = 0; guard < 10; guard++) {
    const url = cursor
      ? `${API_URL}/cart/page?pageSize=20&cursor=${encodeURIComponent(cursor)}`
      : `${API_URL}/cart/page?pageSize=20`;
    const cartRes = await request.get(url, { headers: customerHeaders });
    if (!cartRes.ok()) break;

    const cartJson = (await cartRes.json()) as { data?: CartPageDto };
    const cartData = cartJson.data;
    const targetShopCart = cartData?.shops?.find(
      (s) => s.shopId.toLowerCase() === shopId.toLowerCase(),
    );
    for (const item of targetShopCart?.items ?? []) {
      await request.delete(`${API_URL}/cart/items/${encodeURIComponent(item.id)}`, {
        headers: customerHeaders,
      });
    }

    if (!cartData?.hasMore || !cartData?.nextCursor) break;
    cursor = cartData.nextCursor;
  }

  return {
    shopId,
    shopName: profile.name,
    latitude: profile.latitude,
    longitude: profile.longitude,
    dealId: activeDeal.id,
    productName: activeDeal.productName,
  };
}

/**
 * Đăng nhập trên giao diện web qua LoginPage POM.
 */
export async function loginViaUi(
  page: Page,
  baseUrl: string,
  email: string,
  password: string,
): Promise<void> {
  const loginPage = new LoginPage(page);
  await page.goto(`${baseUrl}/login`, { waitUntil: "domcontentloaded" });

  if (!page.url().includes("/login")) {
    return;
  }

  const sessionChecking = page.locator("text=/Checking your session/i");
  try {
    const isChecking = await sessionChecking.isVisible({ timeout: 2000 });
    if (isChecking) {
      await sessionChecking.waitFor({ state: "detached", timeout: 30_000 });
    }
  } catch {
    // ignore
  }

  if (!page.url().includes("/login")) {
    return;
  }

  await loginPage.emailInput.waitFor({ state: "visible", timeout: 30_000 });
  await loginPage.login(email, password);
  await page.waitForFunction(() => window.location.pathname !== "/login", {
    timeout: 20_000,
  });
}

export interface SplitScreenSession {
  baseUrl: string;
  leftBrowser: Browser;
  rightBrowser: Browser;
  leftContext: BrowserContext;
  rightContext: BrowserContext;
  leftPage: Page;
  rightPage: Page;
  close: () => Promise<void>;
}

export interface SplitScreenOptions {
  leftPermissions?: string[];
  leftGeolocation?: { latitude: number; longitude: number; accuracy?: number };
  rightPermissions?: string[];
  rightGeolocation?: { latitude: number; longitude: number; accuracy?: number };
}

/**
 * Khởi tạo 2 cửa sổ trình duyệt đặt cạnh nhau trên màn hình Windows:
 * - Cửa sổ TRÁI: x=0, y=20, size=760x860
 * - Cửa sổ PHẢI: x=765, y=20, size=760x860
 * Khi chạy với --project=ghost sẽ tự động bật giao diện thật và làm chậm thao tác để quan sát.
 */
export async function createSplitScreenSession(
  testInfo: TestInfo,
  options?: SplitScreenOptions,
): Promise<SplitScreenSession> {
  const baseUrl = testInfo.project.use.baseURL || "http://localhost:5173";
  const isGhost = testInfo.project.name === "ghost";
  const headless = isGhost ? false : (testInfo.project.use.headless ?? true);
  const slowMo = isGhost ? 450 : 0;

  const leftBrowser = await chromium.launch({
    headless,
    slowMo,
    args: ["--window-position=0,20", "--window-size=760,860"],
  });

  const rightBrowser = await chromium.launch({
    headless,
    slowMo,
    args: ["--window-position=765,20", "--window-size=760,860"],
  });

  const leftContext = await leftBrowser.newContext({
    baseURL: baseUrl,
    viewport: headless ? { width: 1280, height: 800 } : null,
    permissions: options?.leftPermissions,
    geolocation: options?.leftGeolocation,
  });

  const rightContext = await rightBrowser.newContext({
    baseURL: baseUrl,
    viewport: headless ? { width: 1280, height: 800 } : null,
    permissions: options?.rightPermissions,
    geolocation: options?.rightGeolocation,
  });

  const leftPage = await leftContext.newPage();
  const rightPage = await rightContext.newPage();

  return {
    baseUrl,
    leftBrowser,
    rightBrowser,
    leftContext,
    rightContext,
    leftPage,
    rightPage,
    async close() {
      await leftBrowser.close().catch(() => {});
      await rightBrowser.close().catch(() => {});
    },
  };
}
