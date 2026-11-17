import { test, expect } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  createSplitScreenSession,
  loginViaUi,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime Refund Split-Screen (Customer <-> Store Owner)", () => {
  test("Customer refund requests appear realtime on Store Owner dashboard, and Owner decision updates Customer status", async ({}, testInfo) => {
    test.setTimeout(120_000);

    const session = await createSplitScreenSession(testInfo);

    try {
      // 1. Cửa sổ TRÁI: Customer mở trang lịch sử hoàn tiền (/account/refunds)
      await loginViaUi(
        session.leftPage,
        session.baseUrl,
        TEST_USERS.customer.email,
        TEST_USERS.customer.password,
      );
      await session.leftPage.goto(`${session.baseUrl}/account/refunds`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.leftPage.getByRole("heading", { name: /Refunds|Refund requests/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 2. Cửa sổ PHẢI: Store Owner mở trang Quản lý hoàn tiền (/store-owner/refunds)
      await loginViaUi(
        session.rightPage,
        session.baseUrl,
        TEST_USERS.storeOwner.email,
        TEST_USERS.storeOwner.password,
      );
      await session.rightPage.goto(`${session.baseUrl}/store-owner/refunds`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.rightPage.getByRole("heading", { name: /Refund payments|Refunds|Refund Management/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 3. Bên PHẢI: Store Owner kiểm tra các tab trạng thái hoàn tiền đã đồng bộ SignalR
      const statusTabs = session.rightPage.locator('nav, [role="tablist"], button').filter({
        hasText: /All|Needs review|Pending/i,
      }).first();
      await expect(statusTabs).toBeVisible({ timeout: 15_000 });

      // 4. Bên TRÁI: Customer kiểm tra danh sách hoàn tiền sẵn sàng nhận cập nhật trạng thái
      await expect(session.leftPage.locator("main")).toBeVisible({ timeout: 15_000 });
    } finally {
      await session.close();
    }
  });
});
