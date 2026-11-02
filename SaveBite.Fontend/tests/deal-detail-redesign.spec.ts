import { test, expect, type Page } from "@playwright/test";

const dealId = "00000000-0000-4000-8000-000000000002";
const detail = `/flash-deals/${dealId}`;
const largeId = "00000000-0000-4000-8000-000000000050";

test.beforeEach(async ({ page }) => {
  await page.route("https://unpkg.com/**", (route) => route.abort());
  await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
});

async function seedVariants(page: Page, role = "user") {
  await page.goto(`/__ui?role=${role}`);
  await page.waitForFunction(() =>
    Boolean((window as unknown as { __savebiteMock?: unknown }).__savebiteMock),
  );
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    const deal = core.deals[1];
    deal.status = 1;
    deal.saleStartTime = new Date(Date.now() - 60000).toISOString();
    deal.orderEndTime = new Date(Date.now() + 3600000).toISOString();
    deal.shopClosingTime = new Date(Date.now() + 7200000).toISOString();
    deal.attributeGroups = [
      { name: "Size", values: ["Regular", "Large"] },
      { name: "Spice", values: ["Mild", "Hot"] },
    ];
    const base = {
      ...deal.variants[0],
      totalQuantity: 8,
      soldQuantity: 5,
      status: 0,
      isPurchasable: true,
    };
    deal.variants = [
      {
        ...base,
        attributes: { Size: "Regular", Spice: "Mild" },
        variantName: "Regular / Mild",
        availableQuantity: 3,
        dealPrice: 26000,
        imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      },
      {
        ...base,
        id: "00000000-0000-4000-8000-000000000050",
        attributes: { Size: "Large", Spice: "Mild" },
        variantName: "Large / Mild",
        availableQuantity: 2,
        dealPrice: 35000,
        imageUrl: "/images/deals/banh-mi/bm-lon-khong-cay.jpg",
      },
      {
        ...base,
        id: "00000000-0000-4000-8000-000000000051",
        attributes: { Size: "Large", Spice: "Hot" },
        variantName: "Large / Hot",
        availableQuantity: 0,
        dealPrice: 40000,
        imageUrl: "/images/deals/banh-mi/bm-lon-cay-vua.jpg",
      },
      {
        ...base,
        id: "00000000-0000-4000-8000-000000000052",
        attributes: { Size: "Regular", Spice: "Hot" },
        variantName: "Regular / Hot",
        availableQuantity: 2,
        dealPrice: 30000,
        imageUrl: "/images/deals/banh-mi/bm-thuong-cay-vua.jpg",
      },
    ];
    saveCore();
  });
}

for (const width of [360, 768, 1280]) {
  test(`detail and alias render without overflow at ${width}px`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));
    await page.setViewportSize({ width, height: 1000 });
    await page.goto(`${detail}?role=user`);
    await expect(page.getByRole("heading", { level: 1, name: "Túi bánh cuối ngày" })).toBeVisible();
    await expect(page.getByRole("button", { name: /Add to cart/ })).toBeEnabled();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: `/tmp/savebite-detail-${width}.png`, fullPage: true });
    await page.goto(`/deals/${dealId}?role=user`);
    await expect(page.getByRole("heading", { level: 1, name: "Túi bánh cuối ngày" })).toBeVisible();
    const store = page.getByRole("link", { name: "View store", exact: true });
    await expect(store).toHaveAttribute("href", "/shops/00000000-0000-4000-8000-000000000001");
    await store.click();
    await expect(page).toHaveURL(/\/shops\/00000000-0000-4000-8000-000000000001$/);
    expect(errors).toEqual([]);
  });
}

test("variants update price, photo and stock; cart receives the selected variant and quantity", async ({
  page,
}) => {
  await seedVariants(page);
  await page.goto(`${detail}?role=user`);
  const buy = page.getByRole("region", { name: "Choose your deal" });
  await expect(buy.getByRole("button", { name: "Regular", exact: true })).toHaveAttribute(
    "aria-pressed",
    "true",
  );
  await buy.getByRole("button", { name: "Large", exact: true }).focus();
  await page.keyboard.press("Enter");
  await expect(buy.getByRole("button", { name: "Large", exact: true })).toHaveAttribute(
    "aria-pressed",
    "true",
  );
  await expect(
    page.getByRole("region", { name: "Product gallery" }).locator("img").first(),
  ).toHaveAttribute("src", "/images/deals/banh-mi/bm-lon-khong-cay.jpg");
  await expect(
    buy.getByRole("button", { name: "Add to cart · 35.000đ", exact: true }),
  ).toBeEnabled();
  await buy.getByRole("button", { name: "Increase quantity" }).click();
  await expect(buy.getByRole("button", { name: "Increase quantity" })).toBeDisabled();
  await expect(buy.getByRole("status", { name: "Selected quantity" })).toHaveText("2");
  const request = page.waitForRequest(
    (request) => request.method() === "POST" && request.url().endsWith("/cart/items"),
  );
  await buy.getByRole("button", { name: "Add to cart · 70.000đ", exact: true }).click();
  expect((await request).postDataJSON()).toEqual({ flashDealVariantId: largeId, quantity: 2 });
  await expect(buy.getByRole("link", { name: "View cart", exact: true })).toBeVisible();
  await buy.getByRole("button", { name: "Hot", exact: true }).click();
  await expect(buy.getByRole("button", { name: "Sold Out", exact: true })).toBeDisabled();
  await page.getByRole("button", { name: "View product photo 1", exact: true }).click();
  await expect(buy.getByRole("button", { name: "Regular", exact: true })).toHaveAttribute(
    "aria-pressed",
    "true",
  );
  await expect(buy.getByRole("button", { name: /Add to cart/ })).toBeEnabled();
  await page.screenshot({ path: "/tmp/savebite-detail-variants.png", fullPage: true });
});

test("generic variant options preserve selected and sold-out states", async ({ page }) => {
  await seedVariants(page);
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    core.deals[1].attributeGroups = undefined;
    core.deals[1].variants.forEach((variant) => {
      variant.attributes = undefined;
    });
    saveCore();
  });
  await page.goto(`${detail}?role=user`);
  await page.getByRole("button", { name: "Large / Mild", exact: true }).click();
  await expect(page.getByRole("button", { name: "Large / Mild", exact: true })).toHaveAttribute(
    "aria-pressed",
    "true",
  );
  await expect(page.getByRole("button", { name: "Large / Hot", exact: true })).toBeDisabled();
  await expect(
    page.getByRole("button", { name: "Add to cart · 35.000đ", exact: true }),
  ).toBeEnabled();
});

test("deal, time, variant and stock restrictions keep ordering disabled", async ({ page }) => {
  await seedVariants(page);
  const cases = [
    { status: 0, reason: "Unavailable" },
    { status: 2, reason: "Unavailable" },
    { status: 3, reason: "Sold Out" },
    { status: 4, reason: "Unavailable" },
    { status: 5, reason: "Unavailable" },
    { before: true, reason: "Sale Starts Soon" },
    { ended: true, reason: "Sale Ended" },
    { closed: true, reason: "Sale Ended" },
    { stock: 0, reason: "Sold Out" },
    { inactive: true, reason: "Variant Inactive" },
    { denied: true, reason: "Unavailable" },
  ];
  for (const item of cases) {
    await page.evaluate(async (item) => {
      const { core, saveCore } = await import("/src/mocks/coreState.ts");
      const deal = core.deals[1];
      deal.status = item.status ?? 1;
      deal.saleStartTime = new Date(Date.now() + (item.before ? 3600000 : -60000)).toISOString();
      deal.orderEndTime = new Date(Date.now() + (item.ended ? -60000 : 3600000)).toISOString();
      deal.shopClosingTime = new Date(Date.now() + (item.closed ? -60000 : 7200000)).toISOString();
      Object.assign(deal.variants[0], {
        availableQuantity: item.stock ?? 3,
        status: item.inactive ? 1 : 0,
        isPurchasable: !item.denied,
      });
      saveCore();
    }, item);
    await page.goto(`${detail}?role=user`);
    await expect(
      page
        .getByRole("region", { name: "Choose your deal" })
        .getByRole("button", { name: item.reason, exact: true }),
    ).toBeDisabled();
    await expect(page.getByRole("button", { name: "Increase quantity" })).toBeDisabled();
    if (item.before) await expect(page.getByText("Sale starts", { exact: true })).toBeVisible();
    else if (item.ended || item.closed)
      await expect(page.getByText("Ordering has ended", { exact: true })).toBeVisible();
    else if (item.status !== undefined)
      await expect(page.getByText("Deal status", { exact: true })).toBeVisible();
  }
});

test("guest ordering continues to sign-in", async ({ page }) => {
  await page.goto(`${detail}?role=guest`);
  await page.getByRole("button", { name: /Add to cart/ }).click();
  await expect(page).toHaveURL(/\/login$/);
});

test("failed detail requests retry; missing deals offer discovery", async ({ page }) => {
  await page.goto(`${detail}?role=user&mock=error`);
  await expect(page.getByText("Couldn't load this deal", { exact: true })).toBeVisible({
    timeout: 30000,
  });
  await page.screenshot({ path: "/tmp/savebite-detail-error.png", fullPage: true });
  await page.evaluate(() => history.replaceState(null, "", location.pathname + "?role=user"));
  await page.getByRole("button", { name: "Try again", exact: true }).click();
  await expect(page.getByRole("button", { name: /Add to cart/ })).toBeEnabled();
  await page.goto("/flash-deals/missing-deal?role=user");
  await expect(page.getByText("This deal is no longer available", { exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Browse nearby deals", exact: true }).click();
  await expect(page).toHaveURL("/");
});

test("slow loading and empty variant data have usable states", async ({ page }) => {
  await page.goto(`${detail}?role=user&mock=slow`);
  await expect(page.getByRole("status", { name: "Loading flash deal", exact: true })).toBeVisible();
  await page.screenshot({ path: "/tmp/savebite-detail-loading.png", fullPage: true });
  await expect(page.getByRole("button", { name: /Add to cart/ })).toBeEnabled();
  await page.goto(`${detail}?role=user&mock=empty`);
  await expect(page.getByRole("button", { name: "Sold Out", exact: true })).toBeDisabled();
  await expect(
    page.getByText("There are no portions available for this deal.", { exact: true }),
  ).toBeVisible();
  await expect(page.getByText("0 of 0 sold", { exact: true })).toBeVisible();
});

test("cart failure shows feedback and allows another attempt", async ({ page }) => {
  await page.goto(`${detail}?role=user`);
  await expect(page.getByRole("button", { name: /Add to cart/ })).toBeEnabled();
  await expect(
    page.getByRole("status", { name: "Loading customer reviews", exact: true }),
  ).toHaveCount(0);
  await page.evaluate(() =>
    history.replaceState(null, "", location.pathname + "?role=user&mock=error"),
  );
  await page.getByRole("button", { name: /Add to cart/ }).click();
  await expect(page.getByRole("alert")).toBeVisible();
  await expect(page.getByRole("link", { name: "View cart", exact: true })).toHaveCount(0);
  await page.evaluate(() => history.replaceState(null, "", location.pathname + "?role=user"));
  await page.getByRole("button", { name: /Add to cart/ }).click();
  await expect(page.getByRole("link", { name: "View cart", exact: true })).toBeVisible();
});

test("long text, missing metadata and broken photos fit on mobile", async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 1000 });
  await seedVariants(page);
  await page.evaluate(async () => {
    const { core, saveCore } = await import("/src/mocks/coreState.ts");
    Object.assign(core.deals[1], {
      productName: "Bữa ăn gia đình ".repeat(12),
      shopName: "Cửa hàng ".repeat(12),
      description: undefined,
      shopAddress: "Địa chỉ nhận hàng ".repeat(20),
      shopLogoUrl: "/missing-logo.jpg",
      productImageUrl: undefined,
      productImageUrls: [],
      distanceInKm: undefined,
      categoryName: "Danh mục thực phẩm ".repeat(8),
    });
    core.deals[1].variants.forEach((variant) => {
      variant.imageUrl = "/missing-product-photo.jpg";
    });
    saveCore();
  });
  await page.goto(`${detail}?role=user`);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  await expect(page.getByText("Image unavailable", { exact: true })).toBeVisible();
  await expect(
    page.getByText("The store hasn't added a description for this item yet.", { exact: true }),
  ).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: "/tmp/savebite-detail-partial.png", fullPage: true });
});

test("normal seeded gallery has five selectable photos linked to size and spice", async ({
  page,
}) => {
  await page.goto(`${detail}?role=user`);
  await expect(page.getByRole("heading", { level: 1, name: "Túi bánh cuối ngày" })).toBeVisible({
    timeout: 30000,
  });
  const gallery = page.getByRole("region", { name: "Product gallery" });
  const buy = page.getByRole("region", { name: "Choose your deal" });
  const photos = gallery.getByRole("button", { name: /View product photo/ });
  await expect(photos).toHaveCount(5);
  await buy.getByRole("button", { name: "Lớn", exact: true }).click();
  await buy.getByRole("button", { name: "Cay vừa", exact: true }).click();
  await expect(
    buy.getByRole("button", { name: "Add to cart · 37.000đ", exact: true }),
  ).toBeEnabled();
  await expect(gallery.locator("img").first()).toHaveAttribute(
    "src",
    "/images/deals/banh-mi/bm-lon-cay-vua.jpg",
  );
  const files = [
    "bm-overview.jpg",
    "bm-thuong-khong-cay.jpg",
    "bm-thuong-cay-vua.jpg",
    "bm-lon-khong-cay.jpg",
    "bm-lon-cay-vua.jpg",
  ];
  for (const [index, file] of files.entries()) {
    await photos.nth(index).click();
    await expect(photos.nth(index)).toHaveAttribute("aria-pressed", "true");
    await expect(gallery.locator("img").first()).toHaveAttribute(
      "src",
      `/images/deals/banh-mi/${file}`,
    );
  }
  await page.setViewportSize({ width: 360, height: 1000 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: "/tmp/savebite-detail-gallery-360.png", fullPage: true });
  await page.setViewportSize({ width: 1280, height: 1000 });
  await page.screenshot({ path: "/tmp/savebite-detail-gallery-1280.png", fullPage: true });
});

test("older mock sessions receive gallery data without losing unrelated edits or cart", async ({
  page,
}) => {
  await page.goto(`${detail}?role=user`);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  const cart = await page.evaluate(() => {
    const key = "savebite-mock-core-v1";
    const saved = JSON.parse(sessionStorage.getItem(key) || "null");
    delete saved.mediaSeedVersion;
    saved.deals[1].productImageUrls = [saved.deals[1].productImageUrl];
    saved.deals[1].variants = saved.deals[1].variants.slice(0, 1);
    saved.products[0].name = "Giữ thay đổi phiên mock";
    sessionStorage.setItem(key, JSON.stringify(saved));
    return JSON.stringify(saved.cart);
  });
  await page.reload();
  await expect(page.getByRole("button", { name: /View product photo/ })).toHaveCount(5);
  const preserved = await page.evaluate(async () => {
    const { core } = await import("/src/mocks/coreState.ts");
    return {
      name: core.products[0].name,
      cart: JSON.stringify(core.cart),
      variants: core.deals[1].variants.length,
    };
  });
  expect(preserved).toEqual({ name: "Giữ thay đổi phiên mock", cart, variants: 4 });
});
