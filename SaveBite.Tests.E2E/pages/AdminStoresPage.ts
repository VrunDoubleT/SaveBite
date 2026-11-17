import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminStoresPage extends BasePage {
  readonly pageHeading: Locator;
  readonly searchInput: Locator;
  readonly storesTable: Locator;
  readonly suspendStoreBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Shop Management|Stores|Shops/i }).first();
    this.searchInput = page.locator("input[placeholder*='Search']").first();
    this.storesTable = page.locator("table");
    this.suspendStoreBtn = page.getByRole("button", { name: /Suspend|Deactivate/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/stores");
  }
}
