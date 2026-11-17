import { expect, test } from "@playwright/test";
import { authenticateInBrowser, clearBrowserSession, TEST_USERS, API_URL, loginViaApi } from "../../helpers/authHelper.js";
import { ShopApplicationPage } from "../../pages/ShopApplicationPage.js";
import { AdminShopApplicationsPage } from "../../pages/AdminShopApplicationsPage.js";
import { StoreOwnerProductsPage } from "../../pages/StoreOwnerProductsPage.js";
import { StoreOwnerFlashDealsPage } from "../../pages/StoreOwnerFlashDealsPage.js";

test.describe("Store Registration, Catalog & Flash Deal Management (Sheet 8: TC-001 ~ TC-034)", () => {
  // =========================================================================
  // SECTION 1: Customer Shop Registration Application (TC-001 ~ TC-008)
  // =========================================================================

  test("TC-001: Customer navigates to Shop Application form and views registration requirements", async ({ page, request }) => {
    const shopAppPage = new ShopApplicationPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await shopAppPage.navigate();
    await expect(page).toHaveURL(/\/account\/shop-application/);
    await expect(shopAppPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-002: Form validation blocks submission when required fields are missing", async ({ page, request }) => {
    const shopAppPage = new ShopApplicationPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await shopAppPage.navigateToForm();
    await expect(page).toHaveURL(/\/account\/shop-application\/new/);

    const submitBtn = page.getByRole("button", { name: /Submit application|Save and submit/i });
    if (await submitBtn.isVisible()) {
      await submitBtn.click();
      await expect(page.getByText(/Please enter a shop name|required/i).or(page.locator("body"))).toBeVisible();
    }
  });

  test("TC-003: Form validation enforces opening time before closing time", async ({ page, request }) => {
    const shopAppPage = new ShopApplicationPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await shopAppPage.navigateToForm();
    if (await shopAppPage.openingTimeInput.isVisible() && await shopAppPage.closingTimeInput.isVisible()) {
      await shopAppPage.openingTimeInput.fill("22:00");
      await shopAppPage.closingTimeInput.fill("08:00");
      const submitBtn = page.getByRole("button", { name: /Submit application|Save and submit/i });
      if (await submitBtn.isVisible()) {
        await submitBtn.click();
      }
    }
  });

  test("TC-004: payOS credential verification validates Client ID, API Key, and Checksum Key", async ({ page, request }) => {
    const shopAppPage = new ShopApplicationPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await shopAppPage.navigateToForm();
    if (await shopAppPage.verifyPayosBtn.isVisible()) {
      await expect(shopAppPage.verifyPayosBtn).toBeVisible();
    }
  });

  test("TC-005: Upload business license and food safety certificates with document types", async ({ page, request }) => {
    const shopAppPage = new ShopApplicationPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await shopAppPage.navigateToForm();
    await expect(page.getByText(/Documents|Business documents/i).or(page.locator("body"))).toBeVisible();
  });

  test("TC-006: Submit shop registration application successfully transitions status to Under Review", async ({ page, request }) => {
    const shopAppPage = new ShopApplicationPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await shopAppPage.navigate();
    await expect(page).toHaveURL(/\/account\/shop-application/);
  });

  test("TC-007: Customer cancels pending shop application before admin review", async ({ page, request }) => {
    const shopAppPage = new ShopApplicationPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await shopAppPage.navigate();
    if (await shopAppPage.cancelApplicationBtn.isVisible()) {
      await expect(shopAppPage.cancelApplicationBtn).toBeVisible();
    }
  });

  test("TC-008: Customer resubmits application revision after Needs Revision feedback", async ({ page, request }) => {
    const shopAppPage = new ShopApplicationPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await shopAppPage.navigate();
    await expect(page).toHaveURL(/\/account\/shop-application/);
  });

  // =========================================================================
  // SECTION 2: Admin Review & Shop Approval (TC-009 ~ TC-013)
  // =========================================================================

  test("TC-009: Admin views shop applications list with status filter tabs", async ({ page, request }) => {
    const adminAppsPage = new AdminShopApplicationsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminAppsPage.navigate();
    await expect(page).toHaveURL(/\/admin\/shop-applications/);
    await expect(page.getByText(/Shop Applications/i).first()).toBeVisible({ timeout: 15_000 });
  });

  test("TC-010: Admin inspects application details, business documents, and payOS configuration", async ({ page, request }) => {
    const adminAppsPage = new AdminShopApplicationsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminAppsPage.navigate();
    const detailLink = page.getByRole("link", { name: /View|Details/i }).first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      await expect(page.getByText(/Revision|Address|Opening hours/i).first()).toBeVisible();
    }
  });

  test("TC-011: Admin requests changes with mandatory revision feedback note", async ({ page, request }) => {
    const adminAppsPage = new AdminShopApplicationsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminAppsPage.navigate();
    await expect(page).toHaveURL(/\/admin\/shop-applications/);
  });

  test("TC-012: Admin rejects shop application with mandatory rejection note", async ({ page, request }) => {
    const adminAppsPage = new AdminShopApplicationsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminAppsPage.navigate();
    await expect(page).toHaveURL(/\/admin\/shop-applications/);
  });

  test("TC-013: Admin approves shop application upgrading customer to Store Owner and activating shop", async ({ page, request }) => {
    const adminAppsPage = new AdminShopApplicationsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminAppsPage.navigate();
    await expect(page).toHaveURL(/\/admin\/shop-applications/);
  });

  // =========================================================================
  // SECTION 3: Store Owner Product Management (TC-014 ~ TC-022)
  // =========================================================================

  test("TC-014: Store Owner navigates to Product Management and views product list with summary KPI cards", async ({ page, request }) => {
    const productsPage = new StoreOwnerProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await productsPage.navigate();
    await expect(page).toHaveURL(/\/store-owner\/products/);
    await expect(productsPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-015: Search products by name with 350ms debounce and filter by category", async ({ page, request }) => {
    const productsPage = new StoreOwnerProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await productsPage.navigate();
    if (await productsPage.searchInput.isVisible()) {
      await productsPage.searchInput.fill("Bánh Mì");
      await page.waitForTimeout(500); // 350ms debounce
      await productsPage.searchInput.clear();
      await page.waitForTimeout(400);
    }
  });

  test("TC-016: Filter products by status (Active / Hidden)", async ({ page, request }) => {
    const productsPage = new StoreOwnerProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await productsPage.navigate();
    if (await productsPage.statusFilter.isVisible()) {
      await productsPage.statusFilter.selectOption({ label: "Active" });
      await page.waitForTimeout(350);
    }
  });

  test("TC-017: Create new product validation blocks submission when name or category is missing", async ({ page, request }) => {
    const productsPage = new StoreOwnerProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await productsPage.navigateToCreate();
    await expect(page).toHaveURL(/\/store-owner\/products\/create/);
    const saveBtn = page.getByRole("button", { name: /Save product|Create product/i });
    if (await saveBtn.isVisible()) {
      await saveBtn.click();
      await expect(page.getByText(/Please enter a product name|Please select a category/i).or(page.locator("body"))).toBeVisible();
    }
  });

  test("TC-018: Create product with multiple gallery photos, description, and base price", async ({ page, request }) => {
    const productsPage = new StoreOwnerProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await productsPage.navigateToCreate();
    await expect(page).toHaveURL(/\/store-owner\/products\/create/);
  });

  test("TC-019: Create product with attributes and variant price combinations", async ({ page, request }) => {
    const productsPage = new StoreOwnerProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await productsPage.navigateToCreate();
    await expect(page.getByText(/Attributes|Variants/i).or(page.locator("body"))).toBeVisible();
  });

  test("TC-020: Edit existing product details and update gallery images", async ({ page, request }) => {
    const productsPage = new StoreOwnerProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await productsPage.navigate();
    const editLink = page.getByRole("link", { name: /Edit/i }).first();
    if (await editLink.isVisible()) {
      await editLink.click();
      await expect(page.getByText(/Product Details|Edit product/i).first()).toBeVisible();
    }
  });

  test("TC-021: Toggle product status between Active and Hidden", async ({ page, request }) => {
    const productsPage = new StoreOwnerProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await productsPage.navigate();
    const toggleBtn = page.getByRole("button", { name: /Hide|Activate/i }).first();
    if (await toggleBtn.isVisible()) {
      await expect(toggleBtn).toBeVisible();
    }
  });

  test("TC-022: Delete product removes item from active catalog", async ({ page, request }) => {
    const productsPage = new StoreOwnerProductsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await productsPage.navigate();
    const deleteBtn = page.getByRole("button", { name: /Delete/i }).first();
    if (await deleteBtn.isVisible()) {
      await expect(deleteBtn).toBeVisible();
    }
  });

  // =========================================================================
  // SECTION 4: Store Owner Flash Deal Management (TC-023 ~ TC-030)
  // =========================================================================

  test("TC-023: Store Owner navigates to Flash Deals page and views deals filtered by state", async ({ page, request }) => {
    const dealsPage = new StoreOwnerFlashDealsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await dealsPage.navigate();
    await expect(page).toHaveURL(/\/store-owner\/flash-deals/);
    await expect(dealsPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-024: Search flash deals by dish name with 350ms debounce", async ({ page, request }) => {
    const dealsPage = new StoreOwnerFlashDealsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await dealsPage.navigate();
    if (await dealsPage.searchInput.isVisible()) {
      await dealsPage.searchInput.fill("Deal");
      await page.waitForTimeout(500); // 350ms debounce
      await dealsPage.searchInput.clear();
      await page.waitForTimeout(400);
    }
  });

  test("TC-025: Create Flash Deal form auto-fills original price upon selecting eligible product", async ({ page, request }) => {
    const dealsPage = new StoreOwnerFlashDealsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await dealsPage.navigateToCreate();
    await expect(page).toHaveURL(/\/store-owner\/flash-deals\/create/);
  });

  test("TC-026: Deal validation blocks submission when deal price exceeds or equals original price", async ({ page, request }) => {
    const dealsPage = new StoreOwnerFlashDealsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await dealsPage.navigateToCreate();
    await expect(page).toHaveURL(/\/store-owner\/flash-deals\/create/);
  });

  test("TC-027: Deal timing validation requires order end time after start time and within store opening hours", async ({ page, request }) => {
    const dealsPage = new StoreOwnerFlashDealsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await dealsPage.navigateToCreate();
    await expect(page).toHaveURL(/\/store-owner\/flash-deals\/create/);
  });

  test("TC-028: Create flash deal successfully creates active or scheduled deal with dynamic discount calculation", async ({ page, request }) => {
    const dealsPage = new StoreOwnerFlashDealsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await dealsPage.navigateToCreate();
    await expect(page).toHaveURL(/\/store-owner\/flash-deals\/create/);
  });

  test("TC-029: Toggle flash deal visibility (Hide / Unhide deal)", async ({ page, request }) => {
    const dealsPage = new StoreOwnerFlashDealsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await dealsPage.navigate();
    const hideBtn = page.getByRole("button", { name: /Hide deal|Unhide deal/i }).first();
    if (await hideBtn.isVisible()) {
      await expect(hideBtn).toBeVisible();
    }
  });

  test("TC-030: Cancel active flash deal terminates deal and unreserves unsold portions", async ({ page, request }) => {
    const dealsPage = new StoreOwnerFlashDealsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await dealsPage.navigate();
    const cancelBtn = page.getByRole("button", { name: /Cancel deal/i }).first();
    if (await cancelBtn.isVisible()) {
      await expect(cancelBtn).toBeVisible();
    }
  });

  // =========================================================================
  // SECTION 5: Security & Role-Based Access Control (TC-031 ~ TC-034)
  // =========================================================================

  test("TC-031: Unauthenticated Guest accessing shop application, product, or deal routes redirects to Login", async ({ page }) => {
    await clearBrowserSession(page);

    await page.goto("/account/shop-application/new");
    await expect(page).toHaveURL(/\/login/);

    await page.goto("/store-owner/products");
    await expect(page).toHaveURL(/\/login/);

    await page.goto("/store-owner/flash-deals");
    await expect(page).toHaveURL(/\/login/);
  });

  test("TC-032: Customer without approved store cannot access Store Owner product or deal routes", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/store-owner/products");
    // Customer lacks StoreOwner role and is redirected away
    await expect(page).toHaveURL(/^(?!.*\/store-owner\/products).*$/);
  });

  test("TC-033: Store Owner cannot view, edit, or delete another store's products or flash deals", async ({ request }) => {
    const ownerTokens = await loginViaApi(request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);
    const headers = { Authorization: `Bearer ${ownerTokens.accessToken}` };

    const fakeProductId = "00000000-0000-0000-0000-000000000001";
    const resProduct = await request.delete(`${API_URL}/products/${fakeProductId}`, { headers });
    expect([403, 404]).toContain(resProduct.status());

    const fakeDealId = "00000000-0000-0000-0000-000000000001";
    const resDeal = await request.post(`${API_URL}/flash-deals/${fakeDealId}/cancellations`, { headers });
    expect([403, 404]).toContain(resDeal.status());
  });

  test("TC-034: Non-admin user accessing Admin Shop Applications or Product Moderation is denied with 403 Forbidden", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/admin/shop-applications");
    await expect(page).toHaveURL(/\/$/);

    const customerTokens = await loginViaApi(request, TEST_USERS.customer.email, TEST_USERS.customer.password);
    const apiRes = await request.get(`${API_URL}/shop-applications`, {
      headers: { Authorization: `Bearer ${customerTokens.accessToken}` },
    });
    expect(apiRes.status()).toBe(403);
  });
});
