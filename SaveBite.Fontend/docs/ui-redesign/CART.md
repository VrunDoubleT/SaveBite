# Cart — `/cart`

2026-11-02 · `ui-redesign-page` + `ui-ux-pro-max` · commit `ui(/cart): redesign`.

Preview: `http://localhost:4175/cart` with mock mode enabled and a signed-in customer.

## Complete component inventory

- Page heading, continue exploring link, explanation of checkout per shop and in-store pickup badge.
- Shop cards with logo/fallback, linked name, address and authoritative item count.
- Every cart item: linked photo/name, variant, original/unit prices, item subtotal and actual order deadline.
- Quantity decrement/increment and manual integer input, stock count, inline validation, busy/locked controls and remove action.
- Unavailable item reason, shop checkout eligibility warning, authoritative shop subtotal and checkout button for that shop.
- Initial skeletons, empty cart with discovery action, read/refresh/mutation errors and retry, success feedback.
- Loaded shop count, refresh status, cursor observer, manual load more, loading-more/retry and end-of-list status.

## Shared style and components

Follows the approved home/detail/storefront language: light surfaces, primary green prices and CTA, subtle green shop header, indigo variant/info accents and amber deadline/availability accents. Existing tokens and Inter remain authoritative; no new primitive or palette was introduced.

Uses PageHeader, Card, Badge, MediaImage, Button/ButtonLink, IconButton, Input, Skeleton, EmptyState and ErrorState. The existing CartItemRow keeps its props contract and now uses shared controls and honest image fallbacks. CheckoutPage also consumes this row in read-only mode: quantity text remains visible and editing/removal controls remain absent. Checkout itself is not marked redesigned.

Desktop pairs each shop's items with its own checkout summary. Tablet/mobile stack the summary below that shop. Long names wrap or clamp with a title tooltip, variants truncate with a title, and addresses truncate with a title. Prices and controls remain readable at 360px.

## Data and behavior

No service, DTO, cartStore, realtime, mock implementation or route contract changed. Reads still use cartApi.getPage and cartApi.summary through the existing store; quantity/removal use the original cartApi mutations. Page activity, refreshPages, cursor loading, mutation serialization and realtime reconciliation retain their existing lifecycle.

CartShop fields used: shopId, shopName, shopLogoUrl, shopAddress, items, totalQuantity, subtotal, canCheckout. CartItem fields used: id, flashDealId, productName, variantName, imageUrl, originalPrice, unitPrice, quantity, availableQuantity, subtotal, orderEndTime, canUpdateQuantity, isAvailable, unavailableReason. The new deadline display only formats the existing orderEndTime field; it introduces no client-side expiry rule.

Checkout continues to navigate to `/checkout?shopId=…` only when the server permits it and no mutation/loading/read error blocks it. All totals are server values; loaded shop count describes the current cursor pages. No global checkout or invented fees/discounts were added. Other shops remain in the cart. Manual quantity input retains integer and 1…availableQuantity validation; stock/quantity guards are unchanged. Removal keeps the existing direct action and now reports success; mutation failure refreshes authoritative values as before.

## Skill guidance

Ran design-system searches for `food marketplace shopping cart` and `ecommerce cart grouped items`. Their marketing/showcase recommendations did not match a transactional grouped cart, so they were not adopted. Used relevant focused UX guidance for inline validation, error recovery and action feedback. Retained the existing reversible remove interaction. Project tokens and the approved pages govern the design.

## Verification

- Targeted ESLint and production build pass; the existing bundle-size advisory remains.
- 12 browser checks pass: `npm run test:mock -- cart-redesign.spec.ts mock-backend.spec.ts --grep-invert 'all declared routes|empty/error/slow scenarios'`.
- Cart checks cover 360/768/1280px without horizontal overflow/application errors, multiple shops, quantity requests/server totals, removal and checkout targeting the selected shop with read-only rows.
- Manual input rejects zero, over-stock and fractional quantities without sending mutations; valid max stock and decrement work. Unavailable/locked items stay guarded and removable.
- Initial loading, empty/discovery, read and mutation failures/retry, final item removal/page activity cleanup and guest auth guard pass.
- Seven-shop fixture verifies cursor loading, failed next-page recovery and final loaded state. Broken media, long metadata and large prices fit mobile.
- Existing commerce regression checks cover persistence, checkout/fulfillment/payment/refund/review projections.
- At the initial redesign, multi-shop/edge fixtures lived only in tests. The subsequent requested mock update below adds a default multi-shop seed.
- Screenshots inspected: `/tmp/savebite-cart-{360,768,1280}.png` and state captures. Baseline: `/tmp/savebite-cart-before.png`. These are local verification artifacts.


## Default multi-shop mock update

The requested mock follow-up adds three default shop groups with two item rows per shop. See [MOCKS.md](./MOCKS.md#multi-shop-cart-preview) for totals, linked catalog records and one-time upgrade of older sessions. Existing cart/checkout UI and real services remain unchanged. Added browser checks cover default group totals, second-shop checkout/removal/add-back and migration/persistence; cart, commerce and storefront regression checks pass.
