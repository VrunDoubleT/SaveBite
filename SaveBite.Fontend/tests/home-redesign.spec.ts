import { test, expect } from "@playwright/test";

test.beforeEach(async ({ page }) => {
  await page.route("https://unpkg.com/**", (route) => route.abort());
  await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
});

for (const width of [360, 768, 1280]) {
  test(`home discovery works at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 1000 });
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));
    await page.goto("/?role=user");
    const deals = page.locator('a[aria-label^="View deal:"]');
    await expect(deals.first()).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: `/tmp/savebite-home-${width}.png`, fullPage: true });

    if (width < 1024) await page.getByRole("button", { name: "Filter deals" }).click();
    const filters = page.getByRole("region", { name: "Deal filters" });
    const category = filters.getByRole("checkbox", { name: "Bánh mì" });
    await category.check();
    await expect(category).toBeChecked();
    await filters.getByRole("button", { name: "Reset", exact: true }).click();
    await expect(category).not.toBeChecked();
    await expect(page.getByText("Filters reset", { exact: true })).toBeVisible();
    if (width < 1024) await page.getByRole("button", { name: "Hide filters" }).click();

    await page.getByRole("searchbox", { name: "Search flash deals" }).fill("DOES_NOT_EXIST");
    await expect(page.getByText("No matching deals", { exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Clear search", exact: true }).last().click();
    await expect(deals.first()).toBeVisible();
    await page.getByRole("button", { name: "Use GPS", exact: true }).click();
    await expect(page.getByRole("button", { name: "Use GPS", exact: true })).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    await page.getByRole("button", { name: "Saved address", exact: true }).click();
    await expect(page.getByRole("button", { name: "Saved address", exact: true })).toHaveAttribute(
      "aria-pressed",
      "true",
    );

    const map = page.locator("summary");
    await map.focus();
    await page.keyboard.press("Enter");
    await expect(page.getByRole("heading", { name: "Nearby stores", exact: true })).toBeVisible();
    await expect(page.getByText("Map unavailable", { exact: true })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.screenshot({ path: `/tmp/savebite-home-map-${width}.png`, fullPage: true });
    await map.click();
    await expect(page.getByRole("heading", { name: "Nearby stores", exact: true })).toHaveCount(0);

    await page
      .getByRole("button", { name: "Load more deals", exact: true })
      .scrollIntoViewIfNeeded();
    await expect(deals).toHaveCount(18);
    const target = await deals.first().getAttribute("href");
    await deals.first().click();
    await expect(page).toHaveURL(new RegExp(`${target}$`));
    expect(errors).toEqual([]);
  });
}

test("empty and error states recover through their visible actions", async ({ page }) => {
  await page.goto("/?role=user&mock=empty");
  await expect(page.getByText("No deals nearby yet", { exact: true })).toBeVisible();
  await page.screenshot({ path: "/tmp/savebite-home-empty.png", fullPage: true });
  await page.evaluate(() => history.replaceState(null, "", "/?role=user"));
  await page.getByRole("button", { name: "Refresh deals", exact: true }).last().click();
  await expect(page.locator('a[aria-label^="View deal:"]').first()).toBeVisible();

  await page.goto("/?role=user&mock=error");
  await expect(page.getByText("Couldn't load flash deals", { exact: true })).toBeVisible();
  await page.screenshot({ path: "/tmp/savebite-home-error.png", fullPage: true });
  await page.evaluate(() => history.replaceState(null, "", "/?role=user"));
  await page.getByRole("button", { name: "Try again", exact: true }).click();
  await expect(page.locator('a[aria-label^="View deal:"]').first()).toBeVisible();
});

test("slow responses show layout skeletons before the deal list", async ({ page }) => {
  await page.goto("/?role=user&mock=slow");
  await expect(page.getByRole("status", { name: "Loading flash deals" })).toBeVisible();
  await page.screenshot({ path: "/tmp/savebite-home-loading.png", fullPage: true });
  await expect(page.locator('a[aria-label^="View deal:"]').first()).toBeVisible();
});

test("guest can recover from denied location and discover deals", async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 900 });
  await page.goto("/?role=guest");
  await expect(page.getByRole("button", { name: "Use my location", exact: true })).toBeVisible();
  await page.evaluate(() => {
    const original = navigator.geolocation.getCurrentPosition;
    navigator.geolocation.getCurrentPosition = (_success, error) => {
      navigator.geolocation.getCurrentPosition = original;
      error?.({
        code: 1,
        message: "Denied",
        PERMISSION_DENIED: 1,
        POSITION_UNAVAILABLE: 2,
        TIMEOUT: 3,
      });
    };
  });
  await page.getByRole("button", { name: "Use my location", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("Location permission is blocked");
  await page.screenshot({ path: "/tmp/savebite-home-location-denied.png", fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole("button", { name: "Use my location", exact: true }).click();
  await expect(page.locator('a[aria-label^="View deal:"]').first()).toBeVisible();
});

test("long names and unavailable product photos remain readable on mobile", async ({ page }) => {
  await page.setViewportSize({ width: 360, height: 1000 });
  await page.goto("/?role=user");
  await expect(page.locator('a[aria-label^="View deal:"]').first()).toBeVisible();
  const name = await page.evaluate(async () => {
    const { core } = await import("/src/mocks/coreState.ts");
    const name = "Bánh mì rau củ với phần mô tả rất dài dành cho bữa ăn gia đình — ".repeat(5);
    core.deals[0].productName = name;
    core.deals[0].shopName = "Cửa hàng với tên dài ".repeat(10);
    core.deals[0].productImageUrl = undefined;
    core.deals[0].distanceInKm = undefined;
    core.deals[1].productImageUrl = "/missing-product-photo.jpg";
    return name;
  });
  await page.getByRole("button", { name: "Refresh deals", exact: true }).click();
  await expect(page.getByText("Image unavailable", { exact: true })).toHaveCount(2);
  await expect(page.getByRole("link", { name, exact: true })).toHaveAttribute("title", name);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: "/tmp/savebite-home-partial.png", fullPage: true });
});
