import { test, expect } from "@playwright/test";

test.describe("System Health & Smoke Test", () => {
  test("Frontend web application should load successfully", async ({ page }) => {
    // Truy cập trang chủ
    await page.goto("/");

    // Đợi DOM sẵn sàng
    await page.waitForLoadState("domcontentloaded");

    // Tiêu đề hoặc URL phải chính xác
    await expect(page).toHaveURL(/\//);

    // Kiểm tra trang có phần tử tử cơ bản (header, navbar hoặc logo)
    const body = page.locator("body");
    await expect(body).toBeVisible();
  });

  test("Navigation to /login page should display login card", async ({ page }) => {
    await page.goto("/login");

    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByText(/Sign in to SaveBite/i)).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });
});
