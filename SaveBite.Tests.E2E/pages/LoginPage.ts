import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class LoginPage extends BasePage {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly createAccountLink: Locator;
  readonly forgotPasswordLink: Locator;
  readonly formCard: Locator;

  constructor(page: Page) {
    super(page);
    this.emailInput = page.locator('input[type="email"]');
    this.passwordInput = page.locator('input[placeholder="Enter your password"], input[type="password"]');
    this.submitButton = page.locator('button[type="submit"]', { hasText: /Sign in/i });
    this.createAccountLink = page.getByRole("link", { name: /Create an account/i });
    this.forgotPasswordLink = page.getByRole("link", { name: /Forgot password/i });
    this.formCard = page.locator("form");
  }

  async navigate(): Promise<void> {
    await this.goto("/login");
    await this.waitForPageLoad();
  }

  async login(email: string, pass: string): Promise<void> {
    await this.emailInput.click();
    await this.emailInput.pressSequentially(email, { delay: 80 }); // Gõ từng chữ một
    await this.passwordInput.click();
    await this.passwordInput.pressSequentially(pass, { delay: 80 }); // Gõ mật khẩu từng chữ một
    await this.submitButton.click();
  }

  async getServerErrorAlert(): Promise<Locator> {
    return this.page.locator('[role="alert"], .text-red-600, .bg-red-50');
  }
}
