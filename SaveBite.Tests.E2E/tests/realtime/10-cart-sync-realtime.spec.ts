import { test, expect } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  createSplitScreenSession,
  loginViaUi,
  prepareRealtimeOrderDemoData,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime Multi-Tab Cart Sync Split-Screen (Customer Deal Page <-> Cart Page)", () => {
  test("Customer adds item to cart in Window 1, Window 2 Cart page synchronizes realtime", async ({
    request,
  }, testInfo) => {
    test.setTimeout(120_000);

    const demo = await prepareRealtimeOrderDemoData(request);
    const session = await createSplitScreenSession(testInfo);

    try {
      // 1. Cửa sổ TRÁI: Customer đăng nhập và mở trang chi tiết Flash Deal
      await loginViaUi(
        session.leftPage,
        session.baseUrl,
        TEST_USERS.customer.email,
        TEST_USERS.customer.password,
      );
      await session.leftPage.goto(`${session.baseUrl}/flash-deals/${demo.dealId}`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.leftPage.getByRole("heading", { name: new RegExp(demo.productName, "i") }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 2. Cửa sổ PHẢI: Cùng tài khoản Customer đăng nhập và mở trang Giỏ hàng (/cart)
      await loginViaUi(
        session.rightPage,
        session.baseUrl,
        TEST_USERS.customer.email,
        TEST_USERS.customer.password,
      );
      await session.rightPage.goto(`${session.baseUrl}/cart`, {
        waitUntil: "domcontentloaded",
      });
      await expect(
        session.rightPage.getByRole("heading", { name: /Your cart|Cart/i }).first(),
      ).toBeVisible({ timeout: 20_000 });

      // 3. Bên TRÁI: Customer bấm "Add to cart"
      const addToCartBtn = session.leftPage.getByRole("button", { name: /Add to cart/i }).first();
      await expect(addToCartBtn).toBeEnabled({ timeout: 15_000 });
      await addToCartBtn.click();

      // Kiểm tra thông báo thêm giỏ hàng thành công ở cửa sổ TRÁI
      await expect(
        session.leftPage.getByText(/Added to your cart/i).first(),
      ).toBeVisible({ timeout: 15_000 });

      // 4. Bên PHẢI (KHÔNG CẦN F5): Giỏ hàng tự động cập nhật hiển thị món ăn vừa thêm qua SignalR
      const cartItem = session.rightPage.getByText(new RegExp(demo.productName, "i")).first();
      await expect(cartItem).toBeVisible({ timeout: 20_000 });
    } finally {
      await session.close();
    }
  });
});
