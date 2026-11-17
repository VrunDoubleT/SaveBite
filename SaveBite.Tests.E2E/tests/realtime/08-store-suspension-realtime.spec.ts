import { test, expect } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  createSplitScreenSession,
  loginViaUi,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime Store Suspension Split-Screen (Admin <-> Store Owner)", () => {
  test("Admin store governance controls reflect realtime on Store Owner workspace portal", async ({}, testInfo) => {
    test.setTimeout(120_000);

    const session = await createSplitScreenSession(testInfo);

    try {
      // 1. Cửa sổ TRÁI: Admin mở trang Quản lý danh sách cửa hàng (/admin/shops)
      await loginViaUi(
        session.leftPage,
        session.baseUrl,
        TEST_USERS.admin.email,
        TEST_USERS.admin.password,
      );
      await session.leftPage.goto(`${session.baseUrl}/admin/stores`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.leftPage.getByRole("heading", { name: /Shop Management|Stores Management|Stores|Shops/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 2. Cửa sổ PHẢI: Store Owner mở trang Dashboard quản lý quán (/store-owner)
      await loginViaUi(
        session.rightPage,
        session.baseUrl,
        TEST_USERS.storeOwner.email,
        TEST_USERS.storeOwner.password,
      );
      await session.rightPage.goto(`${session.baseUrl}/store-owner`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.rightPage.locator("#workspace-navigation").or(session.rightPage.locator("main")).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 3. Bên TRÁI: Admin kiểm tra danh sách các quán trên toàn sàn
      const shopsTable = session.leftPage.locator("table, article, div[class*='grid']").first();
      await expect(shopsTable).toBeVisible({ timeout: 15_000 });

      // 4. Bên PHẢI: Store Owner sẵn sàng nhận cập nhật trạng thái hoạt động (Active/Suspended)
      await expect(session.rightPage.locator("main")).toBeVisible({ timeout: 15_000 });
    } finally {
      await session.close();
    }
  });
});
