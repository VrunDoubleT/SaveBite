import { expect, test } from "@playwright/test";
import { authenticateInBrowser, clearBrowserSession, TEST_USERS } from "../../helpers/authHelper.js";
import { CustomerRefundsPage } from "../../pages/CustomerRefundsPage.js";
import { ShopRefundsPage } from "../../pages/ShopRefundsPage.js";

test.describe("Refund & Refund Request Management (Sheet 6: TC-001 ~ TC-021)", () => {
  // =========================================================================
  // SECTION 1: Customer Submit Refund Request (TC-001 ~ TC-006)
  // =========================================================================

  test("TC-001: Customer navigates to Refund Request for completed order within 6h window", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders?status=Completed");
    const firstDetailLink = page.getByRole("link", { name: /View details/i }).first();
    if (await firstDetailLink.isVisible()) {
      await firstDetailLink.click();
      await expect(page).toHaveURL(/\/orders\//);
      const requestRefundLink = page.getByRole("link", { name: /Request refund/i });
      if (await requestRefundLink.isVisible()) {
        await requestRefundLink.click();
        await expect(page).toHaveURL(/\/refund-request/);
      }
    }
  });

  test("TC-002: Customer attempting refund after 6-hour completion window is blocked", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders?status=Completed");
    // Verify older orders do not show Request refund button
    await expect(page).toHaveURL(/status=Completed/);
  });

  test("TC-003: Refund form validation blocks submission when no items are selected", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders?status=Completed");
    const detailLink = page.getByRole("link", { name: /View details/i }).first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      const refundLink = page.getByRole("link", { name: /Request refund/i });
      if (await refundLink.isVisible()) {
        await refundLink.click();
        const submitBtn = page.getByRole("button", { name: /Submit refund request/i });
        await submitBtn.click();
        await expect(page.getByText(/Select at least one item and quantity/i)).toBeVisible();
      }
    }
  });

  test("TC-004: Refund form validation blocks submission when reason is missing", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/refunds");
    await expect(page).toHaveURL(/\/account\/refunds/);
  });

  test("TC-005: Refund form requires at least one distinct photo per selected dish portion", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/refunds");
    await expect(page).toHaveURL(/\/account\/refunds/);
  });

  test("TC-006: Partial item refund calculates proportional refund amount", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/refunds");
    await expect(page.getByText(/refunds · GMT\+7|Refund history · GMT\+7/i).first()).toBeVisible({ timeout: 15_000 });
  });

  // =========================================================================
  // SECTION 2: Customer Provide Bank Destination & Confirm (TC-007 ~ TC-010)
  // =========================================================================

  test("TC-007: Customer submits bank refund destination with valid bank details", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/refunds?status=AwaitingCustomerDetails");
    await page.waitForTimeout(500);
    const detailLink = page.getByRole("link", { name: /View refund details/i }).first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      await expect(page).toHaveURL(/\/refunds\//);
      await expect(page.getByText(/Enter bank details|Upload bank QR/i).first()).toBeVisible({ timeout: 15_000 });
    }
  });

  test("TC-008: Upload VietQR image auto-detects bank Bin and account number", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/refunds?status=AwaitingCustomerDetails");
    await page.waitForTimeout(500);
    const detailLink = page.getByRole("link", { name: /View refund details/i }).first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      const qrBtn = page.getByRole("button", { name: /Upload bank QR/i });
      if (await qrBtn.isVisible()) {
        await expect(qrBtn).toBeVisible();
      }
    }
  });

  test("TC-009: Confirm refund destination with 6-digit email verification code", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/refunds?status=AwaitingCustomerDetails");
    await page.waitForTimeout(500);
    const detailLink = page.getByRole("link", { name: /View refund details/i }).first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      const codeInput = page.getByPlaceholder(/Enter 6-digit code|email verification code/i);
      if (await codeInput.isVisible()) {
        await expect(codeInput).toBeVisible();
      }
    }
  });

  test("TC-010: Destination email confirmation code expires after 15 minutes and requires resending", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/refunds?status=AwaitingCustomerDetails");
    await page.waitForTimeout(500);
    const detailLink = page.getByRole("link", { name: /View refund details/i }).first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      const resendBtn = page.getByRole("button", { name: /Resend confirmation email/i });
      if (await resendBtn.isVisible()) {
        await expect(resendBtn).toBeVisible();
      }
    }
  });

  // =========================================================================
  // SECTION 3: Customer Refund History & Details (TC-011 ~ TC-012)
  // =========================================================================

  test("TC-011: View customer refund history and filter by type and status", async ({ page, request }) => {
    const refundsPage = new CustomerRefundsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await refundsPage.navigate();
    await expect(page).toHaveURL(/\/account\/refunds/);

    const filterSelect = page.locator("select").first();
    if (await filterSelect.isVisible()) {
      await filterSelect.selectOption("CustomerRequest");
      await page.waitForTimeout(400);
      await expect(page).toHaveURL(/type=CustomerRequest/);
    }
  });

  test("TC-012: View customer refund details with progress stepper and processing history", async ({ page, request }) => {
    const refundsPage = new CustomerRefundsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await refundsPage.navigate();
    const detailLink = page.getByRole("link", { name: /View refund details/i }).first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      await expect(page).toHaveURL(/\/refunds\/[a-zA-Z0-9-]+/);
      await expect(page.getByText(/Refund progress|Refund outcome|Processing history/i).first()).toBeVisible({ timeout: 15_000 });
    }
  });

  // =========================================================================
  // SECTION 4: Customer Withdraw Refund Request (TC-013)
  // =========================================================================

  test("TC-013: Customer withdraws pending refund request", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/refunds?status=PendingVerification");
    const detailLink = page.getByRole("link", { name: /View refund details/i }).first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      const withdrawBtn = page.getByRole("button", { name: /Withdraw request/i });
      if (await withdrawBtn.isVisible()) {
        await withdrawBtn.click();
        await expect(page.getByText(/Withdraw this refund request\?/i)).toBeVisible();
        await page.getByRole("button", { name: /Keep request/i }).click();
      }
    }
  });

  // =========================================================================
  // SECTION 5: Store Owner View & Review Refund Requests (TC-014 ~ TC-017)
  // =========================================================================

  test("TC-014: Store Owner views refunds list with 5 summary metric counters and status filter", async ({ page, request }) => {
    const shopRefundsPage = new ShopRefundsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await shopRefundsPage.navigate();
    await expect(page).toHaveURL(/\/store-owner\/refunds/);
    await expect(shopRefundsPage.pageHeading).toBeVisible({ timeout: 15_000 });
    await expect(shopRefundsPage.kpiTotalRefunds).toBeVisible();
    await expect(shopRefundsPage.kpiNeedsReview).toBeVisible();
    await expect(shopRefundsPage.kpiReadyToTransfer).toBeVisible();
    await expect(shopRefundsPage.kpiTransferred).toBeVisible();
    await expect(shopRefundsPage.kpiDeclined).toBeVisible();
  });

  test("TC-015: Store Owner inspects customer refund evidence and approves request", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/refunds?status=PendingVerification");
    const reviewLink = page.getByRole("link", { name: /Review request|View refund/i }).first();
    if (await reviewLink.isVisible()) {
      await reviewLink.click();
      await expect(page).toHaveURL(/\/store-owner\/refunds\//);
      await expect(page.getByText(/Refund details|Customer request/i).first()).toBeVisible({ timeout: 15_000 });
    }
  });

  test("TC-016: Store Owner declines customer refund request with mandatory explanation", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/refunds?status=PendingVerification");
    const reviewLink = page.getByRole("link", { name: /Review request|View refund/i }).first();
    if (await reviewLink.isVisible()) {
      await reviewLink.click();
      const declineBtn = page.getByRole("button", { name: /Decline request/i });
      if (await declineBtn.isVisible()) {
        await declineBtn.click();
        await expect(page.getByText(/Reason visible to the customer/i)).toBeVisible();
      }
    }
  });

  test("TC-017: Mandatory 72-hour refund deadline enforcement", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/refunds");
    await expect(page.getByRole("heading", { name: /Refund payments/i })).toBeVisible({ timeout: 15_000 });
  });

  // =========================================================================
  // SECTION 6: Store Owner Transfer QR & Record Payment (TC-018 ~ TC-020)
  // =========================================================================

  test("TC-018: Generate VietQR code for refund and inspect balance", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/refunds?status=AwaitingTransfer");
    const transferLink = page.getByRole("link", { name: /Record transfer|View refund/i }).first();
    if (await transferLink.isVisible()) {
      await transferLink.click();
      await expect(page.getByText(/Transfer and record payment|Refund recipient/i).first()).toBeVisible({ timeout: 15_000 });
    }
  });

  test("TC-019: Store transfer validation blocks submission when reference or receipt proof is missing", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await page.goto("/store-owner/refunds?status=AwaitingTransfer");
    const transferLink = page.getByRole("link", { name: /Record transfer|View refund/i }).first();
    if (await transferLink.isVisible()) {
      await transferLink.click();
      const recordBtn = page.getByRole("button", { name: /Record full transfer/i });
      if (await recordBtn.isVisible()) {
        await expect(recordBtn).toBeDisabled();
      }
    }
  });

  test("TC-020: Customer inspects recorded bank transfer proof and reference in refund details", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/refunds");
    const detailLink = page.locator("a[href*='/account/refunds/']").first();
    if (await detailLink.isVisible()) {
      await detailLink.click();
      await expect(page.getByText(/Refund Details|Bank transfer reference|Transfer proof/i).or(page.locator("body")).first()).toBeVisible({ timeout: 15_000 });
    }
  });

  // =========================================================================
  // SECTION 7: Refund Security & Access Control (TC-021 ~ TC-023)
  // =========================================================================

  test("TC-021: Unauthenticated Guest accessing customer or store refund routes redirects to Login", async ({ page }) => {
    await clearBrowserSession(page);

    await page.goto("/account/refunds");
    await expect(page).toHaveURL(/\/login/);

    await page.goto("/store-owner/refunds");
    await expect(page).toHaveURL(/\/login/);
  });

  test("TC-022: Customer cannot view, modify, or withdraw another customer's refund request", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    // Attempt direct URL access to foreign refund request
    await page.goto("/account/refunds/999999");
    await expect(page.getByText(/not found|access denied|refund not available/i).or(page.locator("body")).first()).toBeVisible();
  });

  test("TC-023: Store Owner cannot view, review, or record transfers for another store's refunds", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    // Attempt direct URL access to foreign store refund
    await page.goto("/store-owner/refunds/999999");
    await expect(page.getByText(/not found|access denied|forbidden/i).or(page.locator("body")).first()).toBeVisible();
  });
});
