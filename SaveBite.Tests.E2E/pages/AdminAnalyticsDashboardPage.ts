import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class AdminAnalyticsDashboardPage extends BasePage {
  readonly pageHeading: Locator;
  readonly tabOverview: Locator;
  readonly tabRevenueOrders: Locator;
  readonly tabShopRevenue: Locator;
  readonly tabFees: Locator;
  readonly tabUsersShops: Locator;
  readonly tabDeals: Locator;
  readonly tabPaymentsRefunds: Locator;
  readonly periodFilter: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.getByRole("heading", { name: /Platform Analytics|Overview/i }).first();
    this.tabOverview = page.getByRole("tab", { name: /Overview/i }).or(page.getByText(/^Overview/i));
    this.tabRevenueOrders = page.getByRole("tab", { name: /Revenue and orders/i }).or(page.getByText(/Revenue and orders/i));
    this.tabShopRevenue = page.getByRole("tab", { name: /Revenue by shop/i }).or(page.getByText(/Revenue by shop/i));
    this.tabFees = page.getByRole("tab", { name: /Fees/i }).or(page.getByText(/^Fees/i));
    this.tabUsersShops = page.getByRole("tab", { name: /Users and shops/i }).or(page.getByText(/Users and shops/i));
    this.tabDeals = page.getByRole("tab", { name: /Flash Deals/i }).or(page.getByText(/Flash Deals/i));
    this.tabPaymentsRefunds = page.getByRole("tab", { name: /Payments and refunds/i }).or(page.getByText(/Payments and refunds/i));
    this.periodFilter = page.getByRole("button", { name: /Selected period|All time|Month|Year/i }).or(page.locator("button").first());
  }

  async navigate(): Promise<void> {
    await this.goto("/admin/analytics/overview");
  }
}
