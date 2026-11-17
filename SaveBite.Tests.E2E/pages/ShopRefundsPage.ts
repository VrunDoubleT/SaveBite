import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class ShopRefundsPage extends BasePage {
  readonly pageHeading: Locator;
  readonly showFilterSelect: Locator;
  readonly kpiTotalRefunds: Locator;
  readonly kpiNeedsReview: Locator;
  readonly kpiReadyToTransfer: Locator;
  readonly kpiTransferred: Locator;
  readonly kpiDeclined: Locator;
  readonly refundCards: Locator;

  // Shop Refund Detail actions
  readonly approveRefundButton: Locator;
  readonly declineRequestButton: Locator;
  readonly declineReasonTextarea: Locator;
  readonly confirmDeclineButton: Locator;
  readonly generateTransferQrButton: Locator;
  readonly bankReferenceInput: Locator;
  readonly recordTransferButton: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Refund payments/i }).first();
    this.showFilterSelect = page.locator("select").first();
    this.kpiTotalRefunds = page.locator("p").filter({ hasText: /^Total refunds$/i }).first();
    this.kpiNeedsReview = page.locator("p").filter({ hasText: /^Needs review$/i }).first();
    this.kpiReadyToTransfer = page.locator("p").filter({ hasText: /^Ready to transfer$/i }).first();
    this.kpiTransferred = page.locator("p").filter({ hasText: /^Transferred$/i }).first();
    this.kpiDeclined = page.locator("p").filter({ hasText: /^Declined$/i }).first();
    this.refundCards = page.locator("article");

    // Detail actions
    this.approveRefundButton = page.getByRole("button", { name: /Approve requested refund/i });
    this.declineRequestButton = page.getByRole("button", { name: /Decline request/i });
    this.declineReasonTextarea = page.locator("textarea");
    this.confirmDeclineButton = page.getByRole("button", { name: /Confirm decline/i });
    this.generateTransferQrButton = page.getByRole("button", { name: /Generate transfer QR|Regenerate QR/i });
    this.bankReferenceInput = page.locator("input[required]").filter({ hasText: "" });
    this.recordTransferButton = page.getByRole("button", { name: /Record full transfer/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/store-owner/refunds");
  }

  async navigateToDetail(refundId: string): Promise<void> {
    await this.goto(`/store-owner/refunds/${refundId}`);
  }
}
