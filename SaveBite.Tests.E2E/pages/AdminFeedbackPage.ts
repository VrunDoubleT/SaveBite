import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminFeedbackPage extends BasePage {
  readonly pageHeading: Locator;
  readonly searchInput: Locator;
  readonly kpiTotal: Locator;
  readonly kpiVisible: Locator;
  readonly kpiHiddenByCustomer: Locator;
  readonly kpiHiddenByAdmin: Locator;
  readonly feedbackRows: Locator;
  readonly moderateButtons: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Feedback Moderation|Feedbacks/i });
    this.searchInput = page.getByPlaceholder(/Search by customer, store or comment/i);
    this.kpiTotal = page.getByText(/Total feedbacks|Total reviews/i);
    this.kpiVisible = page.getByText(/Visible/i);
    this.kpiHiddenByCustomer = page.getByText(/Hidden by customer/i);
    this.kpiHiddenByAdmin = page.getByText(/Hidden by admin/i);
    this.feedbackRows = page.locator("tr, [role='row']");
    this.moderateButtons = page.getByRole("button", { name: /Moderate/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/feedback");
  }

  async navigateReviews(): Promise<void> {
    await this.goto("/admin/reviews");
  }
}
