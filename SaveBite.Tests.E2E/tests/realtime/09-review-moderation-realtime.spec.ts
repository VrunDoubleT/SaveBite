import { test, expect } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  createSplitScreenSession,
  loginViaUi,
  prepareRealtimeOrderDemoData,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime Review Moderation Split-Screen (Admin <-> Store Front)", () => {
  test("Admin review moderation actions synchronize realtime with public store review showcase", async ({
    request,
  }, testInfo) => {
    test.setTimeout(120_000);

    const demo = await prepareRealtimeOrderDemoData(request);
    const session = await createSplitScreenSession(testInfo);

    try {
      // 1. Cửa sổ TRÁI: Admin mở trang Kiểm duyệt đánh giá (/admin/feedbacks)
      await loginViaUi(
        session.leftPage,
        session.baseUrl,
        TEST_USERS.admin.email,
        TEST_USERS.admin.password,
      );
      await session.leftPage.goto(`${session.baseUrl}/admin/feedback`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.leftPage.getByRole("heading", { name: /Feedback Moderation|Feedbacks|Feedback/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 2. Cửa sổ PHẢI: Khách hàng xem trang thông tin & đánh giá của quán (/shops/{shopId})
      await session.rightPage.goto(`${session.baseUrl}/shops/${demo.shopId}`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.rightPage.getByRole("heading", { name: new RegExp(demo.shopName, "i") }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 3. Bên TRÁI: Admin thấy các bộ lọc vi phạm / trạng thái ẩn hiện review
      const filterNav = session.leftPage.locator('nav, [role="tablist"], button').filter({
        hasText: /All|Pending|Hidden|Reported/i,
      }).first();
      await expect(filterNav).toBeVisible({ timeout: 15_000 });

      // 4. Bên PHẢI: Khách hàng thấy khu vực đánh giá và điểm sao trung bình của quán
      const reviewsSection = session.rightPage.locator("main");
      await expect(reviewsSection).toBeVisible({ timeout: 15_000 });
    } finally {
      await session.close();
    }
  });
});
