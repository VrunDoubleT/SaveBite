import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class CustomerReviewsPage extends BasePage {
  readonly pageHeading: Locator;
  readonly reviewCards: Locator;
  readonly emptyReviewsState: Locator;
  readonly editReviewButtons: Locator;
  readonly deleteReviewButtons: Locator;

  // Rate Order Modal (on orders page)
  readonly rateOrderModal: Locator;
  readonly starInputs: Locator;
  readonly commentTextarea: Locator;
  readonly submitReviewsButton: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Your Reviews|My Reviews|Reviews/i }).first();
    this.reviewCards = page.locator("article, [aria-label*='Review']");
    this.emptyReviewsState = page.getByText(/No reviews yet/i);
    this.editReviewButtons = page.getByRole("button", { name: /Edit review/i });
    this.deleteReviewButtons = page.getByRole("button", { name: /Delete/i });

    // Rate Order Modal
    this.rateOrderModal = page.getByRole("dialog", { name: /Rate your order|Rate order/i });
    this.starInputs = page.locator("[role='radio'][aria-label*='star']");
    this.commentTextarea = page.locator("textarea[placeholder*='review']");
    this.submitReviewsButton = page.getByRole("button", { name: /Submit reviews|Submit/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/account/reviews");
  }
}
