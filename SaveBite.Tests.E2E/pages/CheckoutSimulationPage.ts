import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class CheckoutSimulationPage extends BasePage {
  readonly simulationHeading: Locator;
  readonly paySuccessButton: Locator;
  readonly cancelPaymentButton: Locator;
  readonly expirePaymentButton: Locator;
  readonly viewOrderDetailsLink: Locator;

  constructor(page: Page) {
    super(page);
    this.simulationHeading = page.getByRole("heading", { name: "Test payment" });
    this.paySuccessButton = page.getByRole("button", { name: "Payment successful" });
    this.cancelPaymentButton = page.getByRole("button", { name: "Cancel payment" });
    this.expirePaymentButton = page.getByRole("button", { name: "Simulate expiry now" });
    this.viewOrderDetailsLink = page.getByRole("link", { name: "View order details" });
  }

  async navigate(orderId: string): Promise<void> {
    await this.goto(`/checkout/simulation?orderId=${encodeURIComponent(orderId)}`);
    await this.simulationHeading.waitFor({ state: "visible", timeout: 30_000 });
  }
}
