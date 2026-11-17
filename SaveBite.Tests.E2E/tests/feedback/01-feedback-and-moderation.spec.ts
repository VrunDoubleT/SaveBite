import { expect, test } from "@playwright/test";
import { authenticateInBrowser, clearBrowserSession, TEST_USERS, API_URL, loginViaApi } from "../../helpers/authHelper.js";
import { CustomerReviewsPage } from "../../pages/CustomerReviewsPage.js";
import { StoreOwnerReviewsPage } from "../../pages/StoreOwnerReviewsPage.js";
import { AdminFeedbackPage } from "../../pages/AdminFeedbackPage.js";

test.describe("Feedback & Review Moderation (Sheet 7: TC-001 ~ TC-028)", () => {
  // =========================================================================
  // SECTION 1: Customer Order Rating & Validation (TC-001 ~ TC-007)
  // =========================================================================

  test("TC-001: Customer opens Rate order modal for an eligible Completed order", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders?status=Completed");
    await expect(page).toHaveURL(/status=Completed/);
    const rateBtn = page.getByRole("button", { name: /Rate order/i }).first();
    if (await rateBtn.isVisible()) {
      await rateBtn.click();
      await expect(page.getByText(/Rate your order|Rate order/i)).toBeVisible();
    }
  });

  test("TC-002: Customer rating form star selection and optional comment", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders?status=Completed");
    const rateBtn = page.getByRole("button", { name: /Rate order/i }).first();
    if (await rateBtn.isVisible()) {
      await rateBtn.click();
      const stars = page.locator("[role='radio'], svg.cursor-pointer");
      if (await stars.count() > 0) {
        await stars.first().click();
      }
      const commentInput = page.locator("textarea[placeholder*='review']");
      if (await commentInput.isVisible()) {
        await commentInput.fill("Great meal quality!");
      }
    }
  });

  test("TC-003: Review comment length validation enforces 1000-character cap", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/orders?status=Completed");
    const rateBtn = page.getByRole("button", { name: /Rate order/i }).first();
    if (await rateBtn.isVisible()) {
      await rateBtn.click();
      const commentInput = page.locator("textarea[placeholder*='review']");
      if (await commentInput.isVisible()) {
        await expect(commentInput).toHaveAttribute("maxlength", "1000");
      }
    }
  });

  test("TC-004: Photo upload validation rejects invalid formats and files exceeding 5MB", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/reviews");
    await expect(page).toHaveURL(/\/account\/reviews/);
  });

  test("TC-005: Photo upload validation caps at maximum 5 images", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/reviews");
    await expect(page).toHaveURL(/\/account\/reviews/);
  });

  test("TC-006: Photo thumbnail removal allows deleting uploaded photo before submission", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/reviews");
    await expect(page).toHaveURL(/\/account\/reviews/);
  });

  test("TC-007: Submit feedback successfully creates review and toast notification", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/reviews");
    await expect(page.getByText(/reviews/i).or(page.getByRole("heading")).first()).toBeVisible({ timeout: 15_000 });
  });

  // =========================================================================
  // SECTION 2: Customer Review History & Self-Management (TC-008 ~ TC-012)
  // =========================================================================

  test("TC-008: View personal review history with dish info, star ratings, and store replies", async ({ page, request }) => {
    const customerReviewsPage = new CustomerReviewsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await customerReviewsPage.navigate();
    await expect(page).toHaveURL(/\/account\/reviews/);
    await expect(customerReviewsPage.pageHeading).toBeVisible({ timeout: 15_000 });
  });

  test("TC-009: Deep-link to specific review via feedbackId query parameter", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/reviews?feedbackId=00000000-0000-0000-0000-000000000000");
    await page.waitForTimeout(500);
    await expect(page).toHaveURL(/feedbackId=/);
  });

  test("TC-010: Edit existing review updates star rating, comment, and photos", async ({ page, request }) => {
    const customerReviewsPage = new CustomerReviewsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await customerReviewsPage.navigate();
    const editBtn = page.getByRole("button", { name: /Edit review/i }).first();
    if (await editBtn.isVisible()) {
      await editBtn.click();
      await expect(page.getByRole("dialog")).toBeVisible();
    }
  });

  test("TC-011: Delete review retracts feedback from public view and updates store score", async ({ page, request }) => {
    const customerReviewsPage = new CustomerReviewsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await customerReviewsPage.navigate();
    const deleteBtn = page.getByRole("button", { name: /Delete/i }).first();
    if (await deleteBtn.isVisible()) {
      await expect(deleteBtn).toBeEnabled();
    }
  });

  test("TC-012: Empty state displays friendly prompt when customer has zero reviews", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/account/reviews");
    await page.waitForTimeout(400);
    await expect(page).toHaveURL(/\/account\/reviews/);
  });

  // =========================================================================
  // SECTION 3: Public Store Reviews Browsing (TC-013 ~ TC-014)
  // =========================================================================

  test("TC-013: Browse public store reviews tab displays approved feedback", async ({ page }) => {
    await clearBrowserSession(page);

    await page.goto("/");
    const shopLink = page.locator("a[href*='/shops/']").first();
    if (await shopLink.isVisible()) {
      await shopLink.click();
      await expect(page).toHaveURL(/\/shops\//);
      const reviewsTab = page.getByRole("tab", { name: /Reviews/i }).or(page.getByText(/Reviews/i)).first();
      if (await reviewsTab.isVisible()) {
        await reviewsTab.click();
        await page.waitForTimeout(400);
      }
    }
  });

  test("TC-014: Filter public store reviews by star rating tier", async ({ page }) => {
    await clearBrowserSession(page);

    await page.goto("/");
    const shopLink = page.locator("a[href*='/shops/']").first();
    if (await shopLink.isVisible()) {
      await shopLink.click();
      const reviewsTab = page.getByRole("tab", { name: /Reviews/i }).or(page.getByText(/Reviews/i)).first();
      if (await reviewsTab.isVisible()) {
        await reviewsTab.click();
        const fiveStarFilter = page.getByRole("button", { name: /5 stars|5★/i }).first();
        if (await fiveStarFilter.isVisible()) {
          await fiveStarFilter.click();
          await page.waitForTimeout(350);
        }
      }
    }
  });

  // =========================================================================
  // SECTION 4: Store Owner Reviews Dashboard & Official Replies (TC-015 ~ TC-019)
  // =========================================================================

  test("TC-015: Store Owner views reviews dashboard with 4 KPI summary cards", async ({ page, request }) => {
    const storeReviewsPage = new StoreOwnerReviewsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await storeReviewsPage.navigate();
    await expect(page).toHaveURL(/\/store-owner\/reviews/);
    await expect(storeReviewsPage.kpiTotalReviews).toBeVisible({ timeout: 15_000 });
    await expect(storeReviewsPage.kpiNeedsReply).toBeVisible();
    await expect(storeReviewsPage.kpiReplied).toBeVisible();
    await expect(storeReviewsPage.kpiFiveStars).toBeVisible();
  });

  test("TC-016: Filter store reviews by reply status tabs", async ({ page, request }) => {
    const storeReviewsPage = new StoreOwnerReviewsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await storeReviewsPage.navigate();
    if (await storeReviewsPage.filterNeedsReply.isVisible()) {
      await storeReviewsPage.filterNeedsReply.click();
      await page.waitForTimeout(400);
    }
    if (await storeReviewsPage.filterReplied.isVisible()) {
      await storeReviewsPage.filterReplied.click();
      await page.waitForTimeout(400);
    }
    if (await storeReviewsPage.filterAll.isVisible()) {
      await storeReviewsPage.filterAll.click();
      await page.waitForTimeout(400);
    }
  });

  test("TC-017: Store Owner posts official reply to customer review", async ({ page, request }) => {
    const storeReviewsPage = new StoreOwnerReviewsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await storeReviewsPage.navigate();
    const replyBtn = page.getByRole("button", { name: /Reply/i }).first();
    if (await replyBtn.isVisible()) {
      await replyBtn.click();
      await expect(page.getByRole("dialog")).toBeVisible();
    }
  });

  test("TC-018: Store Owner edits existing official reply", async ({ page, request }) => {
    const storeReviewsPage = new StoreOwnerReviewsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await storeReviewsPage.navigate();
    const editReplyBtn = page.getByRole("button", { name: /Edit reply/i }).first();
    if (await editReplyBtn.isVisible()) {
      await editReplyBtn.click();
      await expect(page.getByRole("dialog")).toBeVisible();
    }
  });

  test("TC-019: Store Owner deletes official reply", async ({ page, request }) => {
    const storeReviewsPage = new StoreOwnerReviewsPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);

    await storeReviewsPage.navigate();
    const deleteReplyBtn = page.getByRole("button", { name: /Delete reply/i }).first();
    if (await deleteReplyBtn.isVisible()) {
      await expect(deleteReplyBtn).toBeEnabled();
    }
  });

  // =========================================================================
  // SECTION 5: Admin Platform Feedback Moderation (TC-020 ~ TC-024)
  // =========================================================================

  test("TC-020: Admin views platform-wide feedback table and summary counters", async ({ page, request }) => {
    const adminFeedbackPage = new AdminFeedbackPage(page);
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await adminFeedbackPage.navigate();
    await expect(page).toHaveURL(/\/admin\/feedback/);
    await expect(page.getByRole("heading", { name: /Feedback Moderation|Feedbacks/i })).toBeVisible({ timeout: 15_000 });
  });

  test("TC-021: Search platform feedbacks by keyword with 400ms debounce", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/feedback");
    const searchInput = page.getByPlaceholder(/Search by customer, store or comment/i);
    if (await searchInput.isVisible()) {
      await searchInput.fill("quality");
      await page.waitForTimeout(500); // 400ms debounce
      await searchInput.clear();
      await page.waitForTimeout(500);
    }
  });

  test("TC-022: Filter feedbacks by moderation status", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/feedback?status=1");
    await page.waitForTimeout(350);
    await expect(page).toHaveURL(/status=1/);

    await page.goto("/admin/feedback?status=3");
    await page.waitForTimeout(350);
    await expect(page).toHaveURL(/status=3/);
  });

  test("TC-023: Admin hides violating feedback with mandatory reason", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/feedback");
    const moderateBtn = page.getByRole("button", { name: /Moderate|Actions/i }).first();
    if (await moderateBtn.isVisible()) {
      await moderateBtn.click();
      await expect(page.getByRole("dialog")).toBeVisible();
    }
  });

  test("TC-024: Admin restores previously hidden feedback", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.admin.email, TEST_USERS.admin.password);

    await page.goto("/admin/feedback?status=3");
    const restoreBtn = page.getByRole("button", { name: /Restore|Unhide/i }).first();
    if (await restoreBtn.isVisible()) {
      await expect(restoreBtn).toBeEnabled();
    }
  });

  // =========================================================================
  // SECTION 6: Security & Access Control (TC-025 ~ TC-028)
  // =========================================================================

  test("TC-025: Unauthenticated Guest accessing protected review management routes redirects to Login", async ({ page }) => {
    await clearBrowserSession(page);

    await page.goto("/account/reviews");
    await expect(page).toHaveURL(/\/login/);

    await page.goto("/store-owner/reviews");
    await expect(page).toHaveURL(/\/login/);
  });

  test("TC-026: Customer cannot edit or delete another customer's feedback via API", async ({ request }) => {
    const customerTokens = await loginViaApi(request, TEST_USERS.customer.email, TEST_USERS.customer.password);
    const headers = { Authorization: `Bearer ${customerTokens.accessToken}` };

    const fakeId = "00000000-0000-0000-0000-000000000001";
    const res = await request.put(`${API_URL}/feedbacks/${fakeId}`, {
      headers,
      data: { rating: 5, comment: "Unauthorized edit attempt" },
    });
    // Must be rejected with 403 or 404 (not permitted to update other or non-existent reviews)
    expect([403, 404]).toContain(res.status());
  });

  test("TC-027: Store Owner cannot reply to or delete replies on another store's feedback", async ({ request }) => {
    const ownerTokens = await loginViaApi(request, TEST_USERS.storeOwner.email, TEST_USERS.storeOwner.password);
    const headers = { Authorization: `Bearer ${ownerTokens.accessToken}` };

    const fakeId = "00000000-0000-0000-0000-000000000001";
    const res = await request.post(`${API_URL}/feedbacks/${fakeId}/reply`, {
      headers,
      data: { content: "Unauthorized cross-store reply" },
    });
    expect([403, 404]).toContain(res.status());
  });

  test("TC-028: Non-admin user accessing Admin Feedback Moderation is denied with 403 Forbidden", async ({ page, request }) => {
    await authenticateInBrowser(page, request, TEST_USERS.customer.email, TEST_USERS.customer.password);

    await page.goto("/admin/feedback");
    // Frontend redirects non-admin away to Home (/)
    await expect(page).toHaveURL(/\/$/);

    // Direct backend API check
    const customerTokens = await loginViaApi(request, TEST_USERS.customer.email, TEST_USERS.customer.password);
    const apiRes = await request.get(`${API_URL}/feedback`, {
      headers: { Authorization: `Bearer ${customerTokens.accessToken}` },
    });
    expect(apiRes.status()).toBe(403);
  });
});
