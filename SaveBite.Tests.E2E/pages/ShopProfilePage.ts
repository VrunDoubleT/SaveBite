import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class ShopProfilePage extends BasePage {
  readonly followButton: Locator;
  readonly unfollowButton: Locator;
  readonly notificationStatusText: Locator;

  constructor(page: Page) {
    super(page);
    this.followButton = page.getByRole("button", { name: "Follow store", exact: true });
    this.unfollowButton = page.getByRole("button", { name: "Unfollow store", exact: true });
    this.notificationStatusText = page.getByRole("status").filter({
      hasText: /Deal notifications (on|off)/i,
    });
  }

  async navigate(shopId: string): Promise<void> {
    await this.goto(`/shops/${shopId}`);
    await this.page
      .locator('button:has-text("Follow store"), button:has-text("Unfollow store")')
      .first()
      .waitFor({ state: "visible", timeout: 30_000 });
  }

  toastStatusWithText(text: string | RegExp): Locator {
    return this.page
      .locator('section[aria-label="Notifications"] [role="status"]')
      .filter({ hasText: text });
  }
}
