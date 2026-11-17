import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class StoreOwnerFlashDealsPage extends BasePage {
  readonly pageHeading: Locator;
  readonly searchInput: Locator;
  readonly createDealBtn: Locator;
  readonly tabAll: Locator;
  readonly tabRunning: Locator;
  readonly tabScheduled: Locator;
  readonly tabEnded: Locator;
  readonly tabHidden: Locator;
  readonly productSelect: Locator;
  readonly dealPriceInput: Locator;
  readonly totalQuantityInput: Locator;
  readonly submitDealBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Flash Deals|Shop Flash Deals/i });
    this.searchInput = page.getByPlaceholder(/Search by dish name|Search/i);
    this.createDealBtn = page.getByRole("button", { name: /Create Flash Deal/i }).or(page.getByRole("link", { name: /Create Flash Deal/i }));
    this.tabAll = page.getByRole("button", { name: /^All/i });
    this.tabRunning = page.getByRole("button", { name: /Running|Active/i });
    this.tabScheduled = page.getByRole("button", { name: /Scheduled|Pending/i });
    this.tabEnded = page.getByRole("button", { name: /Ended/i });
    this.tabHidden = page.getByRole("button", { name: /Hidden/i });
    this.productSelect = page.getByRole("combobox").or(page.locator("select"));
    this.dealPriceInput = page.getByPlaceholder(/Enter deal price|Deal price/i);
    this.totalQuantityInput = page.getByPlaceholder(/Quantity|Total quantity/i);
    this.submitDealBtn = page.getByRole("button", { name: /Create Flash Deal|Save deal/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/store-owner/flash-deals");
  }

  async navigateToCreate(): Promise<void> {
    await this.goto("/store-owner/flash-deals/create");
  }
}
