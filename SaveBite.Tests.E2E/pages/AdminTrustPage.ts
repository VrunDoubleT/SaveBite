import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminTrustPage extends BasePage {
  readonly pageHeading: Locator;
  readonly tabLevels: Locator;
  readonly tabPenaltyRules: Locator;
  readonly tabScoreRules: Locator;
  readonly tabEarningRules: Locator;
  readonly addLevelBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Trust Score Configuration/i }).or(page.getByRole("heading")).first();
    this.tabLevels = page.getByRole("tab", { name: /Levels/i }).or(page.getByText(/^Levels/i));
    this.tabPenaltyRules = page.getByRole("tab", { name: /Penalty rules/i }).or(page.getByText(/^Penalty rules/i));
    this.tabScoreRules = page.getByRole("tab", { name: /Score rules/i }).or(page.getByText(/^Score rules/i));
    this.tabEarningRules = page.getByRole("tab", { name: /Earning rules/i }).or(page.getByText(/^Earning rules/i));
    this.addLevelBtn = page.getByRole("button", { name: /Add trust level|Add level/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/trust");
  }
}
