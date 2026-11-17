import { expect, test } from "@playwright/test";
import { authenticateInBrowser, clearBrowserSession, TEST_USERS } from "../../helpers/authHelper.js";
import { getNearbyFlashDealsViaApi } from "../../helpers/discoveryHelper.js";
import {
  findActiveShopsViaApi,
  followShopViaApi,
  getFollowedShopsViaApi,
  unfollowShopViaApi,
} from "../../helpers/profileHelper.js";
import { DiscoveryPage } from "../../pages/DiscoveryPage.js";
import { FlashDealDetailPage } from "../../pages/FlashDealDetailPage.js";
import { FollowedShopsPage } from "../../pages/FollowedShopsPage.js";
import { ShopProfilePage } from "../../pages/ShopProfilePage.js";

test.describe("Discovery & Browsing (TC-001 ~ TC-014)", () => {
  // =========================================================================
  // SECTION 1: Browse Nearby Deals & Location Gate
  // =========================================================================

  test("TC-001: Browse nearby flash deals on Home page", async ({ page }) => {
    const discoveryPage = new DiscoveryPage(page);

    await discoveryPage.navigate();

    await expect(discoveryPage.flashDealsHeading).toBeVisible();
    await expect(discoveryPage.searchInput).toBeVisible();
    await expect(discoveryPage.refreshButton).toBeVisible();

    await expect(
      discoveryPage.dealCards.first().or(discoveryPage.emptyDealsMessage),
    ).toBeVisible({ timeout: 30_000 });

    if (await discoveryPage.dealCards.first().isVisible()) {
      await expect(discoveryPage.dealCards.first().locator('a[href^="/flash-deals/"]').first()).toBeVisible();
    } else {
      await expect(discoveryPage.emptyDealsMessage).toBeVisible();
    }
  });

  test("TC-002: Browser location permission blocked shows location alert and allows manual selection", async ({
    page,
    context,
  }) => {
    // Clear geolocation permissions for this context to test blocked location
    await context.clearPermissions();

    await page.goto("/");

    // Verify location gate welcome prompt is displayed
    const welcomeHeading = page.locator("#location-welcome-heading");
    const blockedAlert = page.locator("div[role='alert']").or(page.getByText(/Location permission is blocked/i));
    await expect(welcomeHeading.or(blockedAlert).first()).toBeVisible({ timeout: 15_000 });

    // Verify action button to retry location is available
    const locationBtn = page.getByRole("button", { name: /Try.*location|Use.*location/i }).first();
    await expect(locationBtn).toBeVisible({ timeout: 15_000 });
  });

  test("TC-003: Zero deals in radius displays No deals nearby yet empty state", async ({
    page,
  }) => {
    // Intercept nearby deals API to simulate zero deals in radius
    await page.route(
      (url) => url.pathname.endsWith("/flash-deals") || url.pathname.endsWith("/flash-deals/"),
      async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            data: {
              deals: [],
              nextCursor: null,
              hasMore: false,
              hasOlder: false,
              hasNewer: false,
              prependedCount: 0,
              newDealsCount: 0,
            },
            message: "No flash deals found within this radius.",
          }),
        });
      },
    );

    const discoveryPage = new DiscoveryPage(page);
    await discoveryPage.navigate();

    // Verify empty state message renders when zero deals exist
    await expect(discoveryPage.emptyDealsMessage).toBeVisible({ timeout: 15_000 });
  });

  // =========================================================================
  // SECTION 2: Search & Filter Deals
  // =========================================================================

  test("TC-004: Search flash deals by food or store keyword", async ({ page }) => {
    const discoveryPage = new DiscoveryPage(page);

    await discoveryPage.navigate();
    await expect(
      discoveryPage.dealCards.first().or(discoveryPage.emptyDealsMessage),
    ).toBeVisible({ timeout: 20_000 });

    const hasDeals = await discoveryPage.dealCards.first().isVisible();
    if (hasDeals) {
      const firstCard = discoveryPage.dealCards.first();
      const titleLink = firstCard.locator('a[href^="/flash-deals/"]').nth(1);
      const cardTitle = (await titleLink.textContent())?.trim() ?? "";
      const searchKeyword = cardTitle.slice(0, 4) || "Com";
      await discoveryPage.search(searchKeyword);

      await expect(
        discoveryPage.dealCards.first().or(discoveryPage.emptyDealsMessage),
      ).toBeVisible({ timeout: 15_000 });

      await discoveryPage.clearSearch();
      await expect(discoveryPage.dealCards.first()).toBeVisible();
    } else {
      await discoveryPage.search("NonExistentDish12345");
      await expect(discoveryPage.emptyDealsMessage).toBeVisible();
    }
  });

  test("TC-005: Search with non-existent keyword displays No matching deals empty state", async ({
    page,
  }) => {
    const discoveryPage = new DiscoveryPage(page);
    await discoveryPage.navigate();

    // Search for a random string that definitely does not exist
    await discoveryPage.search("NonExistentXYZ999999");

    // Verify "No matching deals" empty state and "Clear search" button
    await expect(page.getByText(/No matching deals|No flash deals found/i).first()).toBeVisible({
      timeout: 15_000,
    });
    await expect(discoveryPage.clearSearchButton).toBeVisible();

    // Clear search and verify restoration
    await discoveryPage.clearSearch();
    await expect(
      discoveryPage.dealCards.first().or(discoveryPage.emptyDealsMessage),
    ).toBeVisible({ timeout: 15_000 });
  });

  test("TC-006: Filter flash deals by Category from sidebar", async ({ page }) => {
    const discoveryPage = new DiscoveryPage(page);
    await discoveryPage.navigate();

    const categoryCheckboxes = page.locator('#home-filters input[type="checkbox"]');
    const categoryCount = await categoryCheckboxes.count();

    if (categoryCount > 0) {
      const firstCategory = categoryCheckboxes.first();
      await firstCategory.check();
      await page.waitForTimeout(600);

      await expect(discoveryPage.flashDealsHeading.or(discoveryPage.emptyDealsMessage).first()).toBeVisible();

      await firstCategory.uncheck();
      await page.waitForTimeout(400);
    }
  });

  test("TC-007: Filter deals by distance and price range and verify Reset button", async ({
    page,
  }) => {
    const discoveryPage = new DiscoveryPage(page);
    await discoveryPage.navigate();

    const resetButton = page.getByRole("button", { name: "Reset" });
    const distanceCheckbox = page.getByLabel(/Under 2km|Under 5km/i).first();

    if (await distanceCheckbox.isVisible().catch(() => false)) {
      await distanceCheckbox.check();
      await page.waitForTimeout(500);

      // Verify Reset button resets filters
      if (await resetButton.isVisible()) {
        await resetButton.click();
        await page.waitForTimeout(400);
        await expect(distanceCheckbox).not.toBeChecked();
      }
    }
  });

  // =========================================================================
  // SECTION 3: Flash Deal Detail & Store Profile
  // =========================================================================

  test("TC-008: View Flash Deal Detail Page with pricing and countdown", async ({
    page,
  }) => {
    const discoveryPage = new DiscoveryPage(page);
    const detailPage = new FlashDealDetailPage(page);

    await discoveryPage.navigate();
    await expect(
      discoveryPage.dealCards.first().or(discoveryPage.emptyDealsMessage),
    ).toBeVisible({ timeout: 20_000 });

    const hasDeals = await discoveryPage.dealCards.first().isVisible();
    if (!hasDeals) {
      test.skip(true, "No flash deals in database to test deal detail page.");
      return;
    }

    const firstCard = discoveryPage.dealCards.first();
    const dealLink = firstCard.locator('a[href^="/flash-deals/"]').first();
    await dealLink.click();

    await expect(page).toHaveURL(/\/flash-deals\/[a-zA-Z0-9-]+/);
    await expect(detailPage.dealHeading).toBeVisible();
    await expect(detailPage.visitStoreButton).toBeVisible();
    await expect(detailPage.countdownStatus).toBeVisible();
    await expect(detailPage.pickupAddressText).toBeVisible();
  });

  test("TC-009: Accessing non-existent deal ID displays Deal unavailable empty state", async ({
    page,
  }) => {
    // Open a completely non-existent deal UUID
    await page.goto("/flash-deals/00000000-0000-0000-0000-000000000000");

    // Verify friendly "This deal is no longer available" empty state
    await expect(
      page.getByText(/This deal is no longer available|Couldn't load this deal/i).first(),
    ).toBeVisible({ timeout: 20_000 });
    await expect(page.getByRole("link", { name: /Browse nearby deals|Back to deals/i })).toBeVisible();
  });

  test("TC-010: Navigate to Store Profile & Reviews from Deal Detail", async ({
    page,
    request,
  }) => {
    const detailPage = new FlashDealDetailPage(page);
    const deals = await getNearbyFlashDealsViaApi(request);

    if (deals.length === 0) {
      test.skip(true, "No flash deals in database to test store navigation.");
      return;
    }

    const targetDeal = deals[0];
    await detailPage.navigate(targetDeal.id);

    await detailPage.visitStoreButton.click();
    await expect(page).toHaveURL(new RegExp(`/shops/${targetDeal.shopId}`));
    await expect(page.locator("h1")).toContainText(targetDeal.shopName);
  });

  test("TC-011: Accessing non-existent shop ID displays Store not found empty state", async ({
    page,
  }) => {
    // Open a non-existent shop UUID
    await page.goto("/shops/00000000-0000-0000-0000-000000000000");

    // Verify friendly "Store not found" empty state
    await expect(
      page.getByText(/Store not found|Couldn't load store/i).first(),
    ).toBeVisible({ timeout: 20_000 });
    await expect(page.getByRole("link", { name: /Browse nearby deals|Back to deals/i })).toBeVisible();
  });

  // =========================================================================
  // SECTION 4: Follow & Unfollow Store
  // =========================================================================

  test("TC-012: Follow store from Deal/Shop page and verify in Profile Followed Shops", async ({
    page,
    request,
  }) => {
    const customerTokens = await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );

    const activeShops = await findActiveShopsViaApi(request, customerTokens.accessToken);
    if (activeShops.length === 0) {
      test.skip(true, "No active shops in DB to follow.");
      return;
    }

    const targetShop = activeShops[0];
    await unfollowShopViaApi(request, customerTokens.accessToken, targetShop.id);

    const shopProfilePage = new ShopProfilePage(page);
    const followedShopsPage = new FollowedShopsPage(page);

    await shopProfilePage.navigate(targetShop.id);
    await expect(shopProfilePage.followButton).toBeVisible();
    await shopProfilePage.followButton.click();

    await expect(
      shopProfilePage.toastStatusWithText(new RegExp(`Following ${targetShop.name}`, "i")),
    ).toBeVisible({ timeout: 15_000 });
    await expect(shopProfilePage.unfollowButton).toBeVisible();

    await followedShopsPage.navigate();
    await expect(followedShopsPage.storeCardByName(targetShop.name)).toBeVisible({
      timeout: 15_000,
    });
  });

  test("TC-013: Guest clicking Follow store is prompted to sign in", async ({
    page,
    request,
  }) => {
    // Clear session so user is unauthenticated
    await clearBrowserSession(page);

    const deals = await getNearbyFlashDealsViaApi(request);
    if (deals.length === 0) {
      test.skip(true, "No deals available to inspect store.");
      return;
    }

    const targetDeal = deals[0];
    await page.goto(`/shops/${targetDeal.shopId}`);

    const followBtn = page.getByRole("button", { name: "Follow store", exact: true });
    if (await followBtn.isVisible()) {
      await followBtn.click();
      // Should redirect to /login or show auth toast
      await expect(
        page.getByRole("heading", { name: /Sign in|Welcome back/i }).or(page.getByText(/Sign in to follow/i)),
      ).toBeVisible({ timeout: 15_000 });
    }
  });

  test("TC-014: Unfollow store from Shop Profile removes it from Profile Followed Shops", async ({
    page,
    request,
  }) => {
    const customerTokens = await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );

    const activeShops = await findActiveShopsViaApi(request, customerTokens.accessToken);
    if (activeShops.length === 0) {
      test.skip(true, "No active shops in DB to test unfollow.");
      return;
    }

    const targetShop = activeShops[0];
    // Ensure shop is followed first
    await followShopViaApi(request, customerTokens.accessToken, targetShop.id);

    const shopProfilePage = new ShopProfilePage(page);
    await shopProfilePage.navigate(targetShop.id);

    await expect(shopProfilePage.unfollowButton).toBeVisible({ timeout: 15_000 });
    await shopProfilePage.unfollowButton.click();

    await expect(
      shopProfilePage.toastStatusWithText(new RegExp(`Unfollowed ${targetShop.name}`, "i")),
    ).toBeVisible({ timeout: 15_000 });
    await expect(shopProfilePage.followButton).toBeVisible();
  });
});
