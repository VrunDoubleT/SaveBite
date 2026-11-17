import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class StoreOwnerShopProfilePage extends BasePage {
  readonly pageHeading: Locator;
  readonly shopNameInput: Locator;
  readonly phoneInput: Locator;
  readonly descriptionInput: Locator;
  readonly openingTimeInput: Locator;
  readonly closingTimeInput: Locator;
  readonly saveBusinessInfoBtn: Locator;
  readonly saveImagesBtn: Locator;
  readonly temporaryCloseToggle: Locator;
  readonly statusBadge: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Shop Profile|Store Profile/i });
    this.shopNameInput = page.getByLabel(/Shop name|Store name/i).or(page.locator("input[name='name']"));
    this.phoneInput = page.getByLabel(/Phone number|Phone/i).or(page.locator("input[name='phone']"));
    this.descriptionInput = page.getByLabel(/Description|Bio/i).or(page.locator("textarea[name='description']"));
    this.openingTimeInput = page.locator("input[type='time']").first();
    this.closingTimeInput = page.locator("input[type='time']").nth(1);
    this.saveBusinessInfoBtn = page.getByRole("button", { name: /Save business info|Save changes/i });
    this.saveImagesBtn = page.getByRole("button", { name: /Save images|Update images/i });
    this.temporaryCloseToggle = page.getByRole("switch").or(page.locator("input[type='checkbox']").first());
    this.statusBadge = page.locator("[data-slot='badge'], .badge");
  }

  async navigate(): Promise<void> {
    await this.goto("/store-owner/profile");
  }
}
