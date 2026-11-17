import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminUsersPage extends BasePage {
  readonly pageHeading: Locator;
  readonly searchInput: Locator;
  readonly kpiTotalUsers: Locator;
  readonly kpiActiveAccounts: Locator;
  readonly kpiSuspendedAccounts: Locator;
  readonly usersTable: Locator;
  readonly lockAccountBtn: Locator;
  readonly unlockAccountBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /User Management|Users/i }).first();
    this.searchInput = page.getByPlaceholder(/Search users|Search by name, email/i);
    this.kpiTotalUsers = page.getByText(/Total users/i);
    this.kpiActiveAccounts = page.getByText(/Active accounts|Active/i);
    this.kpiSuspendedAccounts = page.getByText(/Suspended accounts|Suspended/i);
    this.usersTable = page.locator("table");
    this.lockAccountBtn = page.getByRole("button", { name: /Lock|Suspend/i });
    this.unlockAccountBtn = page.getByRole("button", { name: /Unlock|Reactivate/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/users");
  }
}
