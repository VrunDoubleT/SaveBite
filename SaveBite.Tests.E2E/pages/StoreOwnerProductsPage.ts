import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class StoreOwnerProductsPage extends BasePage {
  readonly pageHeading: Locator;
  readonly searchInput: Locator;
  readonly categoryFilter: Locator;
  readonly statusFilter: Locator;
  readonly createProductBtn: Locator;
  readonly productNameInput: Locator;
  readonly productCategorySelect: Locator;
  readonly productDescriptionInput: Locator;
  readonly productBasePriceInput: Locator;
  readonly saveProductBtn: Locator;
  readonly kpiTotalProducts: Locator;
  readonly kpiActiveProducts: Locator;
  readonly kpiHiddenProducts: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Product Management/i });
    this.searchInput = page.getByPlaceholder(/Search products|Search/i);
    this.categoryFilter = page.locator("select").first();
    this.statusFilter = page.locator("select").nth(1);
    this.createProductBtn = page.getByRole("link", { name: /Create product/i }).or(page.getByRole("button", { name: /Create product/i }));
    this.productNameInput = page.getByLabel(/Product name/i).or(page.locator("input[name='name']"));
    this.productCategorySelect = page.getByLabel(/Category/i).or(page.locator("select[name='categoryId']"));
    this.productDescriptionInput = page.getByLabel(/Description/i).or(page.locator("textarea[name='description']"));
    this.productBasePriceInput = page.getByLabel(/Price|Base price/i).or(page.locator("input[name='price']"));
    this.saveProductBtn = page.getByRole("button", { name: /Save product|Create product/i });
    this.kpiTotalProducts = page.getByText(/Total products/i);
    this.kpiActiveProducts = page.getByText(/Active products/i);
    this.kpiHiddenProducts = page.getByText(/Hidden products/i);
  }

  async navigate(): Promise<void> {
    await this.goto("/store-owner/products");
  }

  async navigateToCreate(): Promise<void> {
    await this.goto("/store-owner/products/create");
  }
}
