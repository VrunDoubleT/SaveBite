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

## Verification

- `npm run build`: passed; existing large-chunk advisory remains.
- ESLint: passed for home, the four deal/filter components, ButtonLink/export, showcase, test and Playwright config. The existing map component has the same **17 errors and 2 warnings** as HEAD (14 explicit-any, 2 synchronous-effect state errors, 1 immutability error, 2 dependency warnings); no new map lint diagnostics. Integration refactoring is outside this page's visual scope.
- `npm run test:mock -- home-redesign.spec.ts`: **7 checks passed**. Coverage includes 360/768/1280px with no horizontal page overflow or application errors; category/reset, search/clear, GPS/saved location, map disclosure by keyboard, infinite loading from 9 to 18, and detail navigation.
- Normal/empty/error/slow/denied-location/missing-image/long-name states rendered. Empty and error actions successfully restore results; denied GPS can be retried.
- Browser check with CDN enabled: Leaflet initialized, map pane and markers rendered, no application errors. Mock mode uses fixture map tiles, so geographic basemap artwork is not a live-backend acceptance check.
- Screenshots were inspected in `/tmp/savebite-home-{360,768,1280}.png`, plus map/state captures. These are local verification artifacts, not production assets.

Run locally: `npm run dev:mock -- --host 127.0.0.1 --port 4175`. Open `http://127.0.0.1:4175/`; `?mock=empty`, `?mock=error`, `?mock=slow`, and `?role=guest` exercise the alternate states.

## Color accent refinement

Added small token-based accents following user feedback: a soft green title rule and filter icon/counts, a warm amber flash-deal icon background, indigo category pills and selected saved-address control, green selected filters, and green View deal actions (sold-out actions stay neutral). Existing labels, checked states and aria-pressed remain available alongside color. Layout, API and data behavior stay unchanged.

Verification: targeted ESLint and production build passed; the three existing discovery checks passed again at 360/768/1280px. Reviewed the updated desktop screenshot. The existing bundle-size advisory remains.
