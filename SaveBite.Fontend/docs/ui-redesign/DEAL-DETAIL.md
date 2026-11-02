# Flash deal detail — `/flash-deals/:id`

2026-11-02 · `ui-redesign-page` + `ui-ux-pro-max` · commit `ui(/flash-deals/:id): redesign`. Alias `/deals/:id` uses the same page and is included in regression checks.

## Design

Uses the approved home style: primary green action/selection, an unadorned product title, indigo category/pickup accents and warm amber ordering countdown. Shared white cards keep the layout calm.

Desktop: gallery, description and pickup store on the left; sticky selection/purchase panel on the right. Mobile/tablet: gallery, purchase panel, description, pickup details. Native buttons provide pressed states for photos/options, descriptive quantity actions and visible keyboard focus. Countdown is readable without announcing every tick to screen readers. Upcoming/closed/unavailable deals show their appropriate timing label rather than an active ordering countdown.

The page prioritizes product photos, the selected price, variant, availability and Add to cart. Pickup address and full date/time deadlines come from the DTO. Removed fixed star ratings/review counts and the unsupported Open label. The Customer reviews section is present below the detail content and uses the existing store-review service, with its store-wide scope clearly labeled. Missing descriptions/images/times now have honest fallback text instead of invented product claims, unrelated photos or a fixed 21:00 deadline. Stock uses actual quantities, including zero.

Skill tooling: ran `food product detail selection --design-system` and retained relevant image/CTA hierarchy only; project tokens override palette/font recommendations. The variant-selection search and its retry did not produce a verified selection-specific match, so the implementation follows the skill's general keyboard, pressed-state and touch-target guidance. The React stack search confirmed effect dependency/lifecycle guidance.

## Data and behavior

- Existing `flashDealApi.getDealById(id)` and `FlashDeal`/`FlashDealVariant` contracts. Uses product/name/category/description/photos, shop ID/name/logo/address/distance, variant attributes/prices/quantity/status and deal status/start/order-end/shop-close timestamps. Corrected the audit rows to include the already-existing detail service call.
- Keeps default-variant preference, attribute matching/fallback, direct variant selection, photo selection, quantity limits, sale/variant/stock checks, auth redirect and `addCartItem(variant.id, quantity)` behavior. Existing toast/store mechanisms handle mutations.
- The content mounts afresh per deal ID/retry. The existing fetch stays in an effect; retry repeats the same request. A clock interval updates derived countdown/eligibility without synchronous effect state writes. Loading, 404/empty and request-error-with-retry are separate UI states.
- Added shared `MediaImage` and its showcase examples; other UI controls reuse established primitives. Real API/service/cart-store contracts remain unchanged. The detail now consumes the existing store-review read service.

## Complete component coverage and customer reviews

Preserve every section/component when redesigning a page, including placeholder and secondary sections. Missing data/API fields require an honest empty/error state or a clearly labeled supported source; they are not a reason to remove a section.

The complete detail now covers: breadcrumb, gallery/thumbnails, category/status/title/shop link, deal timing, price/discount, attribute and direct-variant selection, stock, quantity, cart success/error, pickup notice, description, store/address/deadlines, and Customer reviews.

`DealStoreReviews` consumes `shopApi.getStoreReviews(shopId, { rating, page, pageSize: 4 })`. There is no product-review endpoint in the current service contract, so the section explicitly says its reviews cover all products at this store. Existing `StoreReviewsSummary`, `StoreReview` and reply fields drive the UI. Rating/counts are server data, not fixed values. The purchase panel links to the review section and labels the score as Store rating. Backend ShopService.GetStoreReviewsAsync was checked: global shop aggregates are separate from the paginated/rating-filtered review list. Review-fetch errors are isolated from purchasing and have retry/refresh actions.

The shared `StoreReviewList` is restyled completely: rating summary and breakdown, all/star/photo filters, loading skeletons, empty results with action, reviewer avatar/name/tier/date/stars, purchased product/variant, comment, photo thumbnails, store response, pagination, and a native accessible photo modal. Photo filtering is clearly scoped to the current page because the existing query DTO supports rating/page but no global photo filter. Pagination stays available when the photo filter leaves an empty page. Controls retain focus while filtered results load; modal Escape restores focus to the photo button. The shop page reuses the same component and is regression-tested.

Verification: six review checks cover 360/768/1280px, modal keyboard/close/focus restore, star query/reset-to-page-one, page-scoped photos, pagination, loading/error/retry/empty, shared shop integration and existing-session preservation. Targeted lint and production build pass. Review captures: `/tmp/savebite-reviews-{360,768,1280}.png` (sticky site header hidden only during section capture).

## Purchase panel spacing refinement

The store link and rating link occupy separate compact rows directly under the product title. The store icon uses primary green; the rating uses a soft amber surface, dark score, secondary text and an amber star. Reduced outer section spacing from six to four spacing units and tightened option/action separators so the panel reads as one connected purchase flow. All sections, data and interactions remain present; the lower review component is unchanged.

Targeted lint and production build passed, as did six existing detail/alias/review checks across 360/768/1280px. Reviewed `/tmp/savebite-panel-refined.png`.

## Current workflow and verification

The frontend uses the real backend only. Follow [WORKFLOW.md](./WORKFLOW.md) and the frontend AGENTS.md; do not recreate API fixtures, simulated authentication/realtime or seeded page data. Open this page with an actual account and real resource IDs from the backend.

The existing approved UI and its API/DTO contracts are retained. Earlier visual checks were performed during the redesign and are historical evidence, not verification against the current real backend. The retired preview-dependent suites have been removed. Validate current loading/empty/error/normal states with backend-supported data and run the production build and relevant lint checks. Record any unavailable backend/account/state explicitly rather than fabricating it.
