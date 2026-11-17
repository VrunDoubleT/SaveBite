import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class ShopOrdersPage extends BasePage {
  readonly pageHeading: Locator;
  readonly searchInput: Locator;
  readonly statusTabs: Locator;
  readonly orderCards: Locator;

  // Order Details Locators
  readonly confirmOrderButton: Locator;
  readonly rejectOrderButton: Locator;
  readonly rejectionReasonTextarea: Locator;
  readonly confirmRejectionButton: Locator;
  readonly startPreparingButton: Locator;
  readonly markReadyForPickupButton: Locator;
  readonly handoverCheckbox: Locator;
  readonly cashReceivedCheckbox: Locator;
  readonly completeOrderButton: Locator;
  readonly generateRemainingQrButton: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Store Orders/i });
    this.searchInput = page
      .getByPlaceholder(/Pickup code or exact order ID|order code/i)
      .or(page.locator('input[placeholder*="order" i]'));
    this.statusTabs = page.locator("button, [role='tab']");
    this.orderCards = page.locator("article, [aria-label^='Order ']");

    // Detail actions
    this.confirmOrderButton = page.getByRole("button", { name: /Confirm order/i });
    this.rejectOrderButton = page.getByRole("button", { name: /Reject order/i });
    this.rejectionReasonTextarea = page.locator("textarea");
    this.confirmRejectionButton = page.getByRole("button", { name: /Confirm rejection/i });
    this.startPreparingButton = page.getByRole("button", { name: /Start preparing/i });
    this.markReadyForPickupButton = page.getByRole("button", { name: /Mark ready for pickup/i });
    this.handoverCheckbox = page.getByLabel(/I have handed the goods to the customer/i);
    this.cashReceivedCheckbox = page.getByLabel(/I have received .* in cash from the customer/i);
    this.completeOrderButton = page.getByRole("button", { name: /Complete order/i });
    this.generateRemainingQrButton = page.getByRole("button", { name: /Generate remaining payment QR/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/store-owner/orders");
  }

  async navigateToOrderDetail(orderId: string): Promise<void> {
    await this.goto(`/store-owner/orders/${orderId}`);
  }
}
