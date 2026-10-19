import { test, expect } from "@playwright/test";
import { LoginPage } from "../../pages/LoginPage.js";

test.describe("Authentication - Login System Test", () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigate();
  });

  test("TC_AUTH_01: Verify all key login elements are present", async () => {
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.submitButton).toBeVisible();
    await expect(loginPage.createAccountLink).toBeVisible();
    await expect(loginPage.forgotPasswordLink).toBeVisible();
  });

  test("TC_AUTH_02: Should navigate to Register page when clicking Create an account", async ({ page }) => {
    await loginPage.createAccountLink.click();
    await expect(page).toHaveURL(/\/register/);
  });

  test("TC_AUTH_03: Should navigate to Forgot Password page when clicking Forgot password link", async ({ page }) => {
    await loginPage.forgotPasswordLink.click();
    await expect(page).toHaveURL(/\/forgot-password/);
  });

  test("TC_AUTH_04: Should show validation error when submitting empty fields", async ({ page }) => {
    await loginPage.submitButton.click();

    // Kiểm tra có thông báo lỗi validation từ Zod/React-Hook-Form
    const errorMessage = page.locator(".text-red-600, .text-rose-500, [aria-invalid='true']").first();
    await expect(errorMessage).toBeVisible();
  });

  test("TC_AUTH_05: User can fill in email and password to submit", async ({ page }) => {
    await loginPage.login("customer@savebite.vn", "Password123@");
    await expect(loginPage.emailInput).toHaveValue("customer@savebite.vn");
    await page.waitForTimeout(1500); // Dừng 1.5 giây để bạn kịp nhìn thấy màn hình
  });
});
