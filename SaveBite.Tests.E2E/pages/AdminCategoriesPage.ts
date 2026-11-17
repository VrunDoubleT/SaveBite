import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminCategoriesPage extends BasePage {
  readonly pageHeading: Locator;
  readonly searchInput: Locator;
  readonly createCategoryBtn: Locator;
  readonly categoriesTable: Locator;
  readonly categoryNameInput: Locator;
  readonly categoryCodeInput: Locator;
  readonly categoryDescInput: Locator;
  readonly saveCategoryBtn: Locator;
  readonly kpiTotalCategories: Locator;
  readonly kpiActiveCategories: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Food Categories|Categories|Category/i }).first();
    this.searchInput = page.getByPlaceholder(/Search categories|Search/i);
    this.createCategoryBtn = page.getByRole("button", { name: /Create category|Add category/i });
    this.categoriesTable = page.locator("table");
    this.categoryNameInput = page.getByLabel(/Category name|Name/i).or(page.locator("input[name='name']"));
    this.categoryCodeInput = page.getByLabel(/Code/i).or(page.locator("input[name='code']"));
    this.categoryDescInput = page.getByLabel(/Description/i).or(page.locator("textarea[name='description']"));
    this.saveCategoryBtn = page.getByRole("button", { name: /Create category|Save category|Save changes/i });
    this.kpiTotalCategories = page.getByText(/Total categories/i);
    this.kpiActiveCategories = page.getByText(/Active categories/i);
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/categories");
  }
}
