import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class ForgotPasswordPage extends BasePage {
  readonly heading: Locator;
  readonly emailInput: Locator;
  readonly newPasswordInput: Locator;
  readonly confirmNewPasswordInput: Locator;
  readonly submitButton: Locator;
  readonly formCard: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole("heading", { name: "Reset your password" });
    this.formCard = page.locator("form");
    this.emailInput = this.formCard.getByLabel("Email", { exact: true });
    this.newPasswordInput = this.formCard.getByLabel("New password", { exact: true });
    this.confirmNewPasswordInput = this.formCard.getByLabel("Confirm new password", { exact: true });
    this.submitButton = this.formCard.locator('button[type="submit"]');
  }

  async navigate(): Promise<void> {
    await this.goto("/forgot-password");
    await this.waitForPageLoad();
    await this.emailInput.waitFor({ state: "visible", timeout: 30_000 });
  }

  async requestReset(
    email: string,
    newPassword: string,
    confirmPassword: string = newPassword,
    revealPasswords: boolean = true,
  ): Promise<void> {
    await this.emailInput.fill(email);
    if (revealPasswords) {
      await this.revealPasswordField("new password", this.formCard);
      await this.revealPasswordField("confirm new password", this.formCard);
    }
    await this.newPasswordInput.fill(newPassword);
    await this.confirmNewPasswordInput.fill(confirmPassword);
    await this.submitButton.click();
  }

  alertWithText(text: string | RegExp): Locator {
    return this.formCard.getByRole("alert").filter({ hasText: text });
  }
}
