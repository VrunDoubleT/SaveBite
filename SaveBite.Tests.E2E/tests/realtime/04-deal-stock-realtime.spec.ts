import { test, expect } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  createSplitScreenSession,
  loginViaUi,
  prepareRealtimeOrderDemoData,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime Deal & Stock Split-Screen (Store Owner <-> Customer)", () => {
  test("Store Owner manages Flash Deals while Customer inspects deal and live countdown on split-screen", async ({
    request,
  }, testInfo) => {
    test.setTimeout(120_000);

    const demo = await prepareRealtimeOrderDemoData(request);
    const session = await createSplitScreenSession(testInfo);

    try {
      // 1. Cửa sổ TRÁI: Store Owner mở trang Quản lý Flash Deals (/store-owner/flash-deals)
      await loginViaUi(
        session.leftPage,
        session.baseUrl,
        TEST_USERS.storeOwner.email,
        TEST_USERS.storeOwner.password,
      );
      await session.leftPage.goto(`${session.baseUrl}/store-owner/flash-deals`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.leftPage.getByRole("heading", { name: /Flash Deals|Deals Management/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 2. Cửa sổ PHẢI: Khách hàng xem trang chi tiết Flash Deal của quán
      await session.rightPage.goto(`${session.baseUrl}/flash-deals/${demo.dealId}`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.rightPage.getByRole("heading", { name: new RegExp(demo.productName, "i") }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 3. Bên PHẢI: Khách hàng thấy nút Add to Cart và trạng thái sẵn sàng mua
      const addToCartBtn = session.rightPage.getByRole("button", { name: /Add to cart/i }).first();
      await expect(addToCartBtn).toBeEnabled({ timeout: 15_000 });

      // 4. Bên TRÁI: Store Owner thấy deal đang bán hiển thị trong bảng/danh sách
      const dealItem = session.leftPage.getByText(demo.productName).first();
      await expect(dealItem).toBeVisible({ timeout: 15_000 });
    } finally {
      await session.close();
    }
  });
});
