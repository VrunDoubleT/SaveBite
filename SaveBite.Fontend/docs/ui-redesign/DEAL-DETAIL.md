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

## Verification

- TypeScript/production build and targeted ESLint pass. The page's two previous effect lint errors are resolved; no suppressions added. Existing large-bundle advisory remains.
- `npm run test:mock -- deal-detail-redesign.spec.ts`: 13 checks, covering both aliases at 360/768/1280px, no horizontal page overflow or application errors, store navigation, keyboard option selection, photo/price/quantity changes, exact cart request payload, success/error feedback, generic options, guest sign-in and retry.
- Restrictions cover Pending/Hidden/SoldOut/Closed/Cancelled, future sale start, elapsed ordering/shop closing, zero stock, inactive variant and `isPurchasable: false`.
- Slow loading skeleton, empty variants, missing deal, long text, missing optional metadata and broken images are included. Test-only variant fixtures use the real DTO shape and do not change the app's normal mock data.
- Reviewed screenshots in `/tmp/savebite-detail-{360,768,1280}.png`, plus variant/loading/error/partial captures. Before image: `/tmp/savebite-detail-before.png`. Local verification artifacts are not production assets.

Preview a purchasable mock: `http://127.0.0.1:4175/flash-deals/00000000-0000-4000-8000-000000000002`. Query `?role=guest`, `?mock=slow`, `?mock=error`, `?mock=empty` exercises alternate states.

## Gallery seed refinement

Removed the green rule/padding beside the product title at the user's request. The standard preview deal ending in `000002` now has five local product photos and four size/spice variants. Each variant has its own photo, price and stock; selecting a thumbnail or an option uses the existing gallery/selection handlers. The corresponding Product attributes, value IDs, variants and images are seeded consistently with the FlashDeal DTO.

Existing mock sessions receive a one-time, targeted media/options upgrade for this featured product/deal. Cart, orders, unrelated product edits and deal status/timestamps are retained; no session reset is required. The seed marker is internal mock metadata, not an API field. Browser coverage includes all five thumbnails, option/photo/price synchronization, mobile wrapping and preservation of older session data.

Refinement validation: production build and targeted lint passed; all 13 detail checks and two linked mock commerce checks passed across the initial run and the gallery rerun. The gallery check now waits for the detail heading before asserting thumbnails so initial mock-worker startup is included in the readiness precondition. Desktop/mobile gallery captures are in `/tmp/savebite-detail-gallery-{1280,360}.png`.

## Complete component coverage and customer reviews

Preserve every section/component when redesigning a page, including placeholder and secondary sections. Missing data/API fields require an honest empty/error state or a clearly labeled supported source; they are not a reason to remove a section.

The complete detail now covers: breadcrumb, gallery/thumbnails, category/status/title/shop link, deal timing, price/discount, attribute and direct-variant selection, stock, quantity, cart success/error, pickup notice, description, store/address/deadlines, and Customer reviews.

`DealStoreReviews` consumes `shopApi.getStoreReviews(shopId, { rating, page, pageSize: 4 })`. There is no product-review endpoint in the current service contract, so the section explicitly says its reviews cover all products at this store. Existing `StoreReviewsSummary`, `StoreReview` and reply fields drive the UI. Rating/counts are server data, not fixed values. The purchase panel links to the review section and labels the score as Store rating. Backend ShopService.GetStoreReviewsAsync was checked: global shop aggregates are separate from the paginated/rating-filtered review list. Review-fetch errors are isolated from purchasing and have retry/refresh actions.

The shared `StoreReviewList` is restyled completely: rating summary and breakdown, all/star/photo filters, loading skeletons, empty results with action, reviewer avatar/name/tier/date/stars, purchased product/variant, comment, photo thumbnails, store response, pagination, and a native accessible photo modal. Photo filtering is clearly scoped to the current page because the existing query DTO supports rating/page but no global photo filter. Pagination stays available when the photo filter leaves an empty page. Controls retain focus while filtered results load; modal Escape restores focus to the photo button. The shop page reuses the same component and is regression-tested.

Mock review data includes 12 typed reviews with all star ratings, photos, missing optional fields and store replies. Aggregate counts/average/breakdown and profile totals agree. Older preview sessions upgrade only the review seed/profile rating fields; unrelated state remains intact. Empty mode zeros review aggregates as well as lists.

Verification: six review checks cover 360/768/1280px, modal keyboard/close/focus restore, star query/reset-to-page-one, page-scoped photos, pagination, loading/error/retry/empty, shared shop integration and existing-session preservation. Targeted lint and production build pass. Review captures: `/tmp/savebite-reviews-{360,768,1280}.png` (sticky site header hidden only during section capture).

Final restoration verification: the review suite has six passing checks. Detail/alias responsive and request-error recovery checks passed again after restoring the rating link; remaining detail and linked-commerce checks passed in the broader run. First-load error-state verification allows mock-worker bootstrap to finish before asserting content. Production build and lint for every changed TypeScript file pass; the existing bundle-size advisory remains.
