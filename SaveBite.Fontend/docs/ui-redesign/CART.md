# Cart — `/cart`

2026-11-02 · `ui-redesign-page` + `ui-ux-pro-max` · commit `ui(/cart): redesign`.

## Complete component inventory

- Page heading, All deals → Cart breadcrumb, explanation of checkout per shop and in-store pickup badge.
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

CartShop fields used: shopId, shopName, shopLogoUrl, shopAddress, items, totalQuantity, subtotal, canCheckout. CartItem fields used: id, flashDealId, productName, variantName, imageUrl, originalPrice, unitPrice, quantity, availableQuantity, subtotal, orderEndTime, canUpdateQuantity, isAvailable, unavailableReason. The new deadline display only formats the existing orderEndTime field; it introduces no client-side expiry rule.

Checkout continues to navigate to `/checkout?shopId=…` only when the server permits it and no mutation/loading/read error blocks it. All totals are server values; loaded shop count describes the current cursor pages. No global checkout or invented fees/discounts were added. Other shops remain in the cart. Manual quantity input retains integer and 1…availableQuantity validation; stock/quantity guards are unchanged. Removal keeps the existing direct action and now reports success; mutation failure refreshes authoritative values as before.

## Skill guidance

Ran design-system searches for `food marketplace shopping cart` and `ecommerce cart grouped items`. Their marketing/showcase recommendations did not match a transactional grouped cart, so they were not adopted. Used relevant focused UX guidance for inline validation, error recovery and action feedback. Retained the existing reversible remove interaction. Project tokens and the approved pages govern the design.

## Current workflow and verification

The frontend uses the real backend only. Follow [WORKFLOW.md](./WORKFLOW.md) and the frontend AGENTS.md; do not recreate API fixtures, simulated authentication/realtime or seeded page data. Open this page with an actual account and real resource IDs from the backend.

The existing approved UI and its API/DTO contracts are retained. Earlier visual checks were performed during the redesign and are historical evidence, not verification against the current real backend. The retired preview-dependent suites have been removed. Validate current loading/empty/error/normal states with backend-supported data and run the production build and relevant lint checks. Record any unavailable backend/account/state explicitly rather than fabricating it.


## Cart copy and breadcrumb refinement

Removed the repeated sentence beneath each shop's Checkout button at the user's request. The page-level one-shop checkout explanation and all checkout guards remain. Breadcrumb now uses All deals → Cart, using a compact text link and a plain-text › separator instead of an icon, with aria-current on the current page.
