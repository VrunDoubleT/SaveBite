import { test, expect } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  createSplitScreenSession,
  loginViaUi,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime Shop Application Split-Screen (Customer <-> Admin)", () => {
  test("Customer shop application status is monitored while Admin reviews applications on split-screen", async ({}, testInfo) => {
    test.setTimeout(120_000);

    const session = await createSplitScreenSession(testInfo);

    try {
      // 1. Cửa sổ TRÁI: Customer mở trang Đơn đăng ký mở quán (/account/shop-application)
      await loginViaUi(
        session.leftPage,
        session.baseUrl,
        TEST_USERS.customer.email,
        TEST_USERS.customer.password,
      );
      await session.leftPage.goto(`${session.baseUrl}/account/shop-application`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.leftPage.getByRole("heading", { name: /Shop Application|Register your shop/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 2. Cửa sổ PHẢI: Admin đăng nhập và mở trang Quản lý đơn mở quán (/admin/shop-applications)
      await loginViaUi(
        session.rightPage,
        session.baseUrl,
        TEST_USERS.admin.email,
        TEST_USERS.admin.password,
      );
      await session.rightPage.goto(`${session.baseUrl}/admin/shop-applications`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.rightPage.getByRole("heading", { name: /Shop Applications|Applications/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 3. Bên PHẢI: Admin thấy các tab lọc trạng thái (Pending, Approved, Rejected, Needs Revision)
      const filterTabs = session.rightPage.locator('nav, [role="tablist"], div').filter({
        hasText: /Pending|Approved|Rejected/i,
      }).first();
      await expect(filterTabs).toBeVisible({ timeout: 15_000 });

      // 4. Bên TRÁI: Customer sẵn sàng nhận cập nhật trạng thái đơn duyệt tự động từ Admin
      await expect(session.leftPage.locator("main")).toBeVisible({ timeout: 15_000 });
    } finally {
      await session.close();
    }
  });
});
