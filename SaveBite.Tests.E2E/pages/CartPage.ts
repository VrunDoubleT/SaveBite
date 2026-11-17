import { type Locator, type Page } from "@playwright/test";
import { BasePage } from "./BasePage.js";

export class CartPage extends BasePage {
  readonly pageHeader: Locator;
  readonly emptyCartState: Locator;
  readonly exploreDealsLink: Locator;
  readonly shopCards: Locator;
  readonly cartItemRows: Locator;
  readonly loadingSkeleton: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeader = page.getByRole("heading", { name: "Your cart" }).first();
    this.emptyCartState = page.getByText("Your cart is empty").first();
    this.exploreDealsLink = page.getByRole("link", { name: /Explore deals/i }).first();
    this.shopCards = page.locator('section[aria-label^="Cart from"]');
    this.cartItemRows = page.locator("li").filter({
      has: page.locator('a[href^="/flash-deals/"]'),
    });
    this.loadingSkeleton = page.locator('[aria-label="Loading your cart"]');
  }

  async navigate(): Promise<void> {
    await this.goto("/cart");
    // Wait for either the page header or empty state to appear
    await this.pageHeader
      .or(this.emptyCartState)
      .first()
      .waitFor({ state: "visible", timeout: 30_000 });

    // Wait for skeleton loader to detach if loading
    if (await this.loadingSkeleton.isVisible({ timeout: 1500 }).catch(() => false)) {
      await this.loadingSkeleton.waitFor({ state: "detached", timeout: 30_000 }).catch(() => {});
    }
  }

  shopCardByName(shopName: string): Locator {
    return this.page
      .locator(`section[aria-label="Cart from ${shopName}"]`)
      .or(this.page.locator(`section:has-text("${shopName}")`));
  }

  cartItemByName(productName: string): Locator {
    return this.cartItemRows.filter({ hasText: productName });
  }

  increaseQuantityButton(productName: string): Locator {
    return this.page.getByRole("button", {
      name: new RegExp(`Increase quantity of ${productName}`, "i"),
    });
  }

  decreaseQuantityButton(productName: string): Locator {
    return this.page.getByRole("button", {
      name: new RegExp(`Decrease quantity of ${productName}`, "i"),
    });
  }

  quantityInput(productName: string): Locator {
    return this.page.getByRole("spinbutton", {
      name: new RegExp(`Quantity of ${productName}`, "i"),
    });
  }

  removeItemButton(productName: string): Locator {
    return this.page.getByRole("button", {
      name: new RegExp(`Remove ${productName}`, "i"),
    });
  }

  checkoutButtonForShop(shopName: string): Locator {
    return this.page.getByRole("button", {
      name: new RegExp(`Checkout with ${shopName}`, "i"),
    });
  }

  get firstCheckoutButton(): Locator {
    return this.page.getByRole("button", { name: /^Checkout/i }).first();
  }

  toastMessage(text: string | RegExp): Locator {
    return this.page
      .locator('section[aria-label="Notifications"] [role="status"]')
      .filter({ hasText: text });
  }
}
