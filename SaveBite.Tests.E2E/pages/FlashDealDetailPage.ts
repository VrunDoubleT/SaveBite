import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class FlashDealDetailPage extends BasePage {
  readonly dealHeading: Locator;
  readonly shopNameLink: Locator;
  readonly visitStoreButton: Locator;
  readonly followStoreButton: Locator;
  readonly unfollowStoreButton: Locator;
  readonly pickupAddressText: Locator;
  readonly countdownStatus: Locator;
  readonly storeReviewsSection: Locator;
  readonly addToCartButton: Locator;
  readonly viewCartLink: Locator;
  readonly quantityInput: Locator;
  readonly increaseQuantityButton: Locator;
  readonly decreaseQuantityButton: Locator;
  readonly addedToCartNotice: Locator;

  constructor(page: Page) {
    super(page);
    this.dealHeading = page.locator("h1");
    this.shopNameLink = page.locator('h2 a[href^="/shops/"]');
    this.visitStoreButton = page.getByRole("link", { name: "Visit Store" });
    this.followStoreButton = page.getByRole("button", { name: "Follow store", exact: true });
    this.unfollowStoreButton = page.getByRole("button", { name: "Unfollow store", exact: true });
    this.pickupAddressText = page.getByText(/Your pickup store/i).or(page.locator('span:has-text("km away")'));
    this.countdownStatus = page.locator('dl:has-text("Order before"), dl:has-text("Store closes")');
    this.storeReviewsSection = page.locator('#store-reviews, section[aria-label*="reviews" i]');
    this.addToCartButton = page.getByRole("button", { name: /Add to cart/i });
    this.viewCartLink = page.getByRole("link", { name: "View cart" });
    this.quantityInput = page
      .locator('input[aria-label="Selected quantity"]')
      .or(page.getByLabel("Selected quantity"));
    this.increaseQuantityButton = page.getByRole("button", { name: /Increase quantity/i });
    this.decreaseQuantityButton = page.getByRole("button", { name: /Decrease quantity/i });
    this.addedToCartNotice = page.getByText("Added to your cart.");
  }

  async navigate(dealId: string): Promise<void> {
    await this.goto(`/flash-deals/${dealId}`);
    await this.dealHeading.waitFor({ state: "visible", timeout: 30_000 });

    // If an authenticated session exists, ensure auth store initialization finishes before actions
    const hasToken = await this.page
      .evaluate(() => Boolean(localStorage.getItem("savebite_access_token")))
      .catch(() => false);
    if (hasToken) {
      await this.page
        .locator('header a[href="/cart"]')
        .waitFor({ state: "visible", timeout: 10_000 })
        .catch(() => {});
    }
  }

  toastStatusWithText(text: string | RegExp): Locator {
    return this.page
      .locator('section[aria-label="Notifications"] [role="status"]')
      .filter({ hasText: text });
  }
}
