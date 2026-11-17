import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class PaymentsPage extends BasePage {
  readonly pageHeader: Locator;
  readonly paymentStatusFilter: Locator;
  readonly paymentCards: Locator;
  readonly loadingSkeleton: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeader = page.getByText(/Payment history/i).or(page.getByRole("heading", { name: "Payments" }));
    this.paymentStatusFilter = page.locator('select, button[aria-haspopup="listbox"]');
    this.paymentCards = page.locator("section, article, div").filter({
      has: page.locator('a[href^="/account/payments/"]'),
    });
    this.loadingSkeleton = page.locator('[aria-label="Loading payments"]');
  }

  async navigate(): Promise<void> {
    await this.goto("/account/payments");
    await this.pageHeader.first().waitFor({ state: "visible", timeout: 30_000 });

    if (await this.loadingSkeleton.isVisible({ timeout: 1500 }).catch(() => false)) {
      await this.loadingSkeleton.waitFor({ state: "detached", timeout: 30_000 }).catch(() => {});
    }
  }

  get firstPaymentDetailLink(): Locator {
    return this.page.locator('a[href^="/account/payments/"]').first();
  }
}
