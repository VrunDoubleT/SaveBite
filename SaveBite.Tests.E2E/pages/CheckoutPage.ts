import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class CheckoutPage extends BasePage {
  readonly pageHeader: Locator;
  readonly getGpsButton: Locator;
  readonly routeSummary: Locator;
  readonly softDeadlineBox: Locator;
  readonly hardDeadlineBox: Locator;
  readonly onlinePaymentRadio: Locator;
  readonly payAtShopRadio: Locator;
  readonly placeOrderButton: Locator;
  readonly orderReceiptHeading: Locator;
  readonly orderPickupCode: Locator;
  readonly paymentSummaryAside: Locator;
  readonly loadingSkeleton: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeader = page.getByRole("heading", { name: "Checkout" });
    this.getGpsButton = page.getByRole("button", {
      name: /Get my GPS location|Refresh GPS & check again/i,
    });
    this.routeSummary = page.locator('span:has-text("km"), span:has-text("minutes by motorcycle")');
    this.softDeadlineBox = page
      .getByText(/Soft deadline · recommended arrival/i)
      .or(page.locator('.bg-primary-50').filter({ hasText: /Soft deadline/i }))
      .or(page.locator('p:has-text("Soft deadline")').filter({ hasText: /recommended/i }));
    this.hardDeadlineBox = page
      .getByText(/Hard deadline · latest pickup/i)
      .or(page.locator('.bg-accent-50').filter({ hasText: /Hard deadline/i }))
      .or(page.locator('p:has-text("Hard deadline")').filter({ hasText: /latest/i }));
    this.onlinePaymentRadio = page.locator('input[type="radio"][value="OnlinePayment"]');
    this.payAtShopRadio = page.locator('input[type="radio"][value="PayAtShop"]');
    this.placeOrderButton = page.getByRole("button", { name: /Place order/i }).first();
    this.orderReceiptHeading = page.getByRole("heading", { name: /Order details/i }).first();
    this.orderPickupCode = page
      .locator('[aria-label="Order code"]')
      .or(page.locator('[aria-label="Order pickup code"]'))
      .or(page.locator('section:has-text("Order code")'))
      .or(page.getByText(/Order code|Pickup code/i));
    this.paymentSummaryAside = page.locator('aside[aria-label="Payment summary"]');
    this.loadingSkeleton = page.locator('[aria-label="Loading checkout"]');
  }

  async navigate(shopId?: string): Promise<void> {
    const url = shopId ? `/checkout?shopId=${encodeURIComponent(shopId)}` : "/checkout";
    await this.goto(url);
    await this.pageHeader
      .or(this.page.getByText("Choose a shop to check out"))
      .first()
      .waitFor({ state: "visible", timeout: 30_000 });

    if (await this.loadingSkeleton.isVisible({ timeout: 1500 }).catch(() => false)) {
      await this.loadingSkeleton.waitFor({ state: "detached", timeout: 30_000 }).catch(() => {});
    }
  }

  async fetchGpsQuote(): Promise<void> {
    if (await this.softDeadlineBox.first().isVisible().catch(() => false)) {
      return;
    }
    if (await this.loadingSkeleton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await this.loadingSkeleton.waitFor({ state: "detached", timeout: 30_000 }).catch(() => {});
    }
    await this.getGpsButton.first().waitFor({ state: "visible", timeout: 15_000 });
    await this.getGpsButton.first().click();

    // Wait for GPS calculation & quote to render, retrying click if missed during hydration
    const appeared = await this.softDeadlineBox.first().waitFor({ state: "visible", timeout: 8_000 }).then(() => true).catch(() => false);
    if (!appeared) {
      if (await this.getGpsButton.first().isVisible().catch(() => false)) {
        await this.getGpsButton.first().click().catch(() => {});
      }
      await this.softDeadlineBox.first().waitFor({ state: "visible", timeout: 25_000 });
    }
  }
}
