import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class FollowedShopsPage extends BasePage {
  readonly emptyStateHeading: Locator;
  readonly exploreDealsLink: Locator;
  readonly summaryText: Locator;
  readonly storeCards: Locator;

  constructor(page: Page) {
    super(page);
    this.emptyStateHeading = page.getByRole("heading", { name: "No followed stores yet" });
    this.exploreDealsLink = page.getByRole("link", { name: "Explore nearby deals" });
    this.summaryText = page.getByText(/\d+\s+followed\s+stores?/i);
    this.storeCards = page
      .locator("ul")
      .filter({ has: page.locator('a[href^="/shops/"]') })
      .first()
      .locator("> li");
  }

  async navigate(): Promise<void> {
    await this.goto("/account/followed-shops");
    await this.emptyStateHeading
      .or(this.summaryText)
      .first()
      .waitFor({ state: "visible", timeout: 30_000 });
  }

  storeCardByName(shopName: string): Locator {
    return this.storeCards.filter({
      has: this.page.getByRole("link", { name: shopName, exact: true }),
    });
  }

  unfollowButtonFor(shopName: string): Locator {
    return this.storeCardByName(shopName).getByRole("button", { name: "Unfollow store" });
  }

  notificationBellButtonFor(shopName: string): Locator {
    return this.storeCardByName(shopName).getByRole("button", {
      name: new RegExp(`Turn (on|off) deal notifications for ${shopName.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}`),
    });
  }

  toastStatusWithText(text: string | RegExp): Locator {
    return this.page
      .locator('section[aria-label="Notifications"] [role="status"]')
      .filter({ hasText: text });
  }
}
