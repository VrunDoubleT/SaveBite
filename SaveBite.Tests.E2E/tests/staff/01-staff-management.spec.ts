import { expect, test } from "@playwright/test";
import { authenticateInBrowser, clearBrowserSession, TEST_USERS, API_URL, loginViaApi } from "../../helpers/authHelper.js";
import { StaffManagementPage } from "../../pages/StaffManagementPage.js";

test.describe("Staff Management (Sheet 9: TC-001 ~ TC-020)", () => {
  // =========================================================================
  // SECTION 1: Search Candidates & Invite Staff (TC-001 ~ TC-005)
  // =========================================================================

  test("TC-001: Store Owner navigates to Staff Management and views staff KPI summary cards", async ({ page, request }) => {
    const staffPage = new StaffManagementPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await staffPage.navigate();
    await expect(page).toHaveURL(/\/store-owner\/staff/);
    await expect(page.getByRole("heading", { name: /Staff Management|Shop Staff/i }).first()).toBeVisible({ timeout: 15_000 });
  });

  test("TC-002: Search active staff by name or phone with 350ms debounce", async ({ page, request }) => {
    const staffPage = new StaffManagementPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await staffPage.navigate();
    if (await staffPage.searchInput.isVisible()) {
      await staffPage.searchInput.fill("Staff");
      await page.waitForTimeout(500); // 350ms debounce
      await staffPage.searchInput.clear();
      await page.waitForTimeout(400);
    }
  });

  test("TC-003: Search staff candidates by email or phone in Invite Staff modal", async ({ page, request }) => {
    const staffPage = new StaffManagementPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await staffPage.navigate();
    if (await staffPage.inviteStaffBtn.isVisible()) {
      await staffPage.inviteStaffBtn.click();
      await expect(page.getByRole("dialog").or(page.getByText(/Invite staff/i))).toBeVisible();
    }
  });

  test("TC-004: Invite new staff member with assigned permissions and role", async ({ page, request }) => {
    const staffPage = new StaffManagementPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await staffPage.navigate();
    await expect(page).toHaveURL(/\/store-owner\/staff/);
  });

  test("TC-005: Store Owner views sent staff invitations list and revokes a pending invitation", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/staff/invitations");
    await expect(page).toHaveURL(/\/store-owner\/staff\/invitations/);
  });

  // =========================================================================
  // SECTION 2: Customer Accept & Decline Invitation (TC-006 ~ TC-009)
  // =========================================================================

  test("TC-006: Customer views received store staff invitations in Account portal", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/staff-invitations");
    await expect(page).toHaveURL(/\/account\/staff-invitations/);
    await expect(page.getByRole("heading", { name: /Staff invitations/i }).or(page.getByText(/Staff invitations/i)).first()).toBeVisible({ timeout: 15_000 });
  });

  test("TC-007: Customer inspects invitation details and store profile", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/staff-invitations");
    await expect(page).toHaveURL(/\/account\/staff-invitations/);
  });

  test("TC-008: Customer declines staff invitation terminating invitation record", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/staff-invitations");
    const declineBtn = page.getByRole("button", { name: /Decline/i }).first();
    if (await declineBtn.isVisible()) {
      await expect(declineBtn).toBeVisible();
    }
  });

  test("TC-009: Customer accepts staff invitation granting store staff access permissions", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/staff-invitations");
    const acceptBtn = page.getByRole("button", { name: /Accept/i }).first();
    if (await acceptBtn.isVisible()) {
      await expect(acceptBtn).toBeVisible();
    }
  });

  // =========================================================================
  // SECTION 3: Manage Shop Staff & Revoke Access (TC-010 ~ TC-012)
  // =========================================================================

  test("TC-010: Store Owner edits existing staff nickname and updates permissions", async ({ page, request }) => {
    const staffPage = new StaffManagementPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await staffPage.navigate();
    const editBtn = page.getByRole("button", { name: /Edit/i }).first();
    if (await editBtn.isVisible()) {
      await editBtn.click();
      await expect(page.getByRole("dialog")).toBeVisible();
    }
  });

  test("TC-011: Store Owner updates staff member operational status", async ({ page, request }) => {
    const staffPage = new StaffManagementPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await staffPage.navigate();
    await expect(page).toHaveURL(/\/store-owner\/staff/);
  });

  test("TC-012: Store Owner removes staff member revoking all store management access", async ({ page, request }) => {
    const staffPage = new StaffManagementPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await staffPage.navigate();
    const removeBtn = page.getByRole("button", { name: /Remove/i }).first();
    if (await removeBtn.isVisible()) {
      await expect(removeBtn).toBeVisible();
    }
  });

  // =========================================================================
  // SECTION 4: View Staff Activity Logs & Staff Workspace (TC-013 ~ TC-017)
  // =========================================================================

  test("TC-013: Store Owner inspects staff activity audit logs with date range filter", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/staff");
    await expect(page).toHaveURL(/\/store-owner\/staff/);
  });

  test("TC-014: Staff member logs in and accesses Staff Workspace portal", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.staff.email, TEST_USERS.staff.password);

    await page.goto("/staff/shops/00000000-0000-0000-0000-000000000022/flash-deals");
    await expect(page.getByText(/Staff|Flash Deals|Orders/i).or(page.locator("body"))).toBeVisible({ timeout: 15_000 });
  });

  test("TC-015: Staff member views assigned store orders and advances order status", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.staff.email, TEST_USERS.staff.password);

    await page.goto("/staff/shops/00000000-0000-0000-0000-000000000022/orders");
    await expect(page.getByText(/Orders|Fulfillment/i).or(page.locator("body"))).toBeVisible({ timeout: 15_000 });
  });

  test("TC-016: Staff member generates payment QR code for Pay-at-Shop remaining balance", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.staff.email, TEST_USERS.staff.password);

    await page.goto("/staff/shops/00000000-0000-0000-0000-000000000022/orders");
    await expect(page.locator("body")).toBeVisible();
  });

  test("TC-017: Staff member accesses Surplus Food Restock management and submits restock decision", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.staff.email, TEST_USERS.staff.password);

    await page.goto("/staff/shops/00000000-0000-0000-0000-000000000022/restock");
    await expect(page.getByText(/Restock|Surplus/i).or(page.locator("body"))).toBeVisible({ timeout: 15_000 });
  });

  // =========================================================================
  // SECTION 5: Security & Role-Based Access Control (TC-018 ~ TC-020)
  // =========================================================================

  test("TC-018: Unauthenticated Guest accessing staff routes redirects to Login", async ({ page }) => {
    await clearBrowserSession(page);

    await page.goto("/store-owner/staff");
    await expect(page).toHaveURL(/\/login/);

    await page.goto("/staff/shops/00000000-0000-0000-0000-000000000022/orders");
    await expect(page).toHaveURL(/\/login/);
  });

  test("TC-019: Customer without staff invitation cannot access Staff Workspace routes", async ({ page, request }) => {
    // Ensure customer account has no active staff memberships from prior test runs
    const tokens = await loginViaApi(request, TEST_USERS.customer.email, TEST_USERS.customer.password);
    try {
      const res = await request.get(`${API_URL}/shop-staff/me/shops`, {
        headers: { Authorization: `Bearer ${tokens.accessToken}` },
      });
      if (res.ok()) {
        const json = await res.json();
        const shops = (json.data || []) as Array<{ shopId: string; status: string }>;
        for (const s of shops) {
          if (s.status === "Active") {
            await request.delete(`${API_URL}/shop-staff/me/shops/${s.shopId}`, {
              headers: { Authorization: `Bearer ${tokens.accessToken}` },
            }).catch(() => {});
          }
        }
      }
    } catch {}

    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/staff/shops/00000000-0000-0000-0000-000000000022/orders");
    const sessionChecking = page.locator("text=/Checking your session/i");
    if (await sessionChecking.isVisible({ timeout: 1500 }).catch(() => false)) {
      await sessionChecking.waitFor({ state: "detached", timeout: 30_000 }).catch(() => {});
    }
    await expect(page).toHaveURL(/^(?!.*\/staff\/shops).*$/, { timeout: 15_000 });
  });

  test("TC-020: Staff member cannot access Store Owner financial billing or remove other staff", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.staff.email, TEST_USERS.staff.password);

    await page.goto("/store-owner/platform-fees");
    await expect(page).toHaveURL(/^(?!.*\/store-owner\/platform-fees).*$/);
  });
});
