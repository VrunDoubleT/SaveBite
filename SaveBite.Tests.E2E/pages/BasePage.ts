import { type Page, type Locator } from "@playwright/test";

export abstract class BasePage {
  constructor(protected readonly page: Page) {}

  async goto(path: string = "/"): Promise<void> {
    await this.page.goto(path, { waitUntil: "domcontentloaded" });
  }

  async waitForPageLoad(): Promise<void> {
    await this.page.waitForLoadState("domcontentloaded");
  }

  async getTitle(): Promise<string> {
    return this.page.title();
  }

  async getUrl(): Promise<string> {
    return this.page.url();
  }

  /**
   * Bấm nút con mắt (Show ...) trên PasswordField để hiển thị mật khẩu dạng chữ rõ.
   */
  async revealPasswordField(label: string, scope?: Locator): Promise<void> {
    const root = scope ?? this.page.locator("body");
    const toggle = root.getByRole("button", { name: `Show ${label.toLowerCase()}`, exact: true });
    if (await toggle.isVisible().catch(() => false)) {
      await toggle.click();
    }
  }
}
