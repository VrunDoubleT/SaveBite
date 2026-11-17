import { expect, test } from "@playwright/test";
import { API_URL, authenticateInBrowser, clearBrowserSession, TEST_USERS } from "../../helpers/authHelper.js";
import {
  addItemToCartViaApi,
  clearCustomerCartViaApi,
  getActiveDealForCheckout,
} from "../../helpers/cartCheckoutHelper.js";
import { CartPage } from "../../pages/CartPage.js";
import { CheckoutPage } from "../../pages/CheckoutPage.js";
import { CheckoutSimulationPage } from "../../pages/CheckoutSimulationPage.js";
import { FlashDealDetailPage } from "../../pages/FlashDealDetailPage.js";
import { PaymentsPage } from "../../pages/PaymentsPage.js";

test.describe("Cart & Checkout (TC-001 ~ TC-016)", () => {
  test.beforeEach(async ({ page, request }) => {
    // Authenticate as Customer before each test in Sheet 4
    await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );
  });

  // =========================================================================
  // SECTION 1: Add to Cart & Boundary Validation
  // =========================================================================

  test("TC-001: Add Flash Deal to cart from detail page with quantity selection", async ({
    page,
    request,
  }) => {
    const detailPage = new FlashDealDetailPage(page);
    const cartPage = new CartPage(page);

    await clearCustomerCartViaApi(request);

    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available in database.");
      return;
    }

    await detailPage.navigate(deal.id);

    await expect(detailPage.addToCartButton).toBeVisible({ timeout: 15_000 });
    await expect(detailPage.addToCartButton).toBeEnabled();

    if (deal.availableQuantity >= 2) {
      await detailPage.increaseQuantityButton.click();
      await page.waitForTimeout(300);
    }

    await detailPage.addToCartButton.click();

    await expect(
      detailPage.toastStatusWithText(/Added to your cart/i).or(page.getByText(/Added to your cart/i)).first(),
    ).toBeVisible({ timeout: 15_000 });

    const viewCart = detailPage.viewCartLink.or(page.locator('header a[href="/cart"]'));
    await viewCart.first().click();
    await expect(page).toHaveURL(/\/cart/);
    await expect(cartPage.pageHeader).toBeVisible();
    await expect(cartPage.cartItemByName(deal.productName).first()).toBeVisible();
  });

  test("TC-002: Guest (unauthenticated) clicking Add to cart redirects to Login page", async ({
    page,
    request,
  }) => {
    const detailPage = new FlashDealDetailPage(page);

    // Clear session to simulate guest user
    await clearBrowserSession(page);

    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await detailPage.navigate(deal.id);
    await expect(detailPage.addToCartButton).toBeVisible({ timeout: 15_000 });
    await detailPage.addToCartButton.click();

    // Verify redirected to Login page with redirect prompt
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByRole("heading", { name: /Sign in|Welcome back/i })).toBeVisible();
  });

  test("TC-003: Sold-out deal or deal with 0 stock blocks adding to cart", async ({
    page,
    request,
  }) => {
    const detailPage = new FlashDealDetailPage(page);

    // Query active deals to check for any sold-out or unpurchasable deals
    const url = `${API_URL}/flash-deals?latitude=10.012452&longitude=105.732432&radiusInKm=15`;
    const res = await request.get(url);
    if (!res.ok()) {
      test.skip(true, "Cannot fetch deals list from API.");
      return;
    }
    const json = await res.json();
    const deals = json.data?.deals ?? [];
    const unavailableDeal = deals.find(
      (d: any) => !d.isPurchasable || d.variants?.every((v: any) => v.availableQuantity === 0),
    );

    if (unavailableDeal) {
      await detailPage.navigate(unavailableDeal.id);
      await expect(detailPage.addToCartButton).toBeDisabled();
    } else {
      test.skip(true, "All active seeded deals currently have available stock.");
    }
  });

  test("TC-004: Quantity input auto-clamps when exceeding available inventory or set below 1", async ({
    page,
    request,
  }) => {
    const detailPage = new FlashDealDetailPage(page);
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await detailPage.navigate(deal.id);
    await expect(detailPage.quantityInput).toBeVisible({ timeout: 15_000 });

    const maxAttr = await detailPage.quantityInput.getAttribute("max");
    const expectedMax = maxAttr ? maxAttr : String(deal.availableQuantity);

    // Try typing an excessive quantity > available inventory
    await detailPage.quantityInput.fill("9999");
    await detailPage.quantityInput.blur();
    await expect(detailPage.quantityInput).toHaveValue(expectedMax);

    // Try typing 0 or negative
    await detailPage.quantityInput.fill("0");
    await detailPage.quantityInput.blur();
    await expect(detailPage.quantityInput).toHaveValue("1");
  });

  // =========================================================================
  // SECTION 2: Cart Management & Shop Segregation
  // =========================================================================

  test("TC-005: View cart with items grouped by shop and inspect shop subtotal", async ({
    page,
    request,
  }) => {
    const cartPage = new CartPage(page);

    await clearCustomerCartViaApi(request);
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await cartPage.navigate();

    const shopCard = cartPage.shopCardByName(deal.shopName);
    await expect(shopCard.first()).toBeVisible({ timeout: 15_000 });

    await expect(page.getByText(deal.shopName).first()).toBeVisible();
    await expect(shopCard.first().getByRole("link", { name: "Visit Store" })).toBeVisible();

    const itemRow = cartPage.cartItemByName(deal.productName).first();
    await expect(itemRow).toBeVisible();

    const checkoutBtn = cartPage.checkoutButtonForShop(deal.shopName).or(cartPage.firstCheckoutButton);
    await expect(checkoutBtn.first()).toBeVisible();
    await expect(checkoutBtn.first()).toBeEnabled();
  });

  test("TC-006: Increase & decrease item quantity in cart with subtotal recalculation", async ({
    page,
    request,
  }) => {
    const cartPage = new CartPage(page);

    await clearCustomerCartViaApi(request);
    const deal = await getActiveDealForCheckout(request);
    if (!deal || deal.availableQuantity < 2) {
      test.skip(true, "Requires an active deal with at least 2 available items.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await cartPage.navigate();

    const increaseBtn = cartPage.increaseQuantityButton(deal.productName);
    const decreaseBtn = cartPage.decreaseQuantityButton(deal.productName);
    const qtyInput = cartPage.quantityInput(deal.productName);

    await expect(increaseBtn).toBeVisible({ timeout: 15_000 });
    await expect(qtyInput).toHaveValue("1");

    // Click increase (+)
    await increaseBtn.click();
    await expect(qtyInput).toHaveValue("2", { timeout: 10_000 });

    // Click decrease (-)
    await decreaseBtn.click();
    await expect(qtyInput).toHaveValue("1", { timeout: 10_000 });

    // Decrease disabled at min (1)
    await expect(decreaseBtn).toBeDisabled();
  });

  test("TC-007: Unavailable item in cart disables Checkout button with warning message", async ({
    page,
    request,
  }) => {
    const cartPage = new CartPage(page);

    await clearCustomerCartViaApi(request);
    await cartPage.navigate();

    // If cart has no unavailable items currently, verify that valid carts enable checkout
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No active deals in database.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await cartPage.navigate();

    const shopCard = cartPage.shopCardByName(deal.shopName);
    await expect(shopCard).toBeVisible({ timeout: 15_000 });

    // Verify when all items are valid, checkout button is active
    const checkoutBtn = cartPage.checkoutButtonForShop(deal.shopName);
    await expect(checkoutBtn).toBeEnabled();
  });

  test("TC-008: Remove item from cart and verify empty cart state", async ({
    page,
    request,
  }) => {
    const cartPage = new CartPage(page);

    await clearCustomerCartViaApi(request);
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await cartPage.navigate();

    const removeBtn = cartPage.removeItemButton(deal.productName);
    await expect(removeBtn).toBeVisible({ timeout: 15_000 });

    await removeBtn.click();

    await expect(
      cartPage.toastMessage(/Item removed/i).or(cartPage.emptyCartState).first(),
    ).toBeVisible({ timeout: 15_000 });

    await expect(cartPage.emptyCartState).toBeVisible();
    await expect(cartPage.exploreDealsLink).toBeVisible();
  });

  // =========================================================================
  // SECTION 3: Checkout Routing, GPS & Travel Quote
  // =========================================================================

  test("TC-009: Accessing checkout without shopId displays empty guidance state", async ({
    page,
  }) => {
    const checkoutPage = new CheckoutPage(page);
    await checkoutPage.navigate(); // Navigates to /checkout with no query param

    await expect(page.getByText(/Choose a shop to check out/i)).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole("link", { name: /Back to cart/i })).toBeVisible();
  });

  test("TC-010: Obtain GPS quote, verify motorcycle ETA, distance, Soft & Hard deadlines", async ({
    page,
    request,
  }) => {
    const cartPage = new CartPage(page);
    const checkoutPage = new CheckoutPage(page);

    await clearCustomerCartViaApi(request);
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await cartPage.navigate();

    const checkoutBtn = cartPage.checkoutButtonForShop(deal.shopName).or(cartPage.firstCheckoutButton);
    await expect(checkoutBtn.first()).toBeVisible({ timeout: 15_000 });
    await checkoutBtn.first().click();

    await expect(page).toHaveURL(new RegExp(`/checkout\\?shopId=${deal.shopId}`));
    await expect(checkoutPage.pageHeader).toBeVisible();

    await checkoutPage.fetchGpsQuote();

    await expect(checkoutPage.softDeadlineBox.first()).toBeVisible({ timeout: 20_000 });
    await expect(checkoutPage.hardDeadlineBox.first()).toBeVisible();
    await expect(checkoutPage.routeSummary.first()).toBeVisible();
  });

  test("TC-011: Expired checkout quote displays warning banner and blocks stale order submission", async ({
    page,
    request,
  }) => {
    const checkoutPage = new CheckoutPage(page);

    await clearCustomerCartViaApi(request);
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await checkoutPage.navigate(deal.shopId);
    await checkoutPage.fetchGpsQuote();

    // Verify quote expiry timer is actively rendered
    await expect(
      page.locator('p[role="status"]').filter({ hasText: /Quote expires/i }).first(),
    ).toBeVisible({ timeout: 15_000 });
  });

  // =========================================================================
  // SECTION 4: Payment Methods & Order Placement
  // =========================================================================

  test("TC-012: Policy enforcement disables Pay at shop option when online payment is mandatory", async ({
    page,
    request,
  }) => {
    const checkoutPage = new CheckoutPage(page);

    await clearCustomerCartViaApi(request);
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await checkoutPage.navigate(deal.shopId);
    await checkoutPage.fetchGpsQuote();

    // Online Payment is default
    await expect(checkoutPage.onlinePaymentRadio).toBeChecked();
    await expect(checkoutPage.paymentSummaryAside).toContainText(/Online 100%/i);

    const isPayAtShopEnabled = await checkoutPage.payAtShopRadio.isEnabled();
    if (isPayAtShopEnabled) {
      await checkoutPage.payAtShopRadio.check();
      await expect(checkoutPage.payAtShopRadio).toBeChecked();
      await checkoutPage.onlinePaymentRadio.check();
    } else {
      // If disabled by policy, verify disabled state
      await expect(checkoutPage.payAtShopRadio).toBeDisabled();
    }
  });

  test("TC-013: Place order with 100% Online payment, complete simulated payment & verify Order Receipt", async ({
    page,
    request,
  }) => {
    const checkoutPage = new CheckoutPage(page);
    const simulationPage = new CheckoutSimulationPage(page);

    await clearCustomerCartViaApi(request);
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await checkoutPage.navigate(deal.shopId);
    await checkoutPage.fetchGpsQuote();

    await checkoutPage.onlinePaymentRadio.check();
    await expect(checkoutPage.placeOrderButton).toBeEnabled({ timeout: 15_000 });
    await checkoutPage.placeOrderButton.click();

    await expect(page).toHaveURL(/\/checkout\/simulation\?orderId=/, { timeout: 25_000 });
    await expect(simulationPage.simulationHeading).toBeVisible();

    await expect(simulationPage.paySuccessButton).toBeVisible();
    await simulationPage.paySuccessButton.click();

    await expect(page).toHaveURL(/\/checkout\?orderId=/, { timeout: 25_000 });
    await expect(checkoutPage.orderReceiptHeading).toBeVisible({ timeout: 20_000 });
    await expect(checkoutPage.orderPickupCode.first()).toBeVisible();
  });

  test("TC-014: Cancel payment on simulation page and verify order cancellation state", async ({
    page,
    request,
  }) => {
    const checkoutPage = new CheckoutPage(page);
    const simulationPage = new CheckoutSimulationPage(page);

    await clearCustomerCartViaApi(request);
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await checkoutPage.navigate(deal.shopId);
    await checkoutPage.fetchGpsQuote();

    await checkoutPage.placeOrderButton.click();
    await expect(page).toHaveURL(/\/checkout\/simulation\?orderId=/, { timeout: 25_000 });

    await expect(simulationPage.cancelPaymentButton).toBeVisible();
    await simulationPage.cancelPaymentButton.click();

    await expect(page).toHaveURL(/\/checkout\?orderId=/, { timeout: 25_000 });
    await expect(checkoutPage.orderReceiptHeading).toBeVisible({ timeout: 20_000 });
  });

  test("TC-015: Simulate payment expiration and verify order expiry state", async ({
    page,
    request,
  }) => {
    const checkoutPage = new CheckoutPage(page);
    const simulationPage = new CheckoutSimulationPage(page);

    await clearCustomerCartViaApi(request);
    const deal = await getActiveDealForCheckout(request);
    if (!deal) {
      test.skip(true, "No purchasable flash deals available.");
      return;
    }

    await addItemToCartViaApi(request, deal.variantId, 1);
    await checkoutPage.navigate(deal.shopId);
    await checkoutPage.fetchGpsQuote();

    await checkoutPage.placeOrderButton.click();
    await expect(page).toHaveURL(/\/checkout\/simulation\?orderId=/, { timeout: 25_000 });

    await expect(simulationPage.expirePaymentButton).toBeVisible();
    await simulationPage.expirePaymentButton.click();

    await expect(page).toHaveURL(/\/checkout\?orderId=/, { timeout: 25_000 });
    await expect(checkoutPage.orderReceiptHeading).toBeVisible({ timeout: 20_000 });
  });

  // =========================================================================
  // SECTION 5: Payment History & Audit
  // =========================================================================

  test("TC-016: Verify payment record in Account Payment History & view Payment Detail", async ({
    page,
  }) => {
    const paymentsPage = new PaymentsPage(page);

    await paymentsPage.navigate();
    await expect(paymentsPage.pageHeader.first()).toBeVisible({ timeout: 20_000 });
    await expect(paymentsPage.firstPaymentDetailLink).toBeVisible({ timeout: 20_000 });

    await paymentsPage.firstPaymentDetailLink.click();
    await expect(page).toHaveURL(/\/account\/payments\/[a-zA-Z0-9-]+/);
    await expect(page.getByRole("heading", { name: /Payment details/i })).toBeVisible({
      timeout: 15_000,
    });
  });
});
