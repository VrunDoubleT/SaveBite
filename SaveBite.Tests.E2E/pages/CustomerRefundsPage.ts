import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class CustomerRefundsPage extends BasePage {
  readonly pageHeading: Locator;
  readonly refundCards: Locator;
  readonly refundTypeFilter: Locator;
  readonly refundStatusFilter: Locator;
  readonly viewRefundDetailsLinks: Locator;

  // Refund Request Form
  readonly submitRefundButton: Locator;
  readonly refundItemCheckboxes: Locator;
  readonly refundReasonTextareas: Locator;
  readonly emptyItemsAlert: Locator;

  // Refund Destination & OTP
  readonly enterBankDetailsButton: Locator;
  readonly uploadBankQrButton: Locator;
  readonly sendConfirmationEmailButton: Locator;
  readonly emailCodeInput: Locator;
  readonly confirmDestinationButton: Locator;
  readonly withdrawRequestButton: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Refunds|Refund history/i });
    this.refundCards = page.locator("article, [aria-label^='Refund for order ']");
    this.refundTypeFilter = page.locator("select").nth(0);
    this.refundStatusFilter = page.locator("select").nth(1);
    this.viewRefundDetailsLinks = page.getByRole("link", { name: /View refund details/i });

    // Request Form
    this.submitRefundButton = page.getByRole("button", { name: /Submit refund request/i });
    this.refundItemCheckboxes = page.locator("input[type='checkbox'][id^='refund-item-']");
    this.refundReasonTextareas = page.locator("textarea[id^='refund-reason-']");
    this.emptyItemsAlert = page.getByText(/Select at least one item and quantity/i);

    // Destination & OTP
    this.enterBankDetailsButton = page.getByRole("button", { name: /Enter bank details/i });
    this.uploadBankQrButton = page.getByRole("button", { name: /Upload bank QR/i });
    this.sendConfirmationEmailButton = page.getByRole("button", { name: /Send confirmation email|Resend confirmation email/i });
    this.emailCodeInput = page.getByLabel(/Email code/i);
    this.confirmDestinationButton = page.getByRole("button", { name: /Confirm destination/i });
    this.withdrawRequestButton = page.getByRole("button", { name: /Withdraw request/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/account/refunds");
  }

  async navigateToRequest(orderId: string): Promise<void> {
    await this.goto(`/orders/${orderId}/refund-request`);
  }

  async navigateToDetail(refundId: string): Promise<void> {
    await this.goto(`/refunds/${refundId}`);
  }
}
