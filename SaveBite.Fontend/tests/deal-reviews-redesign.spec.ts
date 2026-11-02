import { test, expect } from "@playwright/test";
const detail = "/flash-deals/00000000-0000-4000-8000-000000000002?role=user";

test.beforeEach(async ({ page }) => {
  await page.route("https://unpkg.com/**", (route) => route.abort());
  await page.route("https://fonts.googleapis.com/**", (route) => route.abort());
});
for (const width of [360, 768, 1280]) {
  test(`complete review section at ${width}px`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));
    await page.setViewportSize({ width, height: 1000 });
    await page.goto(detail);
    const reviews = page.getByRole("region", { name: "Customer reviews", exact: true });
    await expect(reviews.getByRole("button", { name: "All (12)", exact: true })).toBeVisible();
    await expect(reviews.getByRole("article")).toHaveCount(4);
    await expect(
      page.getByRole("link", { name: "3.8 Store rating · 12 reviews", exact: true }),
    ).toHaveAttribute("href", "#reviews");
    await expect(reviews.getByText("3.8", { exact: false }).first()).toBeVisible();
    await expect(reviews.getByText("Store response", { exact: true }).first()).toBeVisible();
    await reviews.scrollIntoViewIfNeeded();
    await reviews.screenshot({
      path: `/tmp/savebite-reviews-${width}.png`,
      style: "header.sticky { visibility: hidden; }",
    });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );

    const photo = reviews.getByRole("button", {
      name: "View review photo 1 by Nguyễn Minh Anh",
      exact: true,
    });
    await photo.focus();
    await page.keyboard.press("Enter");
    const dialog = page.getByRole("dialog", { name: "Review photo", exact: true });
    await expect(dialog).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(dialog).toHaveCount(0);
    await expect(photo).toBeFocused();
    await reviews.getByRole("button", { name: "Photos on this page (3)", exact: true }).click();
    await expect(reviews.getByRole("article")).toHaveCount(3);
    await reviews.getByRole("button", { name: "All (12)", exact: true }).click();
    await expect(reviews.getByRole("article")).toHaveCount(4);
    const next = page.waitForRequest(
      (request) =>
        request.url().includes("/reviews?") &&
        new URL(request.url()).searchParams.get("page") === "2",
    );
    await reviews.getByRole("button", { name: "Next page", exact: true }).click();
    await next;
    await expect(reviews.getByRole("heading", { name: "Võ Tuấn Kiệt", exact: true })).toBeVisible();
    const filtered = page.waitForRequest(
      (request) =>
        request.url().includes("/reviews?") &&
        new URL(request.url()).searchParams.get("rating") === "5",
    );
    await reviews.getByRole("button", { name: "5 stars (5)", exact: true }).click();
    expect(new URL((await filtered).url()).searchParams.get("page")).toBe("1");
    await expect(reviews.getByRole("button", { name: "5 stars (5)", exact: true })).toBeFocused();
    await expect(reviews.getByRole("article")).toHaveCount(4);
    for (const article of await reviews.getByRole("article").all())
      await expect(
        article.getByRole("img", { name: "5 out of 5 stars", exact: true }),
      ).toBeVisible();
    expect(errors).toEqual([]);
  });
}

test("reviews have independent loading, retry and empty states", async ({ page }) => {
  await page.goto(detail + "&mock=slow");
  await expect(
    page.getByRole("status", { name: "Loading customer reviews", exact: true }),
  ).toBeVisible({ timeout: 15000 });
  const reviews = page.getByRole("region", { name: "Customer reviews", exact: true });
  await expect(reviews.getByRole("button", { name: "All (12)", exact: true })).toBeVisible();
  await page.evaluate(() =>
    history.replaceState(null, "", location.pathname + "?role=user&mock=error"),
  );
  await reviews.getByRole("button", { name: "Refresh reviews", exact: true }).click();
  await expect(reviews.getByText("Couldn't load customer reviews", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: /Add to cart/ })).toBeEnabled();
  await page.evaluate(() => history.replaceState(null, "", location.pathname + "?role=user"));
  await reviews.getByRole("button", { name: "Try again", exact: true }).click();
  await expect(reviews.getByRole("article")).toHaveCount(4);
  await page.goto(detail + "&mock=empty");
  await expect(reviews.getByText("No reviews yet", { exact: true })).toBeVisible();
  await expect(reviews.getByRole("button", { name: "All (0)", exact: true })).toBeVisible();
  await expect(reviews.getByRole("link", { name: "Browse deals", exact: true })).toBeVisible();
});

test("shared store review component preserves shop-page integration", async ({ page }) => {
  await page.goto("/shops/00000000-0000-4000-8000-000000000001?role=user");
  await page.getByRole("button", { name: /^Reviews/ }).click();
  await expect(page.getByRole("button", { name: "All (12)", exact: true })).toBeVisible();
  await expect(page.getByRole("article")).toHaveCount(10);
  await page.getByRole("button", { name: "1 star (1)", exact: true }).click();
  await expect(page.getByRole("article")).toHaveCount(1);
});

test("review demo upgrade retains unrelated preview edits", async ({ page }) => {
  await page.goto(detail);
  await expect(page.getByRole("button", { name: "All (12)", exact: true })).toBeVisible();
  await page.evaluate(async () => {
    const { db } = await import("/src/mocks/db.ts");
    const key = "savebite-mock-db-v1";
    // Persist a legacy-style preview snapshot without the review seed marker.
    const state = structuredClone(db);
    state.responses["preview-preservation-check"] = { data: "Keep my preview" };
    delete state.reviewsSeedVersion;
    sessionStorage.setItem(key, JSON.stringify(state));
  });
  await page.reload();
  await expect(page.getByRole("button", { name: "All (12)", exact: true })).toBeVisible();
  const retained = await page.evaluate(async () => {
    const { db } = await import("/src/mocks/db.ts");
    return db.responses["preview-preservation-check"];
  });
  expect(retained).toEqual({ data: "Keep my preview" });
});
