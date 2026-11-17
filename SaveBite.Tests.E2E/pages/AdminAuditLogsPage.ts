import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminAuditLogsPage extends BasePage {
  readonly pageHeading: Locator;
  readonly logsTable: Locator;
  readonly totalEntriesText: Locator;
  readonly actorTypeSelect: Locator;
  readonly targetTypeSelect: Locator;
  readonly actionSelect: Locator;
  readonly refreshBtn: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Audit Logs/i }).first();
    this.logsTable = page.locator("table");
    this.totalEntriesText = page.getByText(/entries in total|match your filters/i);
    this.actorTypeSelect = page.locator("select").first();
    this.targetTypeSelect = page.locator("select").nth(1);
    this.actionSelect = page.locator("select").nth(2);
    this.refreshBtn = page.getByRole("button", { name: /Refresh/i });
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/audit-logs");
  }
}
