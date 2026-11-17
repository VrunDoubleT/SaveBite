import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class CustomerOrdersPage extends BasePage {
  readonly pageHeading: Locator;
  readonly statusFilterSelect: Locator;
  readonly emptyOrdersState: Locator;
  readonly orderCards: Locator;
  readonly viewDetailsLinks: Locator;

  // Order Details Receipt Locators
  readonly orderReceiptHeading: Locator;
  readonly pickupCodeText: Locator;
  readonly pickupShopName: Locator;
  readonly cancelOrderButton: Locator;
  readonly cancelOrderModal: Locator;
  readonly confirmCancellationButton: Locator;
  readonly keepOrderButton: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.locator("h1").filter({ hasText: /Orders/i }).or(page.getByRole("heading", { name: /Your Orders|Orders/i }));
    this.statusFilterSelect = page.locator("select").or(page.getByRole("combobox", { name: /Order status/i }));
    this.emptyOrdersState = page.getByText(/You have no orders yet|No orders in this status/i);
    this.orderCards = page.locator("article, [aria-label^='Order ']");
    this.viewDetailsLinks = page.getByRole("link", { name: /View details/i });

    // Order Receipt elements
    this.orderReceiptHeading = page.getByRole("heading", { name: /Order details|Pickup shop/i });
    this.pickupCodeText = page.locator("text=/Pickup code|Order code/i");
    this.pickupShopName = page.locator("header h2");
    this.cancelOrderButton = page.getByRole("button", { name: /Cancel order/i });
    this.cancelOrderModal = page.getByRole("dialog", { name: /Cancel this order\?/i });
    this.confirmCancellationButton = page.getByRole("button", { name: /Confirm cancellation/i });
    this.keepOrderButton = page.getByRole("button", { name: /Keep order/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/account/orders");
    const sessionChecking = this.page.locator("text=/Checking your session/i");
    if (await sessionChecking.isVisible({ timeout: 1500 }).catch(() => false)) {
      await sessionChecking.waitFor({ state: "detached", timeout: 30_000 }).catch(() => {});
    }
    await this.pageHeading.first().waitFor({ state: "visible", timeout: 30_000 });
  }

  async navigateToOrderDetail(orderId: string): Promise<void> {
    await this.goto(`/orders/${orderId}`);
  }
}
