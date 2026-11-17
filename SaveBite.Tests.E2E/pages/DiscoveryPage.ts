import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class DiscoveryPage extends BasePage {
  readonly locationWelcomeHeading: Locator;
  readonly useLocationButton: Locator;
  readonly useSavedAddressButton: Locator;
  readonly flashDealsHeading: Locator;
  readonly searchInput: Locator;
  readonly clearSearchButton: Locator;
  readonly refreshButton: Locator;
  readonly dealCards: Locator;
  readonly emptyDealsMessage: Locator;

  constructor(page: Page) {
    super(page);
    this.locationWelcomeHeading = page.locator("#location-welcome-heading");
    this.useLocationButton = page.getByRole("button", { name: /Use my location|Try my location again/i });
    this.useSavedAddressButton = page.getByRole("button", { name: /Use saved address/i });
    this.flashDealsHeading = page.locator("#flash-deals-heading");
    this.searchInput = page.locator("#flash-deal-search");
    this.clearSearchButton = page.getByRole("button", { name: "Clear search" }).first();
    this.refreshButton = page.getByRole("button", { name: "Refresh deals" });
    this.dealCards = page.locator('section, article, div[class*="group flex h-full"]').filter({
      has: page.locator('a[href^="/flash-deals/"]'),
    });
    this.emptyDealsMessage = page.getByText(/No deals nearby yet|No matching deals|No flash deals found/i).first();
  }

  async navigate(): Promise<void> {
    await this.goto("/");
    // If the location welcome gate is displayed, bypass it by clicking use location / saved address
    const isWelcomeVisible = await this.locationWelcomeHeading.isVisible({ timeout: 3000 }).catch(() => false);
    if (isWelcomeVisible) {
      if (await this.useSavedAddressButton.isVisible().catch(() => false)) {
        await this.useSavedAddressButton.click();
      } else if (await this.useLocationButton.isVisible().catch(() => false)) {
        await this.useLocationButton.click().catch(() => {});
      }
    }

    // Wait for the main Flash Deals section or empty state to appear
    await this.flashDealsHeading
      .or(this.emptyDealsMessage)
      .first()
      .waitFor({ state: "visible", timeout: 30_000 });

    // Wait for skeleton loader to detach if currently fetching
    const loader = this.page.locator('[aria-label="Loading flash deals"]');
    if (await loader.isVisible({ timeout: 1500 }).catch(() => false)) {
      await loader.waitFor({ state: "detached", timeout: 30_000 }).catch(() => {});
    }
  }

  async search(keyword: string): Promise<void> {
    await this.searchInput.waitFor({ state: "visible", timeout: 10_000 });
    await this.searchInput.fill(keyword);
    // Debounce wait
    await this.page.waitForTimeout(600);
  }

  async clearSearch(): Promise<void> {
    if (await this.clearSearchButton.isVisible().catch(() => false)) {
      await this.clearSearchButton.click();
    } else {
      await this.searchInput.fill("");
    }
    await this.page.waitForTimeout(400);
  }

  async selectCategory(categoryName: string): Promise<void> {
    const categoryCheckbox = this.page.getByLabel(categoryName, { exact: false });
    if (await categoryCheckbox.isVisible().catch(() => false)) {
      await categoryCheckbox.check();
      await this.page.waitForTimeout(600);
    }
  }

  dealCardByTitle(title: string): Locator {
    return this.dealCards.filter({ hasText: title });
  }
}
