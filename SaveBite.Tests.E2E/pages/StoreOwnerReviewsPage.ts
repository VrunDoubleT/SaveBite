import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class StoreOwnerReviewsPage extends BasePage {
  readonly pageHeading: Locator;
  readonly kpiTotalReviews: Locator;
  readonly kpiNeedsReply: Locator;
  readonly kpiReplied: Locator;
  readonly kpiFiveStars: Locator;

  // Filter Buttons
  readonly filterAll: Locator;
  readonly filterNeedsReply: Locator;
  readonly filterReplied: Locator;

  // Reply Actions
  readonly replyButtons: Locator;
  readonly editReplyButtons: Locator;
  readonly deleteReplyButtons: Locator;
  readonly replyModal: Locator;
  readonly replyTextarea: Locator;
  readonly submitReplyButton: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Customer Feedback & Reviews|Customer Reviews|Store Reviews|Reviews/i }).first();
    this.kpiTotalReviews = page.locator("p").filter({ hasText: /^Total reviews$/i }).first();
    this.kpiNeedsReply = page.locator("p").filter({ hasText: /^Needs reply$/i }).first();
    this.kpiReplied = page.locator("p").filter({ hasText: /^Replied$/i }).first();
    this.kpiFiveStars = page.locator("p").filter({ hasText: /^5-Star reviews$/i }).first();

    // Filters
    this.filterAll = page.getByRole("button", { name: /All Reviews/i });
    this.filterNeedsReply = page.getByRole("button", { name: /Needs Reply/i });
    this.filterReplied = page.getByRole("button", { name: /Replied/i });

    // Actions
    this.replyButtons = page.getByRole("button", { name: /Reply/i });
    this.editReplyButtons = page.getByRole("button", { name: /Edit reply/i });
    this.deleteReplyButtons = page.getByRole("button", { name: /Delete reply/i });
    this.replyModal = page.getByRole("dialog");
    this.replyTextarea = page.locator("textarea");
    this.submitReplyButton = page.getByRole("button", { name: /Submit reply|Update reply/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/store-owner/reviews");
  }
}
