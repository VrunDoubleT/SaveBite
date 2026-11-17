import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class PlatformFeesPage extends BasePage {
  readonly pageHeading: Locator;
  readonly yearFilter: Locator;
  readonly monthFilter: Locator;
  readonly statusFilter: Locator;
  readonly statementsTable: Locator;
  readonly payFeeBtn: Locator;
  readonly currentRateCard: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Platform Fees|Fee Statements/i }).first();
    this.yearFilter = page.locator("select").first();
    this.monthFilter = page.locator("select").nth(1);
    this.statusFilter = page.locator("select").nth(2);
    this.statementsTable = page.locator("table");
    this.payFeeBtn = page.getByRole("button", { name: /Pay platform fee|Pay with payOS/i });
    this.currentRateCard = page.getByText(/Current rate|Fee rate/i);
  }

  async navigate(): Promise<void> {
    await this.goto("/store-owner/platform-fees");
  }
}
