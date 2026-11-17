import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class RegisterPage extends BasePage {
  readonly heading: Locator;
  readonly fullNameInput: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly submitButton: Locator;
  readonly signInLink: Locator;
  readonly formCard: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole("heading", { name: "Create your account" });
    this.formCard = page.locator("form");
    this.fullNameInput = this.formCard.getByLabel("Full name", { exact: true });
    this.emailInput = this.formCard.getByLabel("Email", { exact: true });
    this.passwordInput = this.formCard.getByLabel("Password", { exact: true });
    this.confirmPasswordInput = this.formCard.getByLabel("Confirm password", { exact: true });
    this.submitButton = this.formCard.locator('button[type="submit"]');
    this.signInLink = page.getByRole("link", { name: "Sign in", exact: true });
  }

  async navigate(): Promise<void> {
    await this.goto("/register");
    await this.waitForPageLoad();
    await this.fullNameInput.waitFor({ state: "visible", timeout: 30_000 });
  }

  async register(
    fullName: string,
    email: string,
    password: string,
    confirmPassword: string = password,
    revealPasswords: boolean = true,
  ): Promise<void> {
    await this.fullNameInput.fill(fullName);
    await this.emailInput.fill(email);
    if (revealPasswords) {
      await this.revealPasswordField("password", this.formCard);
      await this.revealPasswordField("confirm password", this.formCard);
    }
    await this.passwordInput.fill(password);
    await this.confirmPasswordInput.fill(confirmPassword);
    await this.submitButton.click();
  }

  alertWithText(text: string | RegExp): Locator {
    return this.formCard.getByRole("alert").filter({ hasText: text });
  }
}
