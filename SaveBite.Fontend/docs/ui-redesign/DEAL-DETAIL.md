# Flash deal detail — `/flash-deals/:id`

2026-11-02 · `ui-redesign-page` + `ui-ux-pro-max` · commit `ui(/flash-deals/:id): redesign`. Alias `/deals/:id` uses the same page and is included in regression checks.

## Design

Uses the approved home style: primary green action/selection, an unadorned product title, indigo category/pickup accents and warm amber ordering countdown. Shared white cards keep the layout calm.

Desktop: gallery, description and pickup store on the left; sticky selection/purchase panel on the right. Mobile/tablet: gallery, purchase panel, description, pickup details. Native buttons provide pressed states for photos/options, descriptive quantity actions and visible keyboard focus. Countdown is readable without announcing every tick to screen readers. Upcoming/closed/unavailable deals show their appropriate timing label rather than an active ordering countdown.

The page prioritizes product photos, the selected price, variant, availability and Add to cart. Pickup address and full date/time deadlines come from the DTO. Removed fixed star ratings/review counts, the unsupported Open label and the placeholder review section. No review API was added. Missing descriptions/images/times now have honest fallback text instead of invented product claims, unrelated photos or a fixed 21:00 deadline. Stock uses actual quantities, including zero.

Skill tooling: ran `food product detail selection --design-system` and retained relevant image/CTA hierarchy only; project tokens override palette/font recommendations. The variant-selection search and its retry did not produce a verified selection-specific match, so the implementation follows the skill's general keyboard, pressed-state and touch-target guidance. The React stack search confirmed effect dependency/lifecycle guidance.

## Data and behavior

- Existing `flashDealApi.getDealById(id)` and `FlashDeal`/`FlashDealVariant` contracts. Uses product/name/category/description/photos, shop ID/name/logo/address/distance, variant attributes/prices/quantity/status and deal status/start/order-end/shop-close timestamps. Corrected the audit rows to include the already-existing detail service call.
- Keeps default-variant preference, attribute matching/fallback, direct variant selection, photo selection, quantity limits, sale/variant/stock checks, auth redirect and `addCartItem(variant.id, quantity)` behavior. Existing toast/store mechanisms handle mutations.
- The content mounts afresh per deal ID/retry. The existing fetch stays in an effect; retry repeats the same request. A clock interval updates derived countdown/eligibility without synchronous effect state writes. Loading, 404/empty and request-error-with-retry are separate UI states.
- Added shared `MediaImage` and its showcase examples; other UI controls reuse established primitives. No API/service/store/mock production implementation changed.

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
