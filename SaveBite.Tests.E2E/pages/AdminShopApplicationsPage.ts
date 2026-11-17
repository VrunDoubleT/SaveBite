import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminShopApplicationsPage extends BasePage {
  readonly pageHeading: Locator;
  readonly filterAll: Locator;
  readonly filterPending: Locator;
  readonly filterNeedsRevision: Locator;
  readonly filterApproved: Locator;
  readonly filterRejected: Locator;
  readonly filterCancelled: Locator;
  readonly reviewNoteInput: Locator;
  readonly approveBtn: Locator;
  readonly rejectBtn: Locator;
  readonly requestChangesBtn: Locator;
  readonly backToApplicationsBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Shop Applications/i });
    this.filterAll = page.getByRole("tab", { name: /All/i }).or(page.getByText(/^All\b/i));
    this.filterPending = page.getByRole("tab", { name: /Pending/i }).or(page.getByText(/^Pending\b/i));
    this.filterNeedsRevision = page.getByRole("tab", { name: /NeedsRevision|Needs revision/i });
    this.filterApproved = page.getByRole("tab", { name: /Approved/i });
    this.filterRejected = page.getByRole("tab", { name: /Rejected/i });
    this.filterCancelled = page.getByRole("tab", { name: /Cancelled/i });
    this.reviewNoteInput = page.getByPlaceholder(/Enter your review note|Review note/i).or(page.locator("textarea"));
    this.approveBtn = page.getByRole("button", { name: /Approve/i });
    this.rejectBtn = page.getByRole("button", { name: /Reject/i });
    this.requestChangesBtn = page.getByRole("button", { name: /Request changes/i });
    this.backToApplicationsBtn = page.getByRole("button", { name: /Back to applications/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/shop-applications");
  }
}
