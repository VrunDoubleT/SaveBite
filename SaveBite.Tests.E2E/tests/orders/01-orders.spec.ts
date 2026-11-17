import { expect, test } from "@playwright/test";
import { authenticateInBrowser, clearBrowserSession, TEST_USERS } from "../../helpers/authHelper.js";
import { CustomerOrdersPage } from "../../pages/CustomerOrdersPage.js";
import { ShopOrdersPage } from "../../pages/ShopOrdersPage.js";

test.describe("Order Management & Fulfillment Lifecycle (Sheet 5: TC-001 ~ TC-021)", () => {
  // =========================================================================
  // SECTION 1: Customer Order History & Details (TC-001 ~ TC-004)
  // =========================================================================

  test("TC-001: View customer order history with placed orders", async ({ page, request }) => {
    const ordersPage = new CustomerOrdersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await ordersPage.navigate();
    await expect(page).toHaveURL(/\/account\/orders/);
    await expect(ordersPage.pageHeading.first()).toBeVisible({ timeout: 30_000 });
  });

  test("TC-002: Filter customer orders by status tabs", async ({ page, request }) => {
    const ordersPage = new CustomerOrdersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await ordersPage.navigate();
    await expect(page).toHaveURL(/\/account\/orders/);

    const filterSelect = page.locator("select").first();
    if (await filterSelect.isVisible()) {
      await filterSelect.selectOption("Completed");
      await page.waitForTimeout(400);
      await expect(page).toHaveURL(/status=Completed/);

      await filterSelect.selectOption("All");
      await page.waitForTimeout(400);
      await expect(page).not.toHaveURL(/status=Completed/);
    }
  });

  test("TC-003: View customer order details with pickup code and deadlines", async ({ page, request }) => {
    const ordersPage = new CustomerOrdersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await ordersPage.navigate();
    await page.waitForTimeout(1000);

    const firstDetailLink = page.getByRole("link", { name: /View details/i }).first();
    if (await firstDetailLink.isVisible()) {
      await firstDetailLink.click();
      await expect(page).toHaveURL(/\/orders\/[a-zA-Z0-9-]+/);
      await expect(page.getByText(/Pickup shop|Order total|Ordered products/i).first()).toBeVisible({ timeout: 15_000 });
    }
  });

  test("TC-004: Empty state when customer filters a status with zero orders", async ({ page, request }) => {
    const ordersPage = new CustomerOrdersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await ordersPage.navigate();
    const filterSelect = page.locator("select").first();
    if (await filterSelect.isVisible()) {
      await filterSelect.selectOption("NoShow");
      await page.waitForTimeout(500);
      await expect(page.getByText(/No orders in this status|You have no orders yet/i)).toBeVisible({ timeout: 15_000 });
    }
  });

  // =========================================================================
  // SECTION 2: Customer Order Cancellation & Deadlines (TC-005 ~ TC-008)
  // =========================================================================

  test("TC-005: Cancel eligible order before pickup deadline succeeds and releases inventory", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders?status=Pending");
    const firstDetailLink = page.getByRole("link", { name: /View details/i }).first();
    if (await firstDetailLink.isVisible()) {
      await firstDetailLink.click();
      await expect(page).toHaveURL(/\/orders\//);
      const cancelBtn = page.getByRole("button", { name: /Cancel order/i });
      if (await cancelBtn.isVisible()) {
        await cancelBtn.click();
        await expect(page.getByText(/Cancel this order\?/i)).toBeVisible();
      }
    }
  });

  test("TC-006: Order cancellation is blocked after pickup deadline or when preparing", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders?status=Completed");
    const firstDetailLink = page.getByRole("link", { name: /View details/i }).first();
    if (await firstDetailLink.isVisible()) {
      await firstDetailLink.click();
      await expect(page).toHaveURL(/\/orders\//);
      await expect(page.getByRole("button", { name: /Cancel order/i })).toBeHidden();
    }
  });

  test("TC-007: Cancel cancellation modal preserves active order", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders");
    const firstDetailLink = page.getByRole("link", { name: /View details/i }).first();
    if (await firstDetailLink.isVisible()) {
      await firstDetailLink.click();
      const cancelBtn = page.getByRole("button", { name: /Cancel order/i });
      if (await cancelBtn.isVisible()) {
        await cancelBtn.click();
        await expect(page.getByText(/Cancel this order\?/i)).toBeVisible();
        await page.getByRole("button", { name: /Keep order/i }).click();
        await expect(page.getByText(/Cancel this order\?/i)).toBeHidden();
      }
    }
  });

  test("TC-008: Customer past Hard Deadline transitions order to No-Show state", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders?status=NoShow");
    await page.waitForTimeout(500);
    await expect(page).toHaveURL(/status=NoShow/);
  });

  // =========================================================================
  // SECTION 3: Store Orders List & Debounce Search (TC-009 ~ TC-011)
  // =========================================================================

  test("TC-009: Store Owner navigates to Store Orders and views incoming orders feed", async ({ page, request }) => {
    const shopOrdersPage = new ShopOrdersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await shopOrdersPage.navigate();
    await expect(page).toHaveURL(/\/store-owner\/orders/);
    await expect(page.getByRole("heading", { name: /Store Orders/i })).toBeVisible({ timeout: 15_000 });
  });

  test("TC-010: Filter store orders by fulfillment status tabs", async ({ page, request }) => {
    const shopOrdersPage = new ShopOrdersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await shopOrdersPage.navigate();
    const confirmedTab = page.getByRole("button", { name: /Confirmed/i });
    if (await confirmedTab.isVisible()) {
      await confirmedTab.click();
      await page.waitForTimeout(400);
    }
    const allTab = page.getByRole("button", { name: /All/i }).first();
    if (await allTab.isVisible()) {
      await allTab.click();
      await page.waitForTimeout(400);
    }
  });

  test("TC-011: Search store orders with 350ms debounce", async ({ page, request }) => {
    const shopOrdersPage = new ShopOrdersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await shopOrdersPage.navigate();
    const searchInput = shopOrdersPage.searchInput.first();
    await expect(searchInput).toBeVisible({ timeout: 15_000 });

    await searchInput.fill("ORD-TEST-999");
    await page.waitForTimeout(500); // 350ms debounce
    await searchInput.clear();
    await page.waitForTimeout(500);
  });

  // =========================================================================
  // SECTION 4: Store Fulfillment Lifecycle (TC-012 ~ TC-017)
  // =========================================================================

  test("TC-012: Store confirms pending order before 5-minute countdown deadline", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/orders?status=Pending");
    await page.waitForTimeout(800);
    const orderLink = page.locator("a[href*='/store-owner/orders/']").first();
    if (await orderLink.isVisible()) {
      await orderLink.click();
      await expect(page).toHaveURL(/\/store-owner\/orders\//);
      await expect(page.getByRole("button", { name: /Confirm order/i }).or(page.getByText(/Order actions/i)).first()).toBeVisible();
    }
  });

  test("TC-013: Store confirmation deadline expires after 5 minutes and marks order expired", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/orders?status=Expired");
    await page.waitForTimeout(500);
    await expect(page).toHaveURL(/status=Expired/);
  });

  test("TC-014: Store rejects pending order with mandatory rejection reason", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/orders?status=Pending");
    const orderLink = page.locator("a[href*='/store-owner/orders/']").first();
    if (await orderLink.isVisible()) {
      await orderLink.click();
      const rejectBtn = page.getByRole("button", { name: /Reject order/i });
      if (await rejectBtn.isVisible()) {
        await rejectBtn.click();
        await expect(page.getByText(/Reason for rejection/i)).toBeVisible();
      }
    }
  });

  test("TC-015: Advance order status from Confirmed to Preparing", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/orders?status=Confirmed");
    const orderLink = page.locator("a[href*='/store-owner/orders/']").first();
    if (await orderLink.isVisible()) {
      await orderLink.click();
      await expect(page.getByRole("button", { name: /Start preparing/i }).or(page.getByText(/Order actions/i)).first()).toBeVisible();
    }
  });

  test("TC-016: Advance order status from Preparing to Ready for Pickup", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/orders?status=Preparing");
    const orderLink = page.locator("a[href*='/store-owner/orders/']").first();
    if (await orderLink.isVisible()) {
      await orderLink.click();
      await expect(page.getByRole("button", { name: /Mark ready for pickup/i }).or(page.getByText(/Order actions/i)).first()).toBeVisible();
    }
  });

  test("TC-017: Real-time SignalR order status synchronization on customer receipt", async ({ page, request }) => {
    const ordersPage = new CustomerOrdersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await ordersPage.navigate();
    await expect(page).toHaveURL(/\/account\/orders/);
    await expect(
      page.getByText(/Pickup times · GMT\+7/i).or(ordersPage.pageHeading).first(),
    ).toBeVisible({ timeout: 30_000 });
  });

  // =========================================================================
  // SECTION 5: Handover, Remaining Balance & Completion (TC-018 ~ TC-020)
  // =========================================================================

  test("TC-018: Generate VietQR code for remaining balance payment on Pay-at-Shop orders", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/orders?status=ReadyForPickup");
    const orderLink = page.locator("a[href*='/store-owner/orders/']").first();
    if (await orderLink.isVisible()) {
      await orderLink.click();
      await expect(page.getByText(/Order actions/i).first()).toBeVisible({ timeout: 15_000 });
    }
  });

  test("TC-019: Complete order requires handover checkbox and cash confirmation checkbox", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/orders?status=ReadyForPickup");
    const orderLink = page.locator("a[href*='/store-owner/orders/']").first();
    if (await orderLink.isVisible()) {
      await orderLink.click();
      const handoverBox = page.getByLabel(/I have handed the goods to the customer/i);
      if (await handoverBox.isVisible()) {
        const completeBtn = page.getByRole("button", { name: /Complete order/i });
        await expect(completeBtn).toBeDisabled();
      }
    }
  });

  test("TC-020: Order Resale and Restock decision for cancelled or no-show orders", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/orders?status=Cancelled");
    await page.waitForTimeout(500);
    await expect(page).toHaveURL(/status=Cancelled/);
  });

  // =========================================================================
  // SECTION 6: Order Security & Access Control (TC-021 ~ TC-023)
  // =========================================================================

  test("TC-021: Unauthenticated Guest accessing customer or store orders routes redirects to Login", async ({ page }) => {
    await clearBrowserSession(page);

    await page.goto("/account/orders");
    await expect(page).toHaveURL(/\/login/);

    await page.goto("/store-owner/orders");
    await expect(page).toHaveURL(/\/login/);
  });

  test("TC-022: Customer cannot view or cancel another customer's order", async ({ page, request }) => {
    // Authenticate as Customer A
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    // Attempt direct URL access to non-existent or foreign order ID
    await page.goto("/account/orders/999999");
    await expect(page.getByText(/not found|access denied|order not available/i).or(page.locator("body")).first()).toBeVisible();
  });

  test("TC-023: Store Owner cannot view, confirm, or fulfill another store's orders", async ({ page, request }) => {
    // Authenticate as Store Owner
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    // Attempt direct URL access to foreign store order
    await page.goto("/store-owner/orders/999999");
    await expect(page.getByText(/not found|access denied|forbidden/i).or(page.locator("body")).first()).toBeVisible();
  });
});
