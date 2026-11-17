import { expect, test } from "@playwright/test";
import { authenticateInBrowser, TEST_USERS } from "../../helpers/authHelper.js";
import {
  findActiveShopsViaApi,
  followShopViaApi,
  getFollowedShopsViaApi,
  unfollowShopViaApi,
} from "../../helpers/profileHelper.js";
import { FollowedShopsPage } from "../../pages/FollowedShopsPage.js";
import { ShopProfilePage } from "../../pages/ShopProfilePage.js";

test.describe("Profile & Address - Followed Shops (TC-013 ~ TC-016)", () => {
  test("TC-013: View followed stores list or empty state on /account/followed-shops", async ({
    page,
    request,
  }) => {
    const followedPage = new FollowedShopsPage(page);

    await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );
    await followedPage.navigate();

    const hasFollowed = await followedPage.summaryText.isVisible().catch(() => false);
    if (hasFollowed) {
      await expect(followedPage.storeCards.first()).toBeVisible();
    } else {
      await expect(followedPage.emptyStateHeading).toBeVisible();
      await expect(followedPage.exploreDealsLink).toBeVisible();
    }
  });

  test("TC-014: Follow a store from Store Profile page adds it to /account/followed-shops", async ({
    page,
    request,
  }) => {
    const shopPage = new ShopProfilePage(page);
    const followedPage = new FollowedShopsPage(page);
    const { accessToken } = await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );

    const shops = await findActiveShopsViaApi(request, accessToken);
    expect(shops.length).toBeGreaterThan(0);

    const initialFollowed = await getFollowedShopsViaApi(request, accessToken);
    const followedIds = new Set(initialFollowed.map((item) => item.shopId));
    const targetShop = shops.find((s) => !followedIds.has(s.id)) ?? shops[0];
    const wasInitiallyFollowed = followedIds.has(targetShop.id);

    try {
      if (wasInitiallyFollowed) {
        await unfollowShopViaApi(request, accessToken, targetShop.id);
      }

      await shopPage.navigate(targetShop.id);
      await shopPage.followButton.click();

      await expect(
        shopPage.toastStatusWithText(`Following ${targetShop.name}. Deal notifications are on.`),
      ).toBeVisible();
      await expect(shopPage.unfollowButton).toBeVisible();
      await expect(shopPage.notificationStatusText).toContainText("Deal notifications on");

      await followedPage.navigate();
      await expect(followedPage.storeCardByName(targetShop.name)).toBeVisible();
    } finally {
      if (wasInitiallyFollowed) {
        await followShopViaApi(request, accessToken, targetShop.id);
      } else {
        await unfollowShopViaApi(request, accessToken, targetShop.id);
      }
    }
  });

  test("TC-015: Toggle deal notifications bell on /account/followed-shops", async ({
    page,
    request,
  }) => {
    const followedPage = new FollowedShopsPage(page);
    const { accessToken } = await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );

    const shops = await findActiveShopsViaApi(request, accessToken);
    expect(shops.length).toBeGreaterThan(0);

    const initialFollowed = await getFollowedShopsViaApi(request, accessToken);
    const followedShop = initialFollowed[0];
    const targetShopId = followedShop?.shopId ?? shops[0].id;
    const targetShopName = followedShop?.shopName ?? shops[0].name;
    const createdTempFollow = !followedShop;

    try {
      if (createdTempFollow) {
        await followShopViaApi(request, accessToken, targetShopId);
      }

      await followedPage.navigate();
      const bell = followedPage.notificationBellButtonFor(targetShopName);
      await expect(bell).toBeVisible();

      const initiallyPressed = (await bell.getAttribute("aria-pressed")) === "true";
      await bell.click();

      await expect(
        followedPage.toastStatusWithText(
          initiallyPressed ? "Deal notifications turned off." : "Deal notifications enabled.",
        ),
      ).toBeVisible();
      await expect(bell).toHaveAttribute("aria-pressed", initiallyPressed ? "false" : "true");

      await bell.click();
      await expect(
        followedPage.toastStatusWithText(
          initiallyPressed ? "Deal notifications enabled." : "Deal notifications turned off.",
        ),
      ).toBeVisible();
      await expect(bell).toHaveAttribute("aria-pressed", initiallyPressed ? "true" : "false");
    } finally {
      if (createdTempFollow) {
        await unfollowShopViaApi(request, accessToken, targetShopId);
      }
    }
  });

  test("TC-016: Unfollow a store from /account/followed-shops removes it from the list", async ({
    page,
    request,
  }) => {
    const followedPage = new FollowedShopsPage(page);
    const { accessToken } = await authenticateInBrowser(
      page,
      request,
      TEST_USERS.customer.email,
      TEST_USERS.customer.password,
    );

    const shops = await findActiveShopsViaApi(request, accessToken);
    expect(shops.length).toBeGreaterThan(0);

    const initialFollowed = await getFollowedShopsViaApi(request, accessToken);
    const existingFollow = initialFollowed[0];
    const targetShopId = existingFollow?.shopId ?? shops[0].id;
    const targetShopName = existingFollow?.shopName ?? shops[0].name;
    const wasInitiallyFollowed = Boolean(existingFollow);

    try {
      if (!wasInitiallyFollowed) {
        await followShopViaApi(request, accessToken, targetShopId);
      }

      await followedPage.navigate();
      await expect(followedPage.storeCardByName(targetShopName)).toBeVisible();
      await followedPage.unfollowButtonFor(targetShopName).click();

      await expect(followedPage.toastStatusWithText(`Unfollowed ${targetShopName}.`)).toBeVisible();
      await expect(followedPage.storeCardByName(targetShopName)).toHaveCount(0);

      await page.reload({ waitUntil: "domcontentloaded" });
      await expect(followedPage.storeCardByName(targetShopName)).toHaveCount(0);
    } finally {
      if (wasInitiallyFollowed) {
        await followShopViaApi(request, accessToken, targetShopId);
      }
    }
  });
});
