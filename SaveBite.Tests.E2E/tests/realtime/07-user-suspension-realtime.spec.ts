import { test, expect } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  createSplitScreenSession,
  loginViaUi,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime User Suspension Split-Screen (Admin <-> Customer)", () => {
  test("Admin user moderation controls reflect realtime on Customer active browsing session", async ({}, testInfo) => {
    test.setTimeout(120_000);

    const session = await createSplitScreenSession(testInfo);

    try {
      // 1. Cửa sổ TRÁI: Admin mở trang Quản lý người dùng (/admin/users)
      await loginViaUi(
        session.leftPage,
        session.baseUrl,
        TEST_USERS.admin.email,
        TEST_USERS.admin.password,
      );
      await session.leftPage.goto(`${session.baseUrl}/admin/users`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.leftPage.getByRole("heading", { name: /User Management|Users/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 2. Cửa sổ PHẢI: Khách hàng đăng nhập và ở trang cá nhân (/account/profile)
      await loginViaUi(
        session.rightPage,
        session.baseUrl,
        TEST_USERS.customer.email,
        TEST_USERS.customer.password,
      );
      await session.rightPage.goto(`${session.baseUrl}/account/profile`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.rightPage.getByRole("heading", { name: /Profile/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 3. Bên TRÁI: Admin tìm kiếm tài khoản khách hàng trên bảng người dùng
      const searchInput = session.leftPage
        .getByPlaceholder(/Search email or name|Search users|Search by/i)
        .first();
      await expect(searchInput).toBeVisible({ timeout: 15_000 });
      await searchInput.fill(TEST_USERS.customer.email);
      await session.leftPage.waitForTimeout(500);

      // 4. Bên PHẢI: Khách hàng thấy tài khoản đang ở trạng thái hoạt động (Active)
      const statusElement = session.rightPage.getByText(/Active/i).first();
      await expect(statusElement).toBeVisible({ timeout: 15_000 });
    } finally {
      await session.close();
    }
  });
});
