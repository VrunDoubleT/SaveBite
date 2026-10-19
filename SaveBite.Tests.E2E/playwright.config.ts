import { defineConfig, devices } from "@playwright/test";
import dotenv from "dotenv";
import path from "node:path";

dotenv.config({ path: path.resolve(__dirname, ".env") });

const BASE_URL = process.env.BASE_URL || "http://localhost:5173";

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ["list"],
    ["html", { outputFolder: "playwright-report", open: "never" }],
  ],
  use: {
    baseURL: BASE_URL,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    actionTimeout: 10000,
    navigationTimeout: 15000,
  },

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "ghost",
      use: {
        ...devices["Desktop Chrome"],
        headless: false,
        launchOptions: {
          slowMo: 800, // Làm chậm 800ms giữa mỗi thao tác để mắt người nhìn rõ
        },
      },
    },
  ],

  /*
   * Tùy chọn: Tự động khởi động Frontend (Vite) nếu chưa chạy
   * Nếu bạn đã tự chạy frontend bằng `npm run dev` ở terminal khác,
   * reuseExistingServer sẽ sử dụng luôn cổng 5173 đang chạy mà không cần khởi động lại.
   */
  webServer: {
    command: "npm run dev",
    cwd: "../SaveBite.Fontend",
    url: BASE_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 120 * 1000,
  },
});
