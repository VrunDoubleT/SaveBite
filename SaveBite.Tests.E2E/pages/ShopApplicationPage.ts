import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class ShopApplicationPage extends BasePage {
  readonly pageHeading: Locator;
  readonly startApplicationBtn: Locator;
  readonly shopNameInput: Locator;
  readonly descriptionInput: Locator;
  readonly businessLicenseInput: Locator;
  readonly addressLineInput: Locator;
  readonly cityInput: Locator;
  readonly openingTimeInput: Locator;
  readonly closingTimeInput: Locator;
  readonly payosClientIdInput: Locator;
  readonly payosApiKeyInput: Locator;
  readonly payosChecksumKeyInput: Locator;
  readonly verifyPayosBtn: Locator;
  readonly submitApplicationBtn: Locator;
  readonly cancelApplicationBtn: Locator;
  readonly statusBadge: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Register your shop|Shop application/i }).first();
    this.startApplicationBtn = page.getByRole("button", { name: /Start application/i });
    this.shopNameInput = page.getByLabel(/Shop name/i).or(page.locator("input[name='name']"));
    this.descriptionInput = page.getByLabel(/Description/i).or(page.locator("textarea[name='description']"));
    this.businessLicenseInput = page.getByLabel(/Business license/i).or(page.locator("input[name='businessLicenseNo']"));
    this.addressLineInput = page.getByLabel(/Address line/i).or(page.locator("input[name='addressLine']"));
    this.cityInput = page.getByLabel(/City/i).or(page.locator("input[name='city']"));
    this.openingTimeInput = page.locator("input[type='time']").first();
    this.closingTimeInput = page.locator("input[type='time']").nth(1);
    this.payosClientIdInput = page.getByLabel(/Client ID/i).or(page.locator("input[name='payosClientId']"));
    this.payosApiKeyInput = page.getByLabel(/API Key/i).or(page.locator("input[name='payosApiKey']"));
    this.payosChecksumKeyInput = page.getByLabel(/Checksum Key/i).or(page.locator("input[name='payosChecksumKey']"));
    this.verifyPayosBtn = page.getByRole("button", { name: /Verify payOS/i });
    this.submitApplicationBtn = page.getByRole("button", { name: /Submit application|Save and submit/i });
    this.cancelApplicationBtn = page.getByRole("button", { name: /Cancel application/i });
    this.statusBadge = page.locator("[data-slot='badge'], .badge");
  }

  async navigate(): Promise<void> {
    await this.goto("/account/shop-application");
  }

  async navigateToForm(): Promise<void> {
    await this.goto("/account/shop-application/new");
  }
}
