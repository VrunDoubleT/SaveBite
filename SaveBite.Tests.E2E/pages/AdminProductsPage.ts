import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminProductsPage extends BasePage {
  readonly pageHeading: Locator;
  readonly searchInput: Locator;
  readonly productsTable: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Product Management|Products/i }).first();
    this.searchInput = page.locator("input[placeholder*='Search']").first();
    this.productsTable = page.locator("table");
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/products");
  }
}
