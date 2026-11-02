import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "./tests",
  testMatch: [
    "mock-backend.spec.ts",
    "home-redesign.spec.ts",
    "deal-detail-redesign.spec.ts",
    "deal-reviews-redesign.spec.ts",
    "shop-redesign.spec.ts",
    "cart-redesign.spec.ts",
  ],
  fullyParallel: false,
  timeout: 300000,
  workers: 1,
  use: { baseURL: "http://127.0.0.1:4175", serviceWorkers: "allow", headless: true },
  webServer: {
    command: "npm run dev:mock -- --host 127.0.0.1 --port 4175 --strictPort",
    url: "http://127.0.0.1:4175",
    reuseExistingServer: !process.env.CI,
  },
});
