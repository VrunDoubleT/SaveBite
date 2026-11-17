import type { APIRequestContext, Page } from "@playwright/test";

export const API_URL = process.env.API_URL || "http://localhost:8080/api";
export const TOKEN_KEY = "savebite-access-token";
export const REFRESH_TOKEN_KEY = "savebite-refresh-token";

export interface TestAccount {
  email: string;
  password: string;
}

export interface TokenPair {
  accessToken: string;
  refreshToken: string;
}

export const TEST_USERS = {
  customer: {
    email: process.env.TEST_CUSTOMER_EMAIL || "customer@gmail.com",
    password: process.env.TEST_CUSTOMER_PASSWORD || "SaveBite@123",
  },
  storeOwner: {
    email: process.env.TEST_STORE_OWNER_EMAIL || "owner@gmail.com",
    password: process.env.TEST_STORE_OWNER_PASSWORD || "SaveBite@123",
  },
  admin: {
    email: process.env.TEST_ADMIN_EMAIL || "admin@gmail.com",
    password: process.env.TEST_ADMIN_PASSWORD || "SaveBite@123",
  },
  staff: {
    email: process.env.TEST_STAFF_EMAIL || "customer.locked@gmail.com",
    password: process.env.TEST_STAFF_PASSWORD || "SaveBite@123",
  },
  changePasswordAccount: {
    email: process.env.TEST_CHANGE_PASSWORD_EMAIL || "shop.locked@gmail.com",
    password: process.env.TEST_CHANGE_PASSWORD_PASSWORD || "SaveBite@123",
  },
} satisfies Record<string, TestAccount>;

/**
 * Đăng nhập trực tiếp qua Backend API để lấy JWT accessToken & refreshToken.
 * Dùng cho API testing (TC-018) hoặc dọn dẹp dữ liệu test (cleanup/reset).
 */
export async function loginViaApi(
  request: APIRequestContext,
  email: string,
  password: string,
): Promise<TokenPair> {
  const response = await request.post(`${API_URL}/auth/sessions`, {
    data: { email, password },
    timeout: 30_000,
  });

  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`API login failed (${response.status()}) for ${email}: ${body}`);
  }

  const json = (await response.json()) as { data?: TokenPair } & Partial<TokenPair>;
  const data = json.data ?? json;
  if (!data.accessToken || !data.refreshToken) {
    throw new Error(`Unexpected token response shape for ${email}`);
  }

  return {
    accessToken: data.accessToken,
    refreshToken: data.refreshToken,
  };
}

/**
 * Đổi mật khẩu qua Backend API (dùng trong finally để khôi phục mật khẩu gốc sau TC-014).
 */
export async function changePasswordViaApi(
  request: APIRequestContext,
  accessToken: string,
  currentPassword: string,
  newPassword: string,
): Promise<void> {
  const response = await request.put(`${API_URL}/auth/me/password`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    data: { currentPassword, newPassword },
  });

  if (!response.ok()) {
    const body = await response.text();
    throw new Error(`API change password failed (${response.status()}): ${body}`);
  }
}

/**
 * Xóa sạch token và cookie trong browser để bảo đảm trạng thái chưa đăng nhập.
 */
export async function clearBrowserSession(page: Page): Promise<void> {
  if (page.url() === "about:blank") {
    await page.goto("/login");
    await page.waitForLoadState("domcontentloaded");
  }
  await page.evaluate(
    ({ tokenKey, refreshKey }) => {
      localStorage.removeItem(tokenKey);
      localStorage.removeItem(refreshKey);
    },
    { tokenKey: TOKEN_KEY, refreshKey: REFRESH_TOKEN_KEY },
  );
  await page.context().clearCookies();
}

/**
 * Đăng nhập qua API và nạp token vào localStorage trên trình duyệt để vào thẳng trang cần test.
 */
export async function authenticateInBrowser(
  page: Page,
  request: APIRequestContext,
  email: string,
  password: string,
): Promise<TokenPair> {
  const tokens = await loginViaApi(request, email, password);

  if (page.url() === "about:blank") {
    await page.goto("/login", { waitUntil: "domcontentloaded" });
  }

  await page.evaluate(
    ({ accessToken, refreshToken, tokenKey, refreshKey }) => {
      localStorage.setItem(tokenKey, accessToken);
      localStorage.setItem(refreshKey, refreshToken);
    },
    {
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
      tokenKey: TOKEN_KEY,
      refreshKey: REFRESH_TOKEN_KEY,
    },
  );

  return tokens;
}

