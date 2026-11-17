import { test, expect } from "@playwright/test";
import { LoginPage } from "../../pages/LoginPage.js";
import {
  TEST_USERS,
  TOKEN_KEY,
  REFRESH_TOKEN_KEY,
  loginViaApi,
} from "../../helpers/authHelper.js";

test.describe("Authentication - Login (TC-001 ~ TC-005)", () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigate();
  });

  test("TC-001: Login successfully as Customer", async ({ page }) => {
    await loginPage.login(TEST_USERS.customer.email, TEST_USERS.customer.password);

    // Role 'user' được chuyển hướng về Home (/) và lưu token trong localStorage
    await expect.poll(() => new URL(page.url()).pathname).toBe("/");
    await expect
      .poll(() =>
        page.evaluate(
          ({ tokenKey, refreshKey }) =>
            Boolean(localStorage.getItem(tokenKey) && localStorage.getItem(refreshKey)),
          { tokenKey: TOKEN_KEY, refreshKey: REFRESH_TOKEN_KEY },
        ),
      )
      .toBe(true);
    await expect(page.getByRole("button", { name: "Sign out" })).toBeVisible();
  });

  test("TC-002: Login successfully as Store Owner", async ({ page }) => {
    await loginPage.login(TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    // Store Owner sau login về Home (/), sau đó mở được workspace /store-owner
    await expect.poll(() => new URL(page.url()).pathname).toBe("/");
    await page.goto("/store-owner", { waitUntil: "domcontentloaded" });
    await expect(page).toHaveURL(/\/store-owner/);
    await expect(page.locator("#workspace-navigation")).toBeVisible();
  });

  test("TC-003: Login successfully as Admin", async ({ page }) => {
    await loginPage.login(TEST_USERS.admin.email, TEST_USERS.admin.password);

    // Role 'admin' tự chuyển vào /admin (/admin/analytics/overview)
    await expect(page).toHaveURL(/\/admin/);
    await expect(page.locator("#workspace-navigation")).toBeVisible();
  });

  test("TC-004: Login fails with empty Email and Password", async ({ page }) => {
    await loginPage.submitButton.click();

    await expect(page.getByText("Email is required.")).toBeVisible();
    await expect(page.getByText("Password is required.")).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
  });

  test("TC-005: Login fails with incorrect password", async ({ page, request }) => {
    try {
      await loginPage.login(TEST_USERS.customer.email, "WrongPass@123");

      await expect(loginPage.alertWithText("Email or password is incorrect.")).toBeVisible();
      await expect(page).toHaveURL(/\/login/);
    } finally {
      // Đăng nhập thành công 1 lần qua API để reset bộ đếm lỗi trên Redis (tránh khóa sau 5 lần chạy)
      await loginViaApi(request, TEST_USERS.customer.email, TEST_USERS.customer.password).catch(
        () => {},
      );
    }
  });
});
