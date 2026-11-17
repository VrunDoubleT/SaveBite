import { test, expect } from "@playwright/test";
import { LoginPage } from "../../pages/LoginPage.js";
import { ProfilePage } from "../../pages/ProfilePage.js";
import {
  API_URL,
  REFRESH_TOKEN_KEY,
  TEST_USERS,
  TOKEN_KEY,
  changePasswordViaApi,
  clearBrowserSession,
  loginViaApi,
} from "../../helpers/authHelper.js";

test.describe("Authentication - Change Password & Sign Out (TC-013 ~ TC-015)", () => {
  test("TC-013: Change password fails when Current password is incorrect", async ({ page }) => {
    const loginPage = new LoginPage(page);
    const profilePage = new ProfilePage(page);

    await loginPage.navigate();
    await loginPage.login(TEST_USERS.customer.email, TEST_USERS.customer.password);
    await expect.poll(() => new URL(page.url()).pathname).toBe("/");

    await profilePage.navigate();
    await profilePage.openChangePassword();
    await profilePage.changePassword("WrongCurrent@123", "NewSaveBite@123", "NewSaveBite@123");

    await expect(
      profilePage.changePasswordAlertWithText("Current password is incorrect."),
    ).toBeVisible();
  });

  test("TC-014: Change password succeeds with valid credentials", async ({ page, request }) => {
    const account = TEST_USERS.changePasswordAccount;
    const tempPassword = "SaveBiteNew@123";
    const loginPage = new LoginPage(page);
    const profilePage = new ProfilePage(page);
    let passwordChanged = false;

    try {
      await loginPage.navigate();
      await loginPage.login(account.email, account.password);
      await expect.poll(() => new URL(page.url()).pathname).toBe("/");

      await profilePage.navigate();
      await profilePage.openChangePassword();
      await profilePage.changePassword(account.password, tempPassword, tempPassword);

      await expect(profilePage.toastStatusWithText("Password changed successfully.")).toBeVisible();
      passwordChanged = true;

      // Đăng xuất và kiểm tra đăng nhập lại bằng mật khẩu mới
      await profilePage.signOutButton.click();
      await expect(page).toHaveURL(/\/login/);

      await loginPage.login(account.email, tempPassword);
      await expect.poll(() => new URL(page.url()).pathname).toBe("/");
    } finally {
      if (passwordChanged) {
        // Luôn trả mật khẩu về mặc định để không làm hỏng dữ liệu seed cho Round sau
        const tokens = await loginViaApi(request, account.email, tempPassword);
        await changePasswordViaApi(request, tokens.accessToken, tempPassword, account.password);
      }
    }
  });

  test("TC-015: Sign out clears session and revokes access to protected pages", async ({
    page,
  }) => {
    const loginPage = new LoginPage(page);

    await loginPage.navigate();
    await loginPage.login(TEST_USERS.customer.email, TEST_USERS.customer.password);
    await expect.poll(() => new URL(page.url()).pathname).toBe("/");

    await page.getByRole("button", { name: "Sign out" }).click();
    await expect(page).toHaveURL(/\/login/);

    await expect
      .poll(() =>
        page.evaluate(
          ({ tokenKey, refreshKey }) =>
            localStorage.getItem(tokenKey) === null && localStorage.getItem(refreshKey) === null,
          { tokenKey: TOKEN_KEY, refreshKey: REFRESH_TOKEN_KEY },
        ),
      )
      .toBe(true);

    await page.goto("/account/profile", { waitUntil: "domcontentloaded" });
    await expect(page).toHaveURL(/\/login/);
  });
});

test.describe("Authentication - Role-Based Access Control (TC-016 ~ TC-018)", () => {
  test("TC-016: Unauthenticated user cannot access protected routes", async ({ page }) => {
    await clearBrowserSession(page);

    const protectedRoutes = ["/cart", "/account/orders", "/store-owner", "/admin"];
    for (const route of protectedRoutes) {
      await page.goto(route, { waitUntil: "domcontentloaded" });
      await expect(page).toHaveURL(/\/login/);
    }
  });

  test("TC-017: Customer and Store Owner cannot access Admin routes", async ({ page }) => {
    const loginPage = new LoginPage(page);

    // 1. Customer cố truy cập /admin -> bị đẩy về Home (/)
    await loginPage.navigate();
    await loginPage.login(TEST_USERS.customer.email, TEST_USERS.customer.password);
    await expect.poll(() => new URL(page.url()).pathname).toBe("/");
    await page.goto("/admin", { waitUntil: "domcontentloaded" });
    await expect.poll(() => new URL(page.url()).pathname).toBe("/");

    // Đăng xuất rồi thử với Store Owner
    await page.getByRole("button", { name: "Sign out" }).click();
    await expect(page).toHaveURL(/\/login/);

    // 2. Store Owner cố truy cập /admin -> bị đẩy về Home (/)
    await loginPage.login(TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);
    await expect.poll(() => new URL(page.url()).pathname).toBe("/");
    await page.goto("/admin", { waitUntil: "domcontentloaded" });
    await expect.poll(() => new URL(page.url()).pathname).toBe("/");
  });

  test("TC-018: Non-admin API call to Admin endpoint returns 403 Forbidden", async ({
    request,
  }) => {
    const tokens = await loginViaApi(
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );

    const response = await request.get(`${API_URL}/admin/audit-logs`, {
      headers: {
        Authorization: `Bearer ${tokens.accessToken}`,
      },
    });

    expect(response.status()).toBe(403);
  });
});
