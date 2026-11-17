import { test, expect } from "@playwright/test";
import { LoginPage } from "../../pages/LoginPage.js";
import { RegisterPage } from "../../pages/RegisterPage.js";
import { ForgotPasswordPage } from "../../pages/ForgotPasswordPage.js";
import { OtpVerificationPage } from "../../pages/OtpVerificationPage.js";
import { TEST_USERS } from "../../helpers/authHelper.js";

test.describe("Authentication - Register & Verify OTP (TC-006 ~ TC-010)", () => {
  test("TC-006: Navigate to Register page from Login", async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.navigate();

    await loginPage.createAccountLink.click();

    await expect(page).toHaveURL(/\/register/);
    await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
  });

  test("TC-007: Register validation rejects weak password and mismatched confirm password", async ({
    page,
  }) => {
    const registerPage = new RegisterPage(page);
    await registerPage.navigate();

    await registerPage.register("Test Customer", "valid.customer@example.com", "weak", "other");

    await expect(page.getByText("Password must contain at least 8 characters.")).toBeVisible();
    await expect(page.getByText("Passwords do not match.")).toBeVisible();
    await expect(page).toHaveURL(/\/register/);
  });

  test("TC-008: Register fails when email already exists", async ({ page }) => {
    const registerPage = new RegisterPage(page);
    await registerPage.navigate();

    await registerPage.register(
      "Duplicate Account",
      TEST_USERS.customer.email,
      "SaveBite@123",
      "SaveBite@123",
    );

    await expect(
      registerPage.alertWithText("An account with this email already exists."),
    ).toBeVisible();
    await expect(page).toHaveURL(/\/register/);
  });

  test("TC-009: Request registration OTP and reject invalid 6-digit OTP", async ({ page }) => {
    const uniqueEmail = `e2e.reg.${Date.now()}.${Math.floor(Math.random() * 1000)}@example.com`;
    const registerPage = new RegisterPage(page);
    const otpPage = new OtpVerificationPage(page);

    await registerPage.navigate();
    await registerPage.register("E2E Temp User", uniqueEmail, "SaveBite@123", "SaveBite@123");

    await expect(page).toHaveURL(/\/register\/verify/);
    await expect(otpPage.infoBanner).toContainText("Registration OTP sent");

    await otpPage.submitOtp("000000");

    await expect(otpPage.alertWithText("OTP is invalid or has expired.")).toBeVisible();
  });

  test.skip("TC-010: Complete registration with real OTP from email and sign in", async () => {
    // Manual (Gmail OTP): Cần đọc mã OTP thật từ hộp thư email để kích hoạt tài khoản mới.
  });
});

test.describe("Authentication - Forgot Password & Reset (TC-011 ~ TC-012)", () => {
  test("TC-011: Request password reset OTP and reject invalid OTP", async ({ page }) => {
    // Dùng email ngẫu nhiên (hoặc cấu hình TEST_RESET_EMAIL) để tránh bị dính cooldown 60s / giới hạn 5 lần/ngày trên customer@gmail.com
    const resetEmail =
      process.env.TEST_RESET_EMAIL ||
      `e2e.reset.${Date.now()}.${Math.floor(Math.random() * 1000)}@example.com`;
    const loginPage = new LoginPage(page);
    const forgotPage = new ForgotPasswordPage(page);
    const otpPage = new OtpVerificationPage(page);

    await loginPage.navigate();
    await loginPage.forgotPasswordLink.click();
    await expect(page).toHaveURL(/\/forgot-password$/);

    await forgotPage.requestReset(resetEmail, "NewSaveBite@123", "NewSaveBite@123");

    await expect(page).toHaveURL(/\/forgot-password\/verify/);
    await expect(otpPage.infoBanner).toContainText(/password-reset OTP has been sent/i);

    await otpPage.submitOtp("000000");

    await expect(
      otpPage.alertWithText(/(OTP is invalid or has expired\.|Password-reset data or OTP has expired\.)/i),
    ).toBeVisible();
  });

  test.skip("TC-012: Complete password reset with real OTP and sign in with new password", async () => {
    // Manual (Gmail OTP): Cần đọc mã OTP thật từ hộp thư email để hoàn tất đặt lại mật khẩu.
  });
});
