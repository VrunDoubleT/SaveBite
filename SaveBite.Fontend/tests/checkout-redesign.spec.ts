import { test, expect, type Page } from "@playwright/test";
const shopId = "00000000-0000-4000-8000-000000000001";
const checkout = `/checkout?shopId=${shopId}&role=user`;
test.beforeEach(async ({ page }) => {
  await page.route("https://unpkg.com/**", (route) => route.abort());
  await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
});
async function ready(page: Page) {
  await page.waitForFunction(() =>
    Boolean((window as unknown as { __savebiteMock?: unknown }).__savebiteMock),
  );
}
async function fits(page: Page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
}
// Patch API results only in this test page; the default MSW fixture remains untouched.
async function fixture(
  page: Page,
  scenario: "deposit" | "restricted" | "unavailable" | "expired" | "long" | "conflict",
) {
  await page.goto("/cart?role=user");
  await ready(page);
  await expect(page.getByRole("region", { name: /^Cart from / })).toHaveCount(3);
  await page.evaluate(async (scenario) => {
    const { checkoutApi } = await import("/src/features/checkout/api/checkoutApi.ts");
    const context = checkoutApi.context;
    const quote = checkoutApi.quote;
    const apply = (data: Awaited<ReturnType<typeof context>>) => {
      if (scenario === "deposit") {
        data.policy.requiredDeposit = 20000;
        data.policy.appliedRange = data.policy.ranges[0];
      }
      if (scenario === "restricted") {
        data.policy.payAtShopAllowed = false;
        data.policy.restrictionCode = "RecoveryRequired";
      }
      if (scenario === "unavailable") data.onlinePaymentAvailable = false;
      if (scenario === "long") {
        data.shop.shopName = "Cửa hàng thực phẩm ".repeat(14);
        data.shop.shopAddress = "Địa chỉ nhận hàng ".repeat(20);
        data.shop.shopLogoUrl = "/missing-logo.jpg";
        data.shop.items[0].productName = "Bữa ăn gia đình ".repeat(10);
        data.shop.items[0].variantName = "Khẩu phần lớn ".repeat(8);
        data.shop.items[0].imageUrl = "/missing-food.jpg";
        data.shop.subtotal = 25000000;
        data.policy.levelName = "Thành viên SaveBite ".repeat(8);
      }
      return data;
    };
    checkoutApi.context = async (...args) => apply(await context(...args));
    checkoutApi.quote = async (...args) => {
      const next = await quote(...args);
      apply(next);
      if (scenario === "expired") {
        next.expiresAt = new Date(Date.now() + 2500).toISOString();
        next.pickup.bufferWasCapped = true;
      }
      return next;
    };
    if (scenario === "conflict")
      checkoutApi.place = async () => {
        throw {
          isAxiosError: true,
          response: { status: 409, data: { message: "Stock changed. Please refresh your quote." } },
        };
      };
  }, scenario);
  await page.getByRole("button", { name: "Checkout with Bếp Xanh Sài Gòn", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Payment summary", exact: true })).toBeVisible();
}
for (const width of [360, 768, 1280]) {
  test(`complete checkout and pay-at-shop receipt work at ${width}px`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));
    await page.setViewportSize({ width, height: 1000 });
    await page.goto(checkout);
    await expect(page.getByRole("heading", { name: "Payment summary", exact: true })).toBeVisible({
      timeout: 30000,
    });
    await expect(page.getByRole("spinbutton")).toHaveCount(0);
    await expect(
      page.getByRole("heading", { name: "Your payment policy", exact: true }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: /^Place order/ })).toBeDisabled();
    await expect(page.getByText("Map unavailable", { exact: true })).toBeVisible();
    await fits(page);
    await page.screenshot({ path: `/tmp/savebite-checkout-${width}.png`, fullPage: true });
    await page.locator("summary").focus();
    await page.keyboard.press("Enter");
    await expect(page.getByText(/Soft deadline = order time/)).toBeVisible();
    const gpsRequest = page.waitForRequest(
      (request) => request.method() === "POST" && request.url().includes("/checkout/quotes"),
    );
    await page.getByRole("button", { name: "Get my GPS location", exact: true }).click();
    const gps = (await gpsRequest).postDataJSON();
    expect(gps.shopId).toBe(shopId);
    expect(gps.location.accuracyMeters).toBe(5);
    await expect(page.getByText("GPS accuracy: ±5 m", { exact: true })).toBeVisible();
    await expect(
      page.getByText("Soft deadline · recommended arrival", { exact: true }),
    ).toBeVisible();
    await page.getByRole("radio", { name: /^Pay at shop/ }).check();
    const summary = page.getByRole("complementary", { name: "Payment summary", exact: true });
    await expect(summary.getByText("0đ", { exact: true })).toBeVisible();
    await expect(summary.getByText("53.000đ", { exact: true })).toHaveCount(2);
    await fits(page);
    await page.screenshot({ path: `/tmp/savebite-checkout-quote-${width}.png`, fullPage: true });
    const orderRequest = page.waitForRequest(
      (request) => request.method() === "POST" && request.url().includes("/checkout/orders"),
    );
    await page
      .getByRole("button", { name: "Place order · No online payment required", exact: true })
      .click();
    const placed = (await orderRequest).postDataJSON();
    expect(placed.paymentMethod).toBe("PayAtShop");
    expect(placed.requestId).toMatch(/^[0-9a-f-]{36}$/i);
    await expect(page).toHaveURL(/\/checkout\?orderId=/);
    await expect(page.getByRole("region", { name: "Order overview", exact: true })).toBeVisible();
    await expect(
      page.getByRole("heading", { name: "Pickup information", exact: true }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Cancel order", exact: true })).toBeEnabled();
    await fits(page);
    await page.screenshot({ path: `/tmp/savebite-checkout-receipt-${width}.png`, fullPage: true });
    expect(errors).toEqual([]);
  });
}
test("fixed deposit and online-only policies drive totals and radio guards", async ({ page }) => {
  await fixture(page, "deposit");
  await page.getByRole("radio", { name: /^Pay at shop/ }).check();
  const summary = page.getByRole("complementary", { name: "Payment summary", exact: true });
  await expect(summary.getByText("20.000đ", { exact: true })).toBeVisible();
  await expect(summary.getByText("33.000đ", { exact: true })).toBeVisible();
  await expect(page.getByText("Your applicable deposit range", { exact: true })).toBeVisible();
  await fixture(page, "restricted");
  await expect(page.getByRole("radio", { name: /^Pay at shop/ })).toBeDisabled();
  await expect(page.getByRole("radio", { name: /^Pay 100% online/ })).toBeChecked();
  await expect(page.getByText(/Complete the required online orders/)).toBeVisible();
});
test("online payment unavailability still allows eligible zero-deposit shop payment", async ({
  page,
}) => {
  await fixture(page, "unavailable");
  await page.getByRole("button", { name: "Get my GPS location", exact: true }).click();
  await expect(page.getByText("GPS accuracy: ±5 m")).toBeVisible();
  await expect(page.getByRole("button", { name: /^Place order/ })).toBeDisabled();
  await expect(page.getByText(/This shop cannot accept online payments yet/)).toBeVisible();
  await page.getByRole("radio", { name: /^Pay at shop/ }).check();
  await expect(page.getByRole("button", { name: /^Place order/ })).toBeEnabled();
});
test("GPS denial and inaccurate location cannot enable placement; refresh recovers", async ({
  page,
}) => {
  await page.goto(checkout);
  await expect(page.getByRole("heading", { name: "Payment summary", exact: true })).toBeVisible();
  await page.evaluate(() => {
    const original = navigator.geolocation.getCurrentPosition.bind(navigator.geolocation);
    let count = 0;
    navigator.geolocation.getCurrentPosition = (success, error, options) => {
      count++;
      if (count === 1) error?.({ code: 1 } as GeolocationPositionError);
      else if (count === 2)
        original(
          (position) =>
            success({
              ...position,
              coords: { ...position.coords, accuracy: 1200 },
            } as GeolocationPosition),
          error,
          options,
        );
      else original(success, error, options);
    };
  });
  await page.getByRole("button", { name: "Get my GPS location", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("Allow GPS access");
  await expect(page.getByRole("button", { name: /^Place order/ })).toBeDisabled();
  await page.getByRole("button", { name: "Get my GPS location", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("not accurate enough");
  await page.getByRole("button", { name: "Get my GPS location", exact: true }).click();
  await expect(page.getByText("GPS accuracy: ±5 m")).toBeVisible();
  await expect(page.getByRole("button", { name: /^Place order/ })).toBeEnabled();
});
test("expired quotes and a placement conflict require fresh GPS without resubmitting an order", async ({
  page,
}) => {
  await fixture(page, "expired");
  await page.getByRole("button", { name: "Get my GPS location", exact: true }).click();
  await expect(page.getByText(/The buffer has been shortened/)).toBeVisible();
  await expect(page.getByText(/Your quote has expired/)).toBeVisible({ timeout: 10000 });
  await expect(page.getByRole("button", { name: /^Place order/ })).toBeDisabled();
  await fixture(page, "conflict");
  await page.getByRole("button", { name: "Get my GPS location", exact: true }).click();
  await expect(page.getByRole("button", { name: /^Place order/ })).toBeEnabled();
  await page.getByRole("button", { name: /^Place order/ }).click();
  await expect(page.getByRole("alert")).toContainText("Stock changed");
  await expect(page.getByRole("link", { name: /check My orders/ })).toBeVisible();
  await expect(page.getByRole("button", { name: /^Place order/ })).toBeDisabled();
});
test("loading, initial errors and missing-shop checkout have recovery actions", async ({
  page,
}) => {
  await page.goto(`${checkout}&mock=slow`);
  await expect(page.getByRole("status", { name: "Loading checkout", exact: true })).toBeVisible();
  await page.screenshot({ path: "/tmp/savebite-checkout-loading.png", fullPage: true });
  await expect(page.getByRole("heading", { name: "Payment summary", exact: true })).toBeVisible();
  await page.goto(`${checkout}&mock=error`);
  await expect(page.getByText("Couldn't load checkout", { exact: true })).toBeVisible({
    timeout: 30000,
  });
  await page.evaluate((checkout) => history.replaceState(null, "", checkout), checkout);
  await page.getByRole("button", { name: "Try again", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Payment summary", exact: true })).toBeVisible();
  await page.goto("/checkout?role=user");
  await expect(page.getByText("Choose a shop to check out", { exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Back to cart", exact: true }).click();
  await expect(page).toHaveURL(/\/cart$/);
});
test("long and missing data fit small mobile without hiding payment controls", async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 1000 });
  await fixture(page, "long");
  await page.getByRole("radio", { name: /^Pay at shop/ }).check();
  await fits(page);
  await page.screenshot({ path: "/tmp/savebite-checkout-partial.png", fullPage: true });
});
test("receipt verification retry, payment review, deadlines and cancellation confirmation remain usable", async ({
  page,
}) => {
  const orderId = shopId;
  await page.goto(`/checkout?orderId=${orderId}&role=user&mock=error`);
  await expect(page.getByText("Couldn't verify your order", { exact: true })).toBeVisible({
    timeout: 30000,
  });
  await page.evaluate(
    (id) => history.replaceState(null, "", `/checkout?orderId=${id}&role=user`),
    orderId,
  );
  await page.getByRole("button", { name: "Try again", exact: true }).click();
  await expect(page.getByRole("region", { name: "Order overview", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Cancel order", exact: true }).click();
  await expect(page.getByText("Cancel this order?", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Keep order", exact: true }).click();
  await expect(page.getByText("Cancel this order?", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Cancel order", exact: true }).click();
  await page.getByRole("button", { name: "Confirm cancellation", exact: true }).click();
  await expect(page.getByRole("region", { name: "Order status", exact: true })).toContainText(
    "Cancelled",
  );
  await expect(page.getByRole("button", { name: "Cancel order", exact: true })).toBeDisabled();
});
test("completed receipts retain product reviews and refund sections", async ({ page }) => {
  await page.goto("/checkout?orderId=00000000-0000-4000-8000-000000000006&role=user");
  await expect(page.getByRole("button", { name: "Rate this item", exact: true })).toBeVisible({
    timeout: 30000,
  });
  await expect(page.getByRole("heading", { name: "Refunds", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Rate this item", exact: true }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
});

test("online checkout redirects to the existing payment simulator and keeps the other shops", async ({
  page,
}) => {
  await page.goto(checkout);
  await page.getByRole("button", { name: "Get my GPS location", exact: true }).click();
  await expect(page.getByRole("button", { name: /^Place order/ })).toBeEnabled();
  await page.getByRole("button", { name: /^Place order/ }).click();
  await expect(page).toHaveURL(/\/checkout\/simulation\?orderId=/);
  await page.waitForLoadState("domcontentloaded");
  await ready(page);
  const groups = await page.evaluate(async () => {
    const { cartApi } = await import("/src/features/cart/api/cartApi.ts");
    return (await cartApi.getPage()).page.items.map((shop) => shop.shopName);
  });
  expect(groups).toEqual(["Tiệm Bánh An Nhiên", "Góc Rau Đà Lạt"]);
});

test("empty items and unavailable shop guards are explained without allowing placement", async ({
  page,
}) => {
  await page.goto(`${checkout}&mock=empty`);
  await expect(page.getByText("No items to check out", { exact: true })).toBeVisible({
    timeout: 30000,
  });
  await expect(page.getByRole("button", { name: /^Place order/ })).toHaveCount(0);
  await page.goto("/cart?role=user");
  await ready(page);
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    core.cart.shops[0].canCheckout = false;
    core.cart.shops[0].items[0].isAvailable = false;
    core.cart.shops[0].items[0].unavailableReason = "This deal has ended.";
    saveCore();
  });
  await page.goto(checkout);
  await expect(page.getByText("This deal has ended.", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Get my GPS location", exact: true }).click();
  await expect(page.getByText("GPS accuracy: ±5 m")).toBeVisible();
  await expect(page.getByRole("button", { name: /^Place order/ })).toBeDisabled();
  await expect(page.getByText(/Some items are unavailable or have changed/)).toBeVisible();
});

test("receipt review warning, expired payment and missing photos stay readable on mobile", async ({
  page,
}) => {
  await page.setViewportSize({ width: 360, height: 1000 });
  await page.goto("/cart?role=user");
  await ready(page);
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    const order = core.orders[0];
    order.paymentReviewRequired = true;
    order.paymentExpiresAt = new Date(Date.now() - 60000).toISOString();
    order.hardDeadlineAt = new Date(Date.now() + 5 * 60000).toISOString();
    order.shopName = "Cửa hàng thực phẩm ".repeat(8);
    order.items[0].productName = "Bữa ăn gia đình ".repeat(8);
    order.items[0].imageUrl = "/broken-item.jpg";
    saveCore();
  });
  await page.goto(`/checkout?orderId=${shopId}&role=user`);
  await expect(page.getByText(/Your payment needs review/)).toBeVisible();
  await expect(page.getByRole("button", { name: /Payment expired · Verifying/ })).toBeDisabled();
  await expect(page.getByText(/min left · Hard deadline/)).toBeVisible();
  await fits(page);
  await page.screenshot({ path: "/tmp/savebite-checkout-receipt-review.png", fullPage: true });
});

test("completed checkout can submit and reopen a review with the shared dialogs", async ({
  page,
}) => {
  await page.goto("/checkout?orderId=00000000-0000-4000-8000-000000000006&role=user");
  await page.getByRole("button", { name: "Rate this item", exact: true }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.getByRole("dialog").locator("textarea").fill("Món ăn ngon, đóng gói cẩn thận.");
  await page.screenshot({ path: "/tmp/savebite-checkout-rate-dialog.png", fullPage: true });
  await page.getByRole("button", { name: "Submit review", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.getByRole("button", { name: /Reviewed.*Edit review/ }).click();
  await expect(page.getByRole("dialog", { name: "Edit your review", exact: true })).toBeVisible();
  await expect(page.getByRole("dialog").locator("textarea")).toHaveValue(
    "Món ăn ngon, đóng gói cẩn thận.",
  );
  await page.screenshot({ path: "/tmp/savebite-checkout-edit-dialog.png", fullPage: true });
  await page.getByRole("dialog").locator("textarea").fill("Món ăn ngon, sẽ quay lại.");
  await page.getByRole("button", { name: "Save changes", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
});

test("refund request and bank destination remain styled, validated and keyboard accessible", async ({
  page,
}) => {
  await page.setViewportSize({ width: 360, height: 1000 });
  await page.goto("/cart?role=user");
  await ready(page);
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    core.refunds = [];
    core.orders[5].completedAt = new Date().toISOString();
    saveCore();
  });
  await page.goto("/checkout?orderId=00000000-0000-4000-8000-000000000006&role=user");
  await page.getByRole("button", { name: "Request refund", exact: true }).click();
  await page.getByRole("button", { name: "Submit refund request", exact: true }).click();
  await expect(
    page.getByText("Select at least one item and quantity.", { exact: true }),
  ).toBeVisible();
  await page
    .getByRole("region", { name: "Order refunds", exact: true })
    .getByRole("checkbox")
    .check();
  await expect(page.getByRole("textbox", { name: /Reason for refund/ })).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Choose evidence images", exact: true }),
  ).toBeVisible();
  await fits(page);
  await page.screenshot({ path: "/tmp/savebite-checkout-refund-request.png", fullPage: true });
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    const { makeRefunds } = await import("/src/mocks/factories/core.ts");
    core.refunds = [makeRefunds()[0]];
    saveCore();
  });
  await page.reload();
  await expect(page.getByRole("button", { name: "Enter bank details", exact: true })).toBeVisible();
  await expect(page.getByRole("combobox", { name: "Bank", exact: true })).toBeEnabled();
  await page.getByRole("button", { name: "Upload bank QR", exact: true }).click();
  await expect(
    page.getByRole("button", { name: "Choose your bank QR image", exact: true }),
  ).toBeVisible();
  await fits(page);
  await page.screenshot({ path: "/tmp/savebite-checkout-refund-destination.png", fullPage: true });
});
