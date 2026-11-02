import { test, expect } from "@playwright/test";
const shop = "/shops/00000000-0000-4000-8000-000000000001";
test.beforeEach(async ({ page }) => {
  await page.route("https://unpkg.com/**", (route) => route.abort());
  await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
});
for (const width of [360, 768, 1280]) {
  test(`complete storefront uses shared deal and review UI at ${width}px`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));
    await page.setViewportSize({ width, height: 1000 });
    await page.goto(`${shop}?role=user`);
    await expect(
      page.getByRole("heading", { level: 1, name: "Bếp Xanh Sài Gòn", exact: true }),
    ).toBeVisible({ timeout: 30000 });
    const cards = page.locator('a[aria-label^="View deal:"]');
    await expect(cards).toHaveCount(8);
    const profile = page.getByRole("region", { name: "Store profile", exact: true });
    await expect(profile.getByText("Open now", { exact: true })).toBeVisible();
    await expect(profile.getByText("96% Response rate", { exact: true })).toHaveCount(0);
    await profile.getByRole("button", { name: "More store info", exact: true }).click();
    await expect(
      profile.getByRole("heading", { name: "About this store", exact: true }),
    ).toBeVisible();
    await expect(
      profile.getByRole("button", { name: "Collapse info", exact: true }),
    ).toHaveAttribute("aria-expanded", "true");
    await profile.getByRole("button", { name: "Collapse info", exact: true }).click();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: `/tmp/savebite-shop-${width}.png`, fullPage: true });

    const dealsTab = page.getByRole("tab", { name: "Flash deals (18)", exact: true });
    const reviewsTab = page.getByRole("tab", { name: "Reviews (12)", exact: true });
    await dealsTab.focus();
    await page.keyboard.press("ArrowRight");
    await expect(reviewsTab).toHaveAttribute("aria-selected", "true");
    await expect(reviewsTab).toBeFocused();
    const reviews = page.getByRole("region", { name: "Customer reviews", exact: true });
    await expect(reviews.getByRole("article")).toHaveCount(10);
    await reviews.screenshot({
      path: `/tmp/savebite-shop-reviews-${width}.png`,
      style: "header.sticky { visibility: hidden; }",
    });
    await page.keyboard.press("Home");
    await expect(dealsTab).toHaveAttribute("aria-selected", "true");
    await expect(cards).toHaveCount(8);
    await page.getByRole("button", { name: "Page 3", exact: true }).click();
    await expect(cards).toHaveCount(2);
    await page.getByRole("button", { name: "Page 1", exact: true }).click();
    await expect(cards).toHaveCount(8);
    await profile.getByRole("link", { name: /3\.8/ }).click();
    await expect(reviewsTab).toHaveAttribute("aria-selected", "true");
    await expect(page).toHaveURL(/#store-content$/);
    await dealsTab.click();
    await cards.nth(1).click();
    await expect(page).toHaveURL(/\/flash-deals\/00000000-0000-4000-8000-000000000002$/);
    await expect(page.getByRole("button", { name: /Add to cart/ })).toBeEnabled();
    expect(errors).toEqual([]);
  });
}

test("store, deals and review request failures recover independently", async ({ page }) => {
  await page.goto(`${shop}?role=user&mock=error`);
  await expect(page.getByText("Couldn't load store", { exact: true })).toBeVisible({
    timeout: 30000,
  });
  await page.evaluate(() => history.replaceState(null, "", location.pathname + "?role=user"));
  await page.getByRole("button", { name: "Try again", exact: true }).click();
  const cards = page.locator('a[aria-label^="View deal:"]');
  await expect(cards).toHaveCount(8);
  await expect(page.getByRole("tab", { name: "Reviews (12)", exact: true })).toBeVisible();
  await page.evaluate(() =>
    history.replaceState(null, "", location.pathname + "?role=user&mock=error"),
  );
  await page.getByRole("button", { name: "Refresh store deals", exact: true }).click();
  await expect(page.getByText("Couldn't load store deals", { exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Bếp Xanh Sài Gòn", exact: true })).toBeVisible();
  await page.evaluate(() => history.replaceState(null, "", location.pathname + "?role=user"));
  await page.getByRole("button", { name: "Try again", exact: true }).click();
  await expect(cards).toHaveCount(8);
  await page.getByRole("tab", { name: "Reviews (12)", exact: true }).click();
  const reviews = page.getByRole("region", { name: "Customer reviews", exact: true });
  await expect(reviews.getByRole("article")).toHaveCount(10);
  await page.evaluate(() =>
    history.replaceState(null, "", location.pathname + "?role=user&mock=error"),
  );
  await reviews.getByRole("button", { name: "Refresh store reviews", exact: true }).click();
  await expect(reviews.getByText("Couldn't load customer reviews", { exact: true })).toBeVisible();
  await page.getByRole("tab", { name: "Flash deals (18)", exact: true }).click();
  await expect(cards).toHaveCount(8);
  await page.getByRole("tab", { name: "Reviews (12)", exact: true }).click();
  await page.evaluate(() => history.replaceState(null, "", location.pathname + "?role=user"));
  await reviews.getByRole("button", { name: "Try again", exact: true }).click();
  await expect(reviews.getByRole("article")).toHaveCount(10);
});

test("slow and empty storefronts preserve all sections and discovery actions", async ({ page }) => {
  await page.goto(`${shop}?role=user&mock=slow`);
  await expect(page.getByRole("status", { name: "Loading store", exact: true })).toBeVisible();
  await page.screenshot({ path: "/tmp/savebite-shop-loading.png", fullPage: true });
  await expect(page.locator('a[aria-label^="View deal:"]')).toHaveCount(8);
  await page.goto(`${shop}?role=user&mock=empty`);
  await expect(page.getByText("No flash deals at this store", { exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Explore nearby deals", exact: true })).toBeVisible();
  await page.getByRole("tab", { name: "Reviews (0)", exact: true }).click();
  await expect(page.getByText("No reviews yet", { exact: true })).toBeVisible();
  await page.screenshot({ path: "/tmp/savebite-shop-empty.png", fullPage: true });
});

test("closed store, missing metadata and long content remain readable on mobile", async ({
  page,
}) => {
  await page.setViewportSize({ width: 360, height: 1000 });
  await page.goto("/__ui?role=user");
  await page.waitForFunction(() =>
    Boolean((window as unknown as { __savebiteMock?: unknown }).__savebiteMock),
  );
  await page.evaluate(async () => {
    const { updateFeature } = await import("/src/mocks/db.ts");
    updateFeature("shops", "00000000-0000-4000-8000-000000000001", {
      name: "Cửa hàng thực phẩm ".repeat(12),
      description: null,
      addressLine: "",
      ward: null,
      district: null,
      city: null,
      logoUrl: "/missing-store-logo.jpg",
      coverImageUrl: "/missing-store-cover.jpg",
      openingTime: null,
      closingTime: null,
      isOpen: false,
    });
  });
  await page.goto(`${shop}?role=user`);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  const profile = page.getByRole("region", { name: "Store profile", exact: true });
  await expect(profile.getByText("Closed", { exact: true })).toBeVisible();
  await expect(profile.getByText("Image unavailable", { exact: true })).toBeVisible();
  await expect(profile.getByText("Pickup address not provided", { exact: true })).toBeVisible();
  await profile.getByRole("button", { name: "More store info", exact: true }).click();
  await expect(
    profile.getByText("The store hasn't shared a description yet.", { exact: true }),
  ).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: "/tmp/savebite-shop-partial.png", fullPage: true });
});

test("absent store data and legacy no-id route retain the unavailable state", async ({ page }) => {
  await page.goto("/__ui?role=user");
  await page.waitForFunction(() =>
    Boolean((window as unknown as { __savebiteMock?: unknown }).__savebiteMock),
  );
  await page.evaluate(async () => {
    const { db, save } = await import("/src/mocks/db.ts");
    db.responses["shops-shopApi_getShopProfile_1"] = { success: true, data: null };
    save();
  });
  await page.goto(`${shop}?role=user`);
  await expect(page.getByText("Store not found", { exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Browse nearby deals", exact: true })).toBeVisible();
  await page.goto("/shop-profile?role=user");
  await expect(page.getByText("Store not found", { exact: true })).toBeVisible();
});
