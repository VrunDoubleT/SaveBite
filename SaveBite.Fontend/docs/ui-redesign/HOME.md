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


## First-visit location entry — centered, full-width layout

User rejected the two-column entry card and requested the NotFound page's centered composition. The current no-location state is a full-width plain canvas below the existing sticky header, with a decorative plate/location illustration, centered heading/copy and GPS/saved-address actions. There is no enclosing card, split panel, separate discovery header or dotted background. The SVG uses the same green/plate language as NotFound without its 404 label or scattered details; NotFound itself is unchanged.

CustomerLayout lets only the Home route own its content width. Once location is ready, CustomerHomePage wraps the existing discovery content in the exact previous max-width/gutter container. The shared header retains its centered max-width-1280 content; other customer routes keep their original container.

The existing GPS callback, loading/disabled status, real saved-address action and inline permission-error/retry guidance remain. No location/shop/deal/auth data is fabricated; hooks, services and DTOs are unchanged.

Validation: targeted lint and production build pass. Real-browser guest checks at 360/768/1280/1920px verify full-width main content, centered entry content, no background pattern/overflow/application errors, visible CTA, keyboard activation and browser-native denied GPS permission/retry. At 1920px, header content remains 1280px wide and centered; the real login page also retains its original centered 1280px main container. Successful GPS/data loading and authenticated saved-address integration still require actual device permissions/backend/account access and retain the existing implementation. Local captures: `/tmp/savebite-location-centered-{360,768,1280,1920}.png` and the mobile denial capture.


## Browsing location controls

Keep the Use GPS and Saved address controls in the upper location bar. At the user's request, remove only the duplicate GPS and Saved address buttons in the expanded NearbyStoresMapSection's lower Browsing location toolbar. The lower toolbar retains its location information, My location and Refresh map actions. Initial location selection and all location callbacks/service contracts remain unchanged.

Validation for this toolbar correction: production build and Home ESLint pass. The map component retains its pre-existing 17 errors and two warnings, with no added rule/message categories compared with HEAD.
