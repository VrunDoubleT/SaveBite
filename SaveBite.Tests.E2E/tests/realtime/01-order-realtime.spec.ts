import { test, expect, chromium, type Browser } from "@playwright/test";
import { TEST_USERS } from "../../helpers/authHelper.js";
import {
  loginViaUi,
  prepareRealtimeOrderDemoData,
} from "../../helpers/orderRealtimeHelper.js";

test.describe("Realtime Order Split-Screen (Customer <-> Store Owner)", () => {
  test("Customer checkout + simulated payment appears realtime on Store Owner screen, and Owner confirm updates Customer receipt realtime", async ({
    request,
  }, testInfo) => {
    test.setTimeout(120_000);

    const baseUrl = testInfo.project.use.baseURL || "http://localhost:5173";
    const isGhost = testInfo.project.name === "ghost";
    const headless = isGhost ? false : (testInfo.project.use.headless ?? true);
    const slowMo = isGhost ? 450 : 0;

    // 1. Lấy sẵn shop & deal còn hạn của Store Owner, dọn giỏ hàng cũ của Customer
    const demo = await prepareRealtimeOrderDemoData(request);

    let ownerBrowser: Browser | undefined;
    let customerBrowser: Browser | undefined;

    try {
      // Cửa sổ PHẢI: Store Owner đứng chờ ở /store-owner/orders
      ownerBrowser = await chromium.launch({
        headless,
        slowMo,
        args: ["--window-position=765,20", "--window-size=760,860"],
      });

      // Cửa sổ TRÁI: Customer thao tác mua hàng & thanh toán
      customerBrowser = await chromium.launch({
        headless,
        slowMo,
        args: ["--window-position=0,20", "--window-size=760,860"],
      });

      const ownerContext = await ownerBrowser.newContext({
        baseURL: baseUrl,
        viewport: headless ? { width: 1280, height: 800 } : null,
        deviceScaleFactor: undefined,
      });
      const customerContext = await customerBrowser.newContext({
        baseURL: baseUrl,
        viewport: headless ? { width: 1280, height: 800 } : null,
        deviceScaleFactor: undefined,
        permissions: ["geolocation"],
        geolocation: {
          latitude: demo.latitude + 0.002,
          longitude: demo.longitude + 0.002,
          accuracy: 20,
        },
      });

      const ownerPage = await ownerContext.newPage();
      const customerPage = await customerContext.newPage();

      // =========================================================================
      // BƯỚC 1: Store Owner đăng nhập và mở sẵn trang Store Orders (cửa sổ PHẢI)
      // =========================================================================
      await loginViaUi(
        ownerPage,
        baseUrl,
        TEST_USERS.storeOwner.email,
        TEST_USERS.storeOwner.password,
      );
      await ownerPage.goto(`${baseUrl}/store-owner/orders`, {
        waitUntil: "domcontentloaded",
      });
      await expect(ownerPage.getByRole("heading", { name: "Store Orders" })).toBeVisible({
        timeout: 20_000,
      });
      // Chờ load xong danh sách ban đầu (SignalR đã kết nối ở RealtimeProvider)
      await expect(ownerPage.getByText("Loading orders…")).toBeHidden({ timeout: 20_000 });

      // =========================================================================
      // BƯỚC 2: Customer đăng nhập, thêm Flash Deal vào giỏ và đi Checkout (cửa sổ TRÁI)
      // =========================================================================
      await loginViaUi(
        customerPage,
        baseUrl,
        TEST_USERS.customer.email,
        TEST_USERS.customer.password,
      );

      await customerPage.goto(`${baseUrl}/flash-deals/${demo.dealId}`, {
        waitUntil: "domcontentloaded",
      });
      const addToCartBtn = customerPage.getByRole("button", { name: /Add to cart/i });
      await expect(addToCartBtn).toBeEnabled({ timeout: 20_000 });
      await addToCartBtn.click();

      await expect(customerPage.getByText(/Added to your cart/i).first()).toBeVisible({
        timeout: 15_000,
      });
      await customerPage.getByRole("link", { name: "View cart" }).first().click();
      await expect(customerPage).toHaveURL(/\/cart/);

      const checkoutWithShopBtn = customerPage.getByRole("button", {
        name: `Checkout with ${demo.shopName}`,
      });
      await expect(checkoutWithShopBtn).toBeEnabled({ timeout: 15_000 });
      await checkoutWithShopBtn.click();
      await expect(customerPage).toHaveURL(/\/checkout\?shopId=/);

      // Bảo đảm Backend đang chạy chế độ Simulated để không đẩy ra cổng PayOS bên ngoài
      await expect(
        customerPage.getByText("DEVELOPMENT · Simulated payment"),
        "Backend SaveBite.Api phải chạy ở Development với Checkout__PaymentProvider=Simulated để demo thanh toán giả lập.",
      ).toBeVisible({ timeout: 15_000 });

      // Lấy GPS giả lập gần shop & đặt hàng
      await customerPage.getByRole("button", { name: /Get my GPS location/i }).click();
      const placeOrderBtn = customerPage.getByRole("button", { name: /Place order/i });
      await expect(placeOrderBtn).toBeEnabled({ timeout: 25_000 });
      await placeOrderBtn.click();

      // Tự chuyển sang màn hình thanh toán giả lập
      await expect(customerPage).toHaveURL(/\/checkout\/simulation\?orderId=/, {
        timeout: 20_000,
      });
      await expect(
        customerPage.getByText("DEVELOPMENT · SIMULATED PAYMENT"),
      ).toBeVisible({ timeout: 15_000 });

      const orderCodeHeading = customerPage.locator("p.font-semibold").filter({
        hasText: /^Order\s+/i,
      });
      await expect(orderCodeHeading).toBeVisible({ timeout: 15_000 });
      const rawOrderText = (await orderCodeHeading.innerText()).trim();
      const orderCode = rawOrderText.replace(/^Order\s+/i, "").trim();
      expect(orderCode.length).toBeGreaterThan(0);

      // Bấm xác nhận thanh toán thành công
      await customerPage.getByRole("button", { name: "Payment successful" }).click();

      // Quay về trang hóa đơn của Customer với trạng thái đã trả tiền & chờ shop duyệt
      await expect(customerPage).toHaveURL(/\/checkout\?orderId=/, { timeout: 20_000 });
      await expect(
        customerPage.locator('section[aria-label="Order status"]').first(),
      ).toContainText("Pending", { timeout: 15_000 });
      await expect(
        customerPage.locator('section[aria-label="Payment status"]').first(),
      ).toContainText(/Paid in full|Deposit paid/i, { timeout: 15_000 });

      // =========================================================================
      // BƯỚC 3 (REALTIME #1): KHÔNG F5 bên Store Owner — đơn mới tự bật lên ở cửa sổ PHẢI!
      // =========================================================================
      const ownerOrderCard = ownerPage.locator("article").filter({ hasText: orderCode });
      await expect(ownerOrderCard).toBeVisible({ timeout: 25_000 });
      await expect(ownerOrderCard).toContainText("Pending");
      await expect(ownerOrderCard).toContainText(/Paid in full|Deposit paid/i);

      // =========================================================================
      // BƯỚC 4 (REALTIME #2): Store Owner bấm Confirm order -> Customer tự nhảy sang Confirmed!
      // =========================================================================
      await ownerOrderCard.getByRole("link", { name: "View details" }).click();
      await expect(ownerPage).toHaveURL(/\/store-owner\/orders\//, { timeout: 15_000 });

      const confirmOrderBtn = ownerPage.getByRole("button", { name: "Confirm order" });
      await expect(confirmOrderBtn).toBeEnabled({ timeout: 15_000 });
      await confirmOrderBtn.click();

      await expect(
        ownerPage.locator('section[aria-label="Order status"]').first(),
      ).toContainText("Confirmed", { timeout: 15_000 });

      // KHÔNG F5 bên Customer — hóa đơn ở cửa sổ TRÁI tự cập nhật sang Confirmed!
      await expect(
        customerPage.locator('section[aria-label="Order status"]').first(),
      ).toContainText("Confirmed", { timeout: 25_000 });
    } finally {
      await customerBrowser?.close().catch(() => {});
      await ownerBrowser?.close().catch(() => {});
    }
  });
});
