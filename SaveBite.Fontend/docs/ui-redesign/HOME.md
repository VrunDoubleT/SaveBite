# Customer home — `/`

2026-11-02 · `ui-redesign-page` + `ui-ux-pro-max` · commit `ui(/): redesign`.

## Structure and decisions

- Keep the existing primary green scale, light surfaces, Inter and shared tokens.
- Put the browsing location, search and deal list above the fold. The store map is a keyboard-operable disclosure, mounted when opened so its canvas measures correctly. Keep the map's location, filtering, store details and routing handlers.
- Desktop uses a filter sidebar and three-card grid; tablet uses two cards; mobile uses one card and an in-flow filter disclosure. Filter labels have generous touch targets and category counts remain tied to loaded deals.
- Cards prioritize product, shop/distance, price, stock and the detail link. Remove the hardcoded `5.0` rating because FlashDeal does not include ratings. “View deal” accurately describes the existing navigation to `/flash-deals/:id`; it does not add to cart directly.
- Shared `ButtonLink` reuses Button variants/sizes and appears in `/__ui`. Other controls, cards, badges, skeletons and feedback reuse shared primitives.
- Preserve hook/service/DTO signatures, authentication, route paths, filter values, pagination and realtime behavior. Reset confirms through toast. No backend mutations were added.

## Data used

FlashDeal: `id`, `productName`, `productImageUrl`, `categoryName`, `shopId`, `shopName`, `distanceInKm`, `minDealPrice`, `maxOriginalPrice`, `maxDiscountPercent`, `saleStartTime`, `orderEndTime`, `status`, `isNew`, `variants` quantities. Hook: categories/counts, existing filters/search, cursor pagination, newer-deal count, loading/error/server message, coordinates/location source/address and GPS state. Map continues using NearbyShop DTO fields.

Missing images have an explicit fallback without pretending another product photo is the actual item. Long product/shop/category/location labels keep their full title; prices wrap and use tabular numerals. Errors use the hook's existing `error` and retry; empty results offer refresh/clear search; loading skeletons reserve image/content space. If the map library is unavailable, the page explains that deal discovery remains usable.

## Skill guidance

Ran the installed search tool with `food marketplace product discovery --design-system`, then the narrower `ecommerce product catalog filters --design-system`. Neither returned a verified page-pattern match: the first suggested storytelling and the retry suggested corporate lead generation. Those pattern/palette/font outputs were not adopted or persisted.

Applied relevant general UX guidance and verified focused results from `search filter empty loading --domain ux` (empty-state actions, no-results recovery) and `list conditional rendering --stack react` (stable IDs and typed reusable components). The project tokens remain authoritative.

## Color accent refinement

Added small token-based accents following user feedback: a soft green title rule and filter icon/counts, a warm amber flash-deal icon background, indigo category pills and selected saved-address control, green selected filters, and green View deal actions (sold-out actions stay neutral). Existing labels, checked states and aria-pressed remain available alongside color. Layout, API and data behavior stay unchanged.

Verification: targeted ESLint and production build passed; the three existing discovery checks passed again at 360/768/1280px. Reviewed the updated desktop screenshot. The existing bundle-size advisory remains.

## Current workflow and verification

The frontend uses the real backend only. Follow [WORKFLOW.md](./WORKFLOW.md) and the frontend AGENTS.md; do not recreate API fixtures, simulated authentication/realtime or seeded page data. Open this page with an actual account and real resource IDs from the backend.

The existing approved UI and its API/DTO contracts are retained. Earlier visual checks were performed during the redesign and are historical evidence, not verification against the current real backend. The retired preview-dependent suites have been removed. Validate current loading/empty/error/normal states with backend-supported data and run the production build and relevant lint checks. Record any unavailable backend/account/state explicitly rather than fabricating it.


## First-visit location entry

The no-location state now uses the same PageHeader, page gutters, shared Card and small green/indigo/amber accents as the approved discovery UI. Removed the full-viewport centered prompt and its large empty space. Desktop pairs a short explanation with a location action panel; mobile/tablet show the action panel first so the GPS button is visible immediately.

The existing GPS callback, loading/disabled state, real saved-address action and permission-error guidance remain. The action panel includes accessible pending status, inline native-GPS failure feedback and a clearly named retry button. The home introduction is reused before and after location readiness. No location, shop or deal data is invented; no auth/API/hook/DTO changes were made.

Validation: targeted lint and production build pass. Real-browser guest checks at 360/768/1280px verify the initial CTA in the viewport, no horizontal overflow/application errors, keyboard GPS activation, browser-native denied permission and the enabled retry action. Coordinates, GPS responses and API data were not fabricated. Successful GPS/data loading and authenticated saved-address integration still require actual device permissions/backend/account access; those paths retain their existing hook implementation. Local captures: `/tmp/savebite-location-{360,768,1280}.png`, denial captures and `/tmp/savebite-location-before.png`.
