import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminPlatformFeesPage extends BasePage {
  readonly pageHeading: Locator;
  readonly feeSchedulePanel: Locator;
  readonly rateInput: Locator;
  readonly reasonInput: Locator;
  readonly scheduleRateBtn: Locator;
  readonly statementsTable: Locator;
  readonly payOsSettingsLink: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Platform Fees Management|Platform Fees/i }).first();
    this.feeSchedulePanel = page.getByText(/Commission schedules|Fee schedules/i);
    this.rateInput = page.locator("input[type='number']").first();
    this.reasonInput = page.locator("input[type='text']").or(page.locator("textarea")).first();
    this.scheduleRateBtn = page.getByRole("button", { name: /Schedule|Save|Update/i });
    this.statementsTable = page.locator("table");
    this.payOsSettingsLink = page.getByRole("link", { name: /PayOS Settings|PayOS/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/platform-fees");
  }

  async navigatePayOsConfig(): Promise<void> {
    await this.goto("/admin/platform-payos-config");
  }
}
