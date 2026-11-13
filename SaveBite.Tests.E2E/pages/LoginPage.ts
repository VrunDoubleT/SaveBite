import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class LoginPage extends BasePage {
  readonly heading: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly createAccountLink: Locator;
  readonly forgotPasswordLink: Locator;
  readonly formCard: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole("heading", { name: "Sign in to SaveBite" });
    this.emailInput = page.locator('input[type="email"]');
    this.passwordInput = page.locator('input[autocomplete="current-password"]');
    this.submitButton = page.locator('button[type="submit"]', { hasText: /Sign in|Signing in/i });
    this.createAccountLink = page.getByRole("link", { name: /Create an account/i });
    this.forgotPasswordLink = page.getByRole("link", { name: /Forgot password/i });
    this.formCard = page.locator("form");
  }

  async navigate(): Promise<void> {
    await this.goto("/login");
    await this.waitForPageLoad();
    await this.emailInput.waitFor({ state: "visible", timeout: 30_000 });
  }

  async login(email: string, pass: string, revealPassword: boolean = true): Promise<void> {
    await this.emailInput.fill(email);
    if (revealPassword) {
      await this.revealPasswordField("password", this.formCard);
    }
    await this.passwordInput.fill(pass);
    await this.submitButton.click();
  }

  alertWithText(text: string | RegExp): Locator {
    return this.formCard.getByRole("alert").filter({ hasText: text });
  }

  infoBannerWithText(text: string | RegExp): Locator {
    return this.formCard.getByRole("status").filter({ hasText: text });
  }
}
