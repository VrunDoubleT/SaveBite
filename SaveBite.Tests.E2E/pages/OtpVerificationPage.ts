import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class OtpVerificationPage extends BasePage {
  readonly formCard: Locator;
  readonly emailInput: Locator;
  readonly otpInput: Locator;
  readonly submitButton: Locator;
  readonly infoBanner: Locator;

  constructor(page: Page) {
    super(page);
    this.formCard = page.locator("form");
    this.emailInput = this.formCard.getByLabel("Email", { exact: true });
    this.otpInput = this.formCard.getByLabel("Verification code", { exact: true });
    this.submitButton = this.formCard.locator('button[type="submit"]');
    this.infoBanner = this.formCard.getByRole("status");
  }

  async submitOtp(otp: string, email?: string): Promise<void> {
    if (email !== undefined) {
      await this.emailInput.fill(email);
    }
    await this.otpInput.fill(otp);
    await this.submitButton.click();
  }

  alertWithText(text: string | RegExp): Locator {
    return this.formCard.getByRole("alert").filter({ hasText: text });
  }
}
