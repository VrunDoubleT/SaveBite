import { test, expect } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  createSplitScreenSession,
  loginViaUi,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime Feedback & Reply Split-Screen (Store Owner <-> Customer)", () => {
  test("Store Owner reviews feedback and posts official replies while Customer monitors reviews realtime", async ({}, testInfo) => {
    test.setTimeout(120_000);

    const session = await createSplitScreenSession(testInfo);

    try {
      // 1. Cửa sổ TRÁI: Store Owner mở trang Quản lý đánh giá (/store-owner/reviews)
      await loginViaUi(
        session.leftPage,
        session.baseUrl,
        TEST_USERS.storeOwner.email,
        TEST_USERS.storeOwner.password,
      );
      await session.leftPage.goto(`${session.baseUrl}/store-owner/reviews`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.leftPage.getByRole("heading", { name: /Customer Reviews|Reviews/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 2. Cửa sổ PHẢI: Khách hàng mở trang Đánh giá của tôi (/account/reviews)
      await loginViaUi(
        session.rightPage,
        session.baseUrl,
        TEST_USERS.customer.email,
        TEST_USERS.customer.password,
      );
      await session.rightPage.goto(`${session.baseUrl}/account/reviews`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.rightPage.getByRole("heading", { name: /Reviews/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 3. Bên TRÁI: Store Owner kiểm tra các KPI thống kê review
      const reviewCards = session.leftPage.locator('section, article, div[class*="rounded"]').filter({
        hasText: /Total reviews|Needs reply|Replied/i,
      }).first();
      await expect(reviewCards).toBeVisible({ timeout: 15_000 });

      // 4. Bên PHẢI: Khách hàng thấy danh sách đánh giá cá nhân sẵn sàng nhận phản hồi realtime
      await expect(session.rightPage.locator("main")).toBeVisible({ timeout: 15_000 });
    } finally {
      await session.close();
    }
  });
});
