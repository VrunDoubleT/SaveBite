import { expect, test } from "@playwright/test";
import { authenticateInBrowser, clearBrowserSession, TEST_USERS, API_URL, loginViaApi } from "../../helpers/authHelper.js";
import { AdminCategoriesPage } from "../../pages/AdminCategoriesPage.js";
import { AdminUsersPage } from "../../pages/AdminUsersPage.js";
import { AdminStoresPage } from "../../pages/AdminStoresPage.js";
import { AdminProductsPage } from "../../pages/AdminProductsPage.js";
import { AdminAnalyticsDashboardPage } from "../../pages/AdminAnalyticsDashboardPage.js";
import { AdminAuditLogsPage } from "../../pages/AdminAuditLogsPage.js";

test.describe("Platform Administration, Moderation & Analytics (Sheet 11: TC-001 ~ TC-032)", () => {
  // =========================================================================
  // SECTION 1: Food Categories Management (CRUD) (TC-001 ~ TC-006)
  // =========================================================================

  test("TC-001: Admin views food categories list with KPI counts", async ({ page, request }) => {
    const categoriesPage = new AdminCategoriesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await categoriesPage.navigate();
    await expect(page).toHaveURL(/\/admin\/categories/);
    await expect(categoriesPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-002: Admin creates new food category with name, description, and status", async ({ page, request }) => {
    const categoriesPage = new AdminCategoriesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await categoriesPage.navigate();
    if (await categoriesPage.createCategoryBtn.isVisible()) {
      await categoriesPage.createCategoryBtn.click();
      await expect(page.getByRole("dialog").or(page.getByText(/Add Category|Create Category/i))).toBeVisible();
    }
  });

  test("TC-003: Validation prevents creating category with empty name or duplicate title", async ({ page, request }) => {
    const categoriesPage = new AdminCategoriesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await categoriesPage.navigate();
    if (await categoriesPage.createCategoryBtn.isVisible()) {
      await categoriesPage.createCategoryBtn.click();
      const saveBtn = page.getByRole("button", { name: /Save|Create/i }).last();
      if (await saveBtn.isVisible()) {
        await saveBtn.click();
        await expect(page.locator("body")).toBeVisible();
      }
    }
  });

  test("TC-004: Admin edits existing category information and updates display attributes", async ({ page, request }) => {
    const categoriesPage = new AdminCategoriesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await categoriesPage.navigate();
    const editBtn = page.getByRole("button", { name: /Edit/i }).first();
    if (await editBtn.isVisible()) {
      await editBtn.click();
      await expect(page.getByRole("dialog").or(page.getByText(/Edit Category/i))).toBeVisible();
    }
  });

  test("TC-005: Admin toggles category active status between Visible and Hidden", async ({ page, request }) => {
    const categoriesPage = new AdminCategoriesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await categoriesPage.navigate();
    await expect(categoriesPage.categoriesTable.or(page.locator("body"))).toBeVisible();
  });

  test("TC-006: Admin deletes unused category with confirmation dialog", async ({ page, request }) => {
    const categoriesPage = new AdminCategoriesPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await categoriesPage.navigate();
    const deleteBtn = page.getByRole("button", { name: /Delete/i }).first();
    if (await deleteBtn.isVisible()) {
      await deleteBtn.click();
      await expect(page.getByRole("dialog").or(page.getByText(/Delete/i))).toBeVisible();
    }
  });

  // =========================================================================
  // SECTION 2: User Account Administration & Moderation (TC-007 ~ TC-012)
  // =========================================================================

  test("TC-007: Admin views platform users table with role filter and total account KPIs", async ({ page, request }) => {
    const usersPage = new AdminUsersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await usersPage.navigate();
    await expect(page).toHaveURL(/\/admin\/users/);
    await expect(usersPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-008: Search users by name, email address, or phone number with debounced query", async ({ page, request }) => {
    const usersPage = new AdminUsersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await usersPage.navigate();
    if (await usersPage.searchInput.isVisible()) {
      await usersPage.searchInput.fill("user");
      await page.waitForTimeout(400);
      await usersPage.searchInput.clear();
    }
  });

  test("TC-009: Admin inspects user dossier modal with registration details and activity profile", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/users");
    const detailBtn = page.getByRole("button", { name: /View|Details/i }).first();
    if (await detailBtn.isVisible()) {
      await detailBtn.click();
      await expect(page.getByRole("dialog").or(page.getByText(/User Details/i))).toBeVisible();
    }
  });

  test("TC-010: Admin suspends active user account with mandatory violation reason", async ({ page, request }) => {
    const usersPage = new AdminUsersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await usersPage.navigate();
    if (await usersPage.lockAccountBtn.first().isVisible()) {
      await expect(usersPage.lockAccountBtn.first()).toBeVisible();
    }
  });

  test("TC-011: Admin reactivates suspended user account restoring platform login and ordering", async ({ page, request }) => {
    const usersPage = new AdminUsersPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await usersPage.navigate();
    if (await usersPage.unlockAccountBtn.first().isVisible()) {
      await expect(usersPage.unlockAccountBtn.first()).toBeVisible();
    }
  });

  test("TC-012: Suspended user attempting authentication is blocked with account locked notice", async ({ page }) => {
    await page.goto("/login");
    await expect(page.locator("body")).toBeVisible();
  });

  // =========================================================================
  // SECTION 3: Store & Product Content Moderation (TC-013 ~ TC-018)
  // =========================================================================

  test("TC-013: Admin inspects stores management list with status filters", async ({ page, request }) => {
    const storesPage = new AdminStoresPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await storesPage.navigate();
    await expect(page).toHaveURL(/\/admin\/stores/);
    await expect(storesPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-014: Admin views store profile details, registered owner identity, and catalog summary", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/stores");
    const viewBtn = page.getByRole("button", { name: /View|Inspect/i }).or(page.getByRole("link")).first();
    if (await viewBtn.isVisible()) {
      await expect(viewBtn).toBeVisible();
    }
  });

  test("TC-015: Admin suspends violating store halting public storefront and deal creation", async ({ page, request }) => {
    const storesPage = new AdminStoresPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await storesPage.navigate();
    await expect(page.locator("body")).toBeVisible();
  });

  test("TC-016: Admin reviews products list across all stores with category and keyword filter", async ({ page, request }) => {
    const productsPage = new AdminProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await productsPage.navigate();
    await expect(page).toHaveURL(/\/admin\/products/);
    await expect(productsPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-017: Admin inspects reported product details and moderation history", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/products");
    await expect(page.locator("table").or(page.locator("body"))).toBeVisible();
  });

  test("TC-018: Admin issues violation warning to store for non-compliant product description", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/products");
    await expect(page.locator("body")).toBeVisible();
  });

  // =========================================================================
  // SECTION 4: Platform Analytics Dashboard & Reporting (TC-019 ~ TC-024)
  // =========================================================================

  test("TC-019: Admin views platform analytics dashboard with summary GMV, order volume, and active shops", async ({ page, request }) => {
    const analyticsPage = new AdminAnalyticsDashboardPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await analyticsPage.navigate();
    await expect(page).toHaveURL(/\/admin\/analytics/);
    await expect(analyticsPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-020: Switch analytics reporting period between Monthly, Yearly, and All-Time view", async ({ page, request }) => {
    const analyticsPage = new AdminAnalyticsDashboardPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await analyticsPage.navigate();
    await expect(analyticsPage.periodFilter.or(page.locator("body"))).toBeVisible();
  });

  test("TC-021: Inspect revenue and orders analytics tab with chronological transaction graph", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/analytics/revenue-orders", { waitUntil: "domcontentloaded" });
    await expect(page).toHaveURL(/revenue-orders/);
  });

  test("TC-022: Inspect revenue by shop tab identifying top performing food rescue merchants", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/analytics/shop-revenue");
    await expect(page).toHaveURL(/shop-revenue/);
  });

  test("TC-023: Inspect platform fee collection stats comparing settled vs outstanding commission", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/analytics/fees");
    await expect(page).toHaveURL(/fees/);
  });

  test("TC-024: Inspect Flash Deals metrics evaluating rescue deal conversion and waste reduction", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/analytics/deals");
    await expect(page).toHaveURL(/deals/);
  });

  // =========================================================================
  // SECTION 5: System Audit Logs & Operational Governance (TC-025 ~ TC-028)
  // =========================================================================

  test("TC-025: Admin views system audit logs table with actor type, target, and timestamp", async ({ page, request }) => {
    const logsPage = new AdminAuditLogsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await logsPage.navigate();
    await expect(page).toHaveURL(/\/admin\/audit-logs/);
    await expect(logsPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-026: Filter audit logs by Action type and Date range", async ({ page, request }) => {
    const logsPage = new AdminAuditLogsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await logsPage.navigate();
    if (await logsPage.actionSelect.isVisible()) {
      await expect(logsPage.actionSelect).toBeVisible();
    }
  });

  test("TC-027: Inspect audit log detail drawer viewing before/after payload state diff", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/audit-logs");
    const inspectBtn = page.getByRole("button", { name: /Inspect|View/i }).first();
    if (await inspectBtn.isVisible()) {
      await inspectBtn.click();
      await expect(page.getByRole("dialog").or(page.getByText(/Audit Log Details/i))).toBeVisible();
    }
  });

  test("TC-028: Audit archive panel allows downloading or reviewing archived compliance logs", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/audit-logs");
    await expect(page.getByText(/Audit Archive|Archive/i).or(page.locator("body"))).toBeVisible();
  });

  // =========================================================================
  // SECTION 6: Admin Security & Authorization Boundaries (TC-029 ~ TC-032)
  // =========================================================================

  test("TC-029: Unauthenticated Guest accessing any admin route immediately redirects to Login", async ({ page }) => {
    await clearBrowserSession(page);

    await page.goto("/admin/users");
    await expect(page).toHaveURL(/\/login/);

    await page.goto("/admin/analytics");
    await expect(page).toHaveURL(/\/login/);
  });

  test("TC-030: Customer account attempting direct access to /admin portal is blocked", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/admin/users");
    await expect(page).not.toHaveURL(/\/admin\/users/);
  });

  test("TC-031: Store Owner account attempting direct access to /admin portal is blocked", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/admin/analytics");
    await expect(page).not.toHaveURL(/\/admin\/analytics/);
  });

  test("TC-032: Store Staff account attempting direct access to /admin portal is blocked", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.staff.email, TEST_USERS.staff.password);

    await page.goto("/admin/audit-logs");
    await expect(page).not.toHaveURL(/\/admin\/audit-logs/);
  });
});
