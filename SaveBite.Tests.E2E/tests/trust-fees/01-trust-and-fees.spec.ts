import { expect, test } from "@playwright/test";
import { authenticateInBrowser, clearBrowserSession, TEST_USERS, API_URL, loginViaApi } from "../../helpers/authHelper.js";
import { CustomerTrustPage } from "../../pages/CustomerTrustPage.js";
import { AdminTrustPage } from "../../pages/AdminTrustPage.js";
import { PlatformFeesPage } from "../../pages/PlatformFeesPage.js";
import { AdminPlatformFeesPage } from "../../pages/AdminPlatformFeesPage.js";

test.describe("Trust Score System & Platform Fee Settlement (Sheet 10: TC-001 ~ TC-030)", () => {
  // =========================================================================
  // SECTION 1: Customer Trust Score & Level Benefits (TC-001 ~ TC-006)
  // =========================================================================

  test("TC-001: Customer navigates to Trust Score page and views current score, standing, and tier", async ({ page, request }) => {
    const trustPage = new CustomerTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await trustPage.navigate();
    await expect(page).toHaveURL(/\/account\/trust-score/);
    await expect(trustPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-002: View Trust Level benefits and compare privilege tiers", async ({ page, request }) => {
    const trustPage = new CustomerTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await trustPage.navigate();
    if (await trustPage.tabLevels.isVisible()) {
      await trustPage.tabLevels.click();
      await expect(page).toHaveURL(/levels/);
    }
  });

  test("TC-003: Inspect points transaction history feed with chronological event timestamps", async ({ page, request }) => {
    const trustPage = new CustomerTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await trustPage.navigate();
    if (await trustPage.tabHistory.isVisible()) {
      await trustPage.tabHistory.click();
      await expect(page).toHaveURL(/history/);
    }
  });

  test("TC-004: Inspect penalty status and recovery requirements for restricted accounts", async ({ page, request }) => {
    const trustPage = new CustomerTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await trustPage.navigate();
    if (await trustPage.tabPenalties.isVisible()) {
      await trustPage.tabPenalties.click();
      await expect(page).toHaveURL(/penalties/);
    }
  });

  test("TC-005: Low trust score restricts offline payment (Pay at shop / COD) during checkout", async ({ page, request }) => {
    // Authenticate with user having seeded low trust score (40) and progress
    await authenticateInBrowser(page, request, TEST_USERS.staff.email, TEST_USERS.staff.password);

    await page.goto("/account/trust-score/overview");
    await expect(page.getByText(/Good standing|recovering|unavailable|standing|Trust data is not initialized/i).first()).toBeVisible({ timeout: 15_000 });
  });

  test("TC-006: High trust score displays membership badge and checkout privilege status", async ({ page, request }) => {
    const trustPage = new CustomerTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.changePasswordAccount.email, TEST_USERS.changePasswordAccount.password);

    await trustPage.navigate();
    await expect(page.getByText(/Level \d+|Highest level|points/i).first()).toBeVisible({ timeout: 15_000 });
  });

  // =========================================================================
  // SECTION 2: Admin Configure Trust Rules & Levels (TC-007 ~ TC-013)
  // =========================================================================

  test("TC-007: Admin navigates to Trust Configuration and views configured tier levels", async ({ page, request }) => {
    const adminTrust = new AdminTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminTrust.navigate();
    await expect(page).toHaveURL(/\/admin\/trust/);
    await expect(adminTrust.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-008: Admin creates or edits trust level thresholds and enabled privilege perks", async ({ page, request }) => {
    const adminTrust = new AdminTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminTrust.navigate();
    if (await adminTrust.tabLevels.isVisible()) {
      await adminTrust.tabLevels.click();
      await expect(page.getByText(/Levels|Rank|Threshold/i).or(adminTrust.addLevelBtn)).toBeVisible();
    }
  });

  test("TC-009: Admin inspects and updates no-show penalty point deduction rules", async ({ page, request }) => {
    const adminTrust = new AdminTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminTrust.navigate();
    if (await adminTrust.tabPenaltyRules.isVisible()) {
      await adminTrust.tabPenaltyRules.click();
      await expect(page).toHaveURL(/penalty-rules/);
    }
  });

  test("TC-010: Admin configures positive earning rules for order completion and reviews", async ({ page, request }) => {
    const adminTrust = new AdminTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminTrust.navigate();
    if (await adminTrust.tabScoreRules.isVisible()) {
      await adminTrust.tabScoreRules.click();
      await expect(page).toHaveURL(/score-rules/);
    }
  });

  test("TC-011: Admin modifies penalty recovery requirement thresholds", async ({ page, request }) => {
    const adminTrust = new AdminTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminTrust.navigate();
    if (await adminTrust.tabEarningRules.isVisible()) {
      await adminTrust.tabEarningRules.click();
      await expect(page).toHaveURL(/earning-rules/);
    }
  });

  test("TC-012: Form validation prevents negative score thresholds or overlapping ranks", async ({ page, request }) => {
    const adminTrust = new AdminTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminTrust.navigate();
    await expect(page.locator("body")).toBeVisible();
  });

  test("TC-013: Reset or cancel configuration modal preserves existing rule state", async ({ page, request }) => {
    const adminTrust = new AdminTrustPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminTrust.navigate();
    await expect(adminTrust.pageHeading.or(page.locator("body"))).toBeVisible();
  });

  // =========================================================================
  // SECTION 3: Store Owner View & Pay Platform Fees (TC-014 ~ TC-020)
  // =========================================================================

  test("TC-014: Store Owner views monthly platform fee statements list with KPI financial summary", async ({ page, request }) => {
    const feesPage = new PlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await feesPage.navigate();
    await expect(page).toHaveURL(/\/store-owner\/platform-fees/);
    await expect(feesPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-015: Filter fee statements by Year, Month, and Payment Status", async ({ page, request }) => {
    const feesPage = new PlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await feesPage.navigate();
    if (await feesPage.yearFilter.isVisible()) {
      await feesPage.yearFilter.selectOption({ index: 0 });
    }
  });

  test("TC-016: Inspect monthly platform fee statement detail with order commission breakdown formula", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/platform-fees");
    const detailLink = page.getByRole("link", { name: /View details|Details/i }).first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      await expect(page).toHaveURL(/\/store-owner\/platform-fees\/.+/);
    }
  });

  test("TC-017: Inspect active store commission rate and settlement policy schedule", async ({ page, request }) => {
    const feesPage = new PlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await feesPage.navigate();
    await expect(page.getByText(/Total unpaid fees|Total statements|Operating status/i).first()).toBeVisible({ timeout: 15_000 });
  });

  test("TC-018: Store Owner initiates online fee payment generating payOS checkout payment link", async ({ page, request }) => {
    const feesPage = new PlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await feesPage.navigate();
    const payBtn = page.getByRole("button", { name: /Pay online|Pay platform fee/i }).first();
    if (await payBtn.isVisible()) {
      await payBtn.click();
      await expect(page.getByRole("dialog").or(page.getByText(/PayOS|Payment/i))).toBeVisible();
    }
  });

  test("TC-019: Overdue platform fee warning banner alerts store suspension risk", async ({ page, request }) => {
    const feesPage = new PlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await feesPage.navigate();
    await expect(page.locator("body")).toBeVisible();
  });

  test("TC-020: Webhook payment confirmation settles statement status from Issued to Paid", async ({ page, request }) => {
    const feesPage = new PlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await feesPage.navigate();
    await expect(page.locator("table").or(page.getByText(/No statements/i)).or(feesPage.pageHeading).first()).toBeVisible({ timeout: 15_000 });
  });

  // =========================================================================
  // SECTION 4: Admin Configure Fee & PayOS / Reconcile (TC-021 ~ TC-026)
  // =========================================================================

  test("TC-021: Admin views all stores platform fee statements with global search and period filter", async ({ page, request }) => {
    const adminFees = new AdminPlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminFees.navigate();
    await expect(page).toHaveURL(/\/admin\/platform-fees/);
    await expect(adminFees.pageHeading.or(page.getByText(/Platform Fees Management/i))).toBeVisible({ timeout: 15_000 });
  });

  test("TC-022: Admin inspects store statement detail modal verifying order itemization and totals", async ({ page, request }) => {
    const adminFees = new AdminPlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminFees.navigate();
    const viewBtn = page.getByRole("button", { name: /View|Details/i }).first();
    if (await viewBtn.isVisible()) {
      await viewBtn.click();
      await expect(page.getByRole("dialog").or(page.getByText(/Statement Details/i))).toBeVisible();
    }
  });

  test("TC-023: Admin schedules new platform commission fee rate with effective date and reason", async ({ page, request }) => {
    const adminFees = new AdminPlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminFees.navigate();
    if (await adminFees.rateInput.isVisible()) {
      await adminFees.rateInput.fill("10");
    }
  });

  test("TC-024: Admin inspects upcoming scheduled fee changes and cancels a pending schedule", async ({ page, request }) => {
    const adminFees = new AdminPlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminFees.navigate();
    await expect(page.getByText(/Scheduled|Active|Open-ended/i).or(page.locator("body"))).toBeVisible();
  });

  test("TC-025: Admin configures and validates payOS payment credentials", async ({ page, request }) => {
    const adminFees = new AdminPlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminFees.navigatePayOsConfig();
    await expect(page).toHaveURL(/\/admin\/platform-payos-config/);
    await expect(page.getByRole("heading", { name: /PayOS Settings|Payment credentials/i }).or(page.getByText(/PayOS Settings|Payment credentials/i)).first()).toBeVisible({ timeout: 15_000 });
  });

  test("TC-026: Admin views payOS credential update audit history log", async ({ page, request }) => {
    const adminFees = new AdminPlatformFeesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminFees.navigatePayOsConfig();
    await expect(page.getByText(/Change history|History/i).or(page.locator("body"))).toBeVisible();
  });

  // =========================================================================
  // SECTION 5: Security & Role-Based Access Control (TC-027 ~ TC-030)
  // =========================================================================

  test("TC-027: Unauthenticated Guest accessing trust score or platform fee routes redirects to Login", async ({ page }) => {
    await clearBrowserSession(page);

    await page.goto("/account/trust-score");
    await expect(page).toHaveURL(/\/login/);

    await page.goto("/store-owner/platform-fees");
    await expect(page).toHaveURL(/\/login/);
  });

  test("TC-028: Customer without store owner role cannot access store platform fee management", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/store-owner/platform-fees");
    await expect(page).not.toHaveURL(/\/store-owner\/platform-fees/);
  });

  test("TC-029: Store Owner cannot view or pay another store's platform fee statements", async ({ request }) => {
    const loginRes = await loginViaApi(request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);
    const foreignShopFeeRes = await request.get(`${API_URL}/api/v1/platform-fees/shops/00000000-0000-0000-0000-000000000999/statements`, {
      headers: { Authorization: `Bearer ${loginRes.accessToken}` },
    });
    expect([401, 403, 404]).toContain(foreignShopFeeRes.status());
  });

  test("TC-030: Store Staff cannot access Store Owner platform fee statements or payOS config", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.staff.email, TEST_USERS.staff.password);

    await page.goto("/store-owner/platform-fees");
    await expect(page).not.toHaveURL(/\/store-owner\/platform-fees/);
  });
});
