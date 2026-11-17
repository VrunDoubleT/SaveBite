import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class CustomerTrustPage extends BasePage {
  readonly pageHeading: Locator;
  readonly tabOverview: Locator;
  readonly tabLevels: Locator;
  readonly tabHistory: Locator;
  readonly tabPenalties: Locator;
  readonly trustScoreValue: Locator;
  readonly currentLevelBadge: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Trust Score/i }).or(page.getByRole("heading")).first();
    this.tabOverview = page.getByRole("tab", { name: /Your membership|Overview/i }).or(page.getByText(/Your membership/i));
    this.tabLevels = page.getByRole("tab", { name: /Compare benefits|Levels/i }).or(page.getByText(/Compare benefits/i));
    this.tabHistory = page.getByRole("tab", { name: /Your points|History/i }).or(page.getByText(/Your points/i));
    this.tabPenalties = page.getByRole("tab", { name: /Recovery progress|Penalties/i }).or(page.getByText(/Recovery progress/i));
    this.trustScoreValue = page.getByText(/points/i);
    this.currentLevelBadge = page.getByText(/Level \d+/i);
  }

  async navigate(): Promise<void> {
    await this.goto("/account/trust-score");
  }
}
