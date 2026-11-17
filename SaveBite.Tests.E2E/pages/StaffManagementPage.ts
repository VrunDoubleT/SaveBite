import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class StaffManagementPage extends BasePage {
  readonly pageHeading: Locator;
  readonly searchInput: Locator;
  readonly inviteStaffBtn: Locator;
  readonly staffTable: Locator;
  readonly kpiTotalStaff: Locator;
  readonly kpiActiveStaff: Locator;
  readonly kpiPendingInvitations: Locator;
  readonly sentInvitationsTab: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Staff Management|Shop Staff/i }).first();
    this.searchInput = page.getByPlaceholder(/Search staff|Search/i);
    this.inviteStaffBtn = page.getByRole("button", { name: /Invite staff|Add staff/i });
    this.staffTable = page.locator("table");
    this.kpiTotalStaff = page.getByText(/Total staff/i).first();
    this.kpiActiveStaff = page.getByText(/Active staff/i).first();
    this.kpiPendingInvitations = page.getByText(/Pending invitations/i).first();
    this.sentInvitationsTab = page.getByRole("tab", { name: /Invitations|Sent invitations/i }).or(page.getByText(/Invitations/i)).first();
  }

  async navigate(): Promise<void> {
    await this.goto("/store-owner/staff");
  }

  async navigateToInvitations(): Promise<void> {
    await this.goto("/store-owner/staff/invitations");
  }
}
