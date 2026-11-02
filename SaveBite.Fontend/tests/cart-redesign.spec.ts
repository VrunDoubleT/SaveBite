import { test, expect, type Page } from "@playwright/test";
const firstShop = "Bếp Xanh Sài Gòn";
const secondShop = "Tiệm Bánh An Nhiên";
const firstProduct = "Túi bánh cuối ngày";
const secondProduct = "Hộp rau tươi Đà Lạt";
const secondShopId = "00000000-0000-4000-8000-000000000041";
test.beforeEach(async ({ page }) => {
  await page.route("https://unpkg.com/**", (route) => route.abort());
  await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
});

async function seedCart(page: Page, count = 2) {
  await page.goto("/__ui?role=user");
  await page.waitForFunction(() =>
    Boolean((window as unknown as { __savebiteMock?: unknown }).__savebiteMock),
  );
  await page.evaluate(async (count) => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    const original = structuredClone(core.cart.shops[0]);
    const uid = (n: number) => `00000000-0000-4000-8000-${String(n).padStart(12, "0")}`;
    core.cart.shops = Array.from({ length: count }, (_, index) => {
      if (!index) return original;
      const shopId = uid(40 + index);
      const shopName = index === 1 ? "Tiệm Bánh An Nhiên" : `Cửa hàng ${index + 1}`;
      const deal = core.deals[index + 3];
      deal.shopId = shopId;
      deal.shopName = shopName;
      deal.status = 1;
      deal.isPurchasable = true;
      core.products[index + 3].shopId = shopId;
      const item = {
        ...structuredClone(original.items[0]),
        id: uid(500 + index),
        flashDealId: deal.id,
        flashDealVariantId: deal.variants[0].id,
        productId: deal.productId,
        productName: deal.productName,
        quantity: 1,
        unitPrice: deal.minDealPrice,
        subtotal: deal.minDealPrice,
        isAvailable: true,
        canUpdateQuantity: true,
        unavailableReason: null,
      };
      return {
        ...structuredClone(original),
        shopId,
        shopName,
        items: [item],
        totalQuantity: 1,
        subtotal: item.subtotal,
        canCheckout: true,
      };
    });
    core.cart.totalQuantity = core.cart.shops.reduce((sum, shop) => sum + shop.totalQuantity, 0);
    saveCore();
  }, count);
}

for (const width of [360, 768, 1280]) {
  test(`cart edits and per-shop checkout work at ${width}px`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));
    await page.setViewportSize({ width, height: 1000 });
    await seedCart(page);
    await page.goto("/cart?role=user");
    const store = page.getByRole("region", { name: `Cart from ${firstShop}`, exact: true });
    const other = page.getByRole("region", { name: `Cart from ${secondShop}`, exact: true });
    await expect(
      store.getByRole("spinbutton", { name: `Quantity of ${firstProduct}`, exact: true }),
    ).toHaveValue("1");
    await expect(other).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: `/tmp/savebite-cart-${width}.png`, fullPage: true });
    const update = page.waitForRequest(
      (request) => request.method() === "PUT" && request.url().includes("/cart/items/"),
    );
    await store
      .getByRole("spinbutton", { name: `Quantity of ${firstProduct}`, exact: true })
      .fill("3");
    await store
      .getByRole("spinbutton", { name: `Quantity of ${firstProduct}`, exact: true })
      .press("Enter");
    expect((await update).postDataJSON()).toEqual({ quantity: 3 });
    await expect(
      store.getByRole("spinbutton", { name: `Quantity of ${firstProduct}`, exact: true }),
    ).toHaveValue("3");
    await expect(store.getByText("105.000đ", { exact: true })).toBeVisible();
    await expect(page.getByText("Quantity updated.", { exact: true })).toBeVisible();
    await expect(other.getByRole("spinbutton")).toHaveValue("1");
    const deletion = page.waitForRequest(
      (request) => request.method() === "DELETE" && request.url().includes("/cart/items/"),
    );
    await store.getByRole("button", { name: `Remove ${secondProduct}`, exact: true }).click();
    await deletion;
    await expect(store.getByRole("spinbutton")).toHaveCount(1);
    await expect(page.getByText("Item removed from your cart.", { exact: true })).toBeVisible();
    await expect(other.getByRole("spinbutton")).toHaveValue("1");
    await other.getByRole("button", { name: `Checkout with ${secondShop}`, exact: true }).click();
    await expect(page).toHaveURL(new RegExp(`/checkout\\?shopId=${secondShopId}$`));
    await expect(page.getByRole("heading", { name: "Payment summary", exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: /Increase quantity of/ })).toHaveCount(0);
    await expect(page.getByRole("button", { name: /Remove / })).toHaveCount(0);
    expect(errors).toEqual([]);
  });
}

test("manual quantity validation preserves stock and integer bounds without sending invalid mutations", async ({
  page,
}) => {
  await seedCart(page, 1);
  await page.goto("/cart?role=user");
  const input = page.getByRole("spinbutton", { name: `Quantity of ${firstProduct}`, exact: true });
  await expect(input).toHaveValue("1");
  const updates: string[] = [];
  page.on("request", (request) => {
    if (request.method() === "PUT" && request.url().includes("/cart/items/"))
      updates.push(request.postData() || "");
  });
  for (const value of ["0", "26", "2.5"]) {
    await input.fill(value);
    await input.press("Enter");
    await expect(input).toHaveAttribute("aria-invalid", "true");
    await expect(page.getByRole("alert")).toHaveText("Enter a quantity from 1 to 25.");
    await expect(input).toHaveValue("1");
  }
  expect(updates).toEqual([]);
  await input.fill("25");
  await input.press("Enter");
  await expect(input).toHaveValue("25");
  await expect(input).toHaveAttribute("aria-invalid", "false");
  await expect(
    page.getByRole("button", { name: `Increase quantity of ${firstProduct}`, exact: true }),
  ).toBeDisabled();
  await page
    .getByRole("button", { name: `Decrease quantity of ${firstProduct}`, exact: true })
    .click();
  await expect(input).toHaveValue("24");
});

test("unavailable and quantity-locked items preserve checkout guards and remain removable", async ({
  page,
}) => {
  await seedCart(page);
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    core.cart.shops[0].canCheckout = false;
    Object.assign(core.cart.shops[0].items[1], {
      isAvailable: false,
      canUpdateQuantity: false,
      availableQuantity: 0,
      unavailableReason: "This deal has ended.",
    });
    saveCore();
  });
  await page.goto("/cart?role=user");
  await expect(page.getByText("This deal has ended.", { exact: true })).toBeVisible();
  await expect(
    page.getByRole("button", { name: `Checkout with ${firstShop}`, exact: true }),
  ).toBeDisabled();
  await expect(
    page.getByRole("button", { name: `Checkout with ${secondShop}`, exact: true }),
  ).toBeEnabled();
  await expect(
    page.getByRole("spinbutton", { name: `Quantity of ${secondProduct}`, exact: true }),
  ).toBeDisabled();
  await expect(
    page.getByRole("button", { name: `Increase quantity of ${secondProduct}`, exact: true }),
  ).toBeDisabled();
  await page.screenshot({ path: "/tmp/savebite-cart-unavailable.png", fullPage: true });
  await page.getByRole("button", { name: `Remove ${secondProduct}`, exact: true }).click();
  await expect(
    page.getByRole("spinbutton", { name: `Quantity of ${secondProduct}`, exact: true }),
  ).toHaveCount(0);
});

test("loading, request errors, mutation errors and retry retain authoritative cart values", async ({
  page,
}) => {
  await seedCart(page);
  await page.goto("/cart?role=user&mock=slow");
  await expect(page.getByRole("status", { name: "Loading your cart", exact: true })).toBeVisible();
  await page.screenshot({ path: "/tmp/savebite-cart-loading.png", fullPage: true });
  await expect(
    page.getByRole("spinbutton", { name: `Quantity of ${firstProduct}`, exact: true }),
  ).toHaveValue("1");
  await page.goto("/cart?role=user&mock=error");
  await expect(page.getByText("Couldn't refresh your cart", { exact: true })).toBeVisible({
    timeout: 30000,
  });
  await page.evaluate(() => history.replaceState(null, "", "/cart?role=user"));
  await page.getByRole("button", { name: "Try again", exact: true }).click();
  const input = page.getByRole("spinbutton", { name: `Quantity of ${firstProduct}`, exact: true });
  await expect(input).toHaveValue("1");
  await page.evaluate(() => history.replaceState(null, "", "/cart?role=user&mock=error"));
  await page
    .getByRole("button", { name: `Increase quantity of ${firstProduct}`, exact: true })
    .click();
  await expect(page.getByText("Couldn't refresh your cart", { exact: true })).toBeVisible();
  await expect(input).toHaveValue("1");
  await expect(
    page.getByRole("button", { name: `Checkout with ${firstShop}`, exact: true }),
  ).toBeDisabled();
  await page.screenshot({ path: "/tmp/savebite-cart-error.png", fullPage: true });
  await page.evaluate(() => history.replaceState(null, "", "/cart?role=user"));
  await page.getByRole("button", { name: "Try again", exact: true }).click();
  await expect(
    page.getByRole("button", { name: `Checkout with ${firstShop}`, exact: true }),
  ).toBeEnabled();
});

test("removing the final items reveals discovery and clears the active-cart lifecycle on exit", async ({
  page,
}) => {
  await seedCart(page, 1);
  await page.goto("/cart?role=user");
  await page.getByRole("button", { name: `Remove ${firstProduct}`, exact: true }).click();
  await expect(
    page.getByRole("button", { name: `Remove ${firstProduct}`, exact: true }),
  ).toHaveCount(0);
  await page.getByRole("button", { name: `Remove ${secondProduct}`, exact: true }).click();
  await expect(page.getByText("Your cart is empty", { exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Explore deals", exact: true }).click();
  await expect(page).toHaveURL("/");
  expect(
    await page.evaluate(async () => {
      const { useCartStore } = await import("/src/features/cart/stores/cartStore.ts");
      return useCartStore.getState().cartPageActive;
    }),
  ).toBe(false);
});

test("cursor scrolling loads more shop groups and exposes a retry after failure", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await seedCart(page, 7);
  await page.goto("/cart?role=user");
  const groups = page.getByRole("region", { name: /^Cart from / });
  await expect(groups).toHaveCount(5);
  await page.evaluate(() => history.replaceState(null, "", "/cart?role=user&mock=error"));
  const next = page.waitForRequest(
    (request) =>
      request.url().includes("/cart/page?") &&
      Boolean(new URL(request.url()).searchParams.get("cursor")),
  );
  await page.keyboard.press("End");
  await next;
  await expect(page.getByRole("button", { name: "Retry loading more", exact: true })).toBeVisible();
  await expect(groups).toHaveCount(5);
  await page.evaluate(() => history.replaceState(null, "", "/cart?role=user"));
  await page.getByRole("button", { name: "Retry loading more", exact: true }).click();
  await expect(groups).toHaveCount(7);
  await expect(page.getByText("All shops loaded", { exact: true })).toBeVisible();
});

test("long names, broken photos and large prices fit mobile", async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 1000 });
  await seedCart(page, 1);
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    const shop = core.cart.shops[0];
    shop.shopName = "Cửa hàng thực phẩm ".repeat(10);
    shop.shopLogoUrl = "/missing-logo.jpg";
    shop.shopAddress = "Địa chỉ nhận hàng ".repeat(15);
    Object.assign(shop.items[0], {
      productName: "Bữa ăn gia đình ".repeat(12),
      variantName: "Khẩu phần đặc biệt ".repeat(8),
      imageUrl: "/missing-product.jpg",
      unitPrice: 25000000,
      originalPrice: 30000000,
      quantity: 2,
      subtotal: 50000000,
    });
    shop.subtotal = shop.items.reduce((sum, item) => sum + item.subtotal, 0);
    shop.totalQuantity = 3;
    core.cart.totalQuantity = 3;
    saveCore();
  });
  await page.goto("/cart?role=user");
  await expect(page.getByRole("button", { name: /^Checkout with / })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: "/tmp/savebite-cart-partial.png", fullPage: true });
});

test("empty scenario offers discovery and guests still require sign-in", async ({ page }) => {
  await page.goto("/cart?role=user&mock=empty");
  await expect(page.getByText("Your cart is empty", { exact: true })).toBeVisible();
  await page.screenshot({ path: "/tmp/savebite-cart-empty.png", fullPage: true });
  await page.goto("/cart?role=guest");
  await expect(page).toHaveURL(/\/login$/);
});

test("default mock cart has three linked shops and checkout removes only the quoted shop", async ({
  page,
}) => {
  await page.goto("/cart?role=user");
  await expect(page.getByRole("region", { name: /^Cart from / })).toHaveCount(3, {
    timeout: 30000,
  });
  for (const name of [firstShop, secondShop, "Góc Rau Đà Lạt"]) {
    await expect(
      page.getByRole("region", { name: `Cart from ${name}`, exact: true }).getByRole("spinbutton"),
    ).toHaveCount(2);
  }
  await page.screenshot({ path: "/tmp/savebite-cart-multishop.png", fullPage: true });
  const result = await page.evaluate(async () => {
    const { httpClient: api } = await import("/src/shared/api/httpClient.ts");
    const { core } = await import("/src/mocks/coreState.ts");
    const { cartApi } = await import("/src/features/cart/api/cartApi.ts");
    const stores = (await cartApi.getPage()).page.items;
    const selected = stores[1];
    const linked = stores.flatMap((shop) =>
      shop.items.map(
        (item) => core.deals.find((deal) => deal.id === item.flashDealId)?.shopId === shop.shopId,
      ),
    );
    const quote = await api.post("/checkout/quotes", { shopId: selected.shopId });
    const placed = await api.post("/checkout/orders", {
      quoteId: quote.data.data.id,
      paymentMethod: "PayAtShop",
      requestId: "multishop-demo",
    });
    const remaining = (await cartApi.getPage()).page.items.map((shop) => shop.shopId);
    const added = await cartApi.addItem(selected.items[0].flashDealVariantId, 2);
    return {
      counts: stores.map((shop) => shop.totalQuantity),
      totals: stores.map((shop) => shop.subtotal),
      linked,
      placedShop: placed.data.data.shopId,
      placedTotal: placed.data.data.totalAmount,
      selectedId: selected.shopId,
      remaining,
      expectedRemaining: [stores[0].shopId, stores[2].shopId],
      firstPreserved:
        JSON.stringify(added.shops.find((shop) => shop.shopId === stores[0].shopId)) ===
        JSON.stringify(stores[0]),
      addedBack: added.shops.find((shop) => shop.shopId === selected.shopId)?.items[0].quantity,
    };
  });
  expect(result.counts).toEqual([2, 3, 4]);
  expect(result.totals).toEqual([53000, 94000, 164000]);
  expect(result.linked.every(Boolean)).toBe(true);
  expect(result.placedShop).toBe(result.selectedId);
  expect(result.placedTotal).toBe(94000);
  expect(result.remaining).toEqual(result.expectedRemaining);
  expect(result.firstPreserved).toBe(true);
  expect(result.addedBack).toBe(2);
  await page.reload();
  await expect(page.getByRole("region", { name: /^Cart from / })).toHaveCount(3);
});

test("older mock sessions gain extra shops once while retaining edited rows and subsequent removals", async ({
  page,
}) => {
  await seedCart(page, 1);
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    core.cart.shops[0].items[0].quantity = 4;
    core.cart.shops[0].items[0].subtotal = 104000;
    core.cart.shops[0].totalQuantity = 5;
    core.cart.shops[0].subtotal = 131000;
    core.cart.totalQuantity = 5;
    saveCore();
    const saved = JSON.parse(sessionStorage.getItem("savebite-mock-core-v1")!);
    delete saved.cartSeedVersion;
    saved.products = saved.products.slice(0, 18);
    saved.deals = saved.deals.slice(0, 18);
    sessionStorage.setItem("savebite-mock-core-v1", JSON.stringify(saved));
  });
  await page.goto("/cart?role=user");
  await expect(page.getByRole("region", { name: /^Cart from / })).toHaveCount(3);
  await expect(
    page.getByRole("spinbutton", { name: `Quantity of ${firstProduct}`, exact: true }),
  ).toHaveValue("4");
  const upgraded = await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    const result = {
      total: core.cart.totalQuantity,
      firstSubtotal: core.cart.shops[0].subtotal,
      version: core.cartSeedVersion,
      products: core.products.length,
      deals: core.deals.length,
    };
    core.cart.shops = core.cart.shops.slice(0, 1);
    core.cart.totalQuantity = 5;
    saveCore();
    return result;
  });
  expect(upgraded).toEqual({
    total: 12,
    firstSubtotal: 131000,
    version: 1,
    products: 22,
    deals: 22,
  });
  await page.reload();
  await expect(page.getByRole("region", { name: /^Cart from / })).toHaveCount(1);
  await expect(
    page.getByRole("spinbutton", { name: `Quantity of ${firstProduct}`, exact: true }),
  ).toHaveValue("4");
});
