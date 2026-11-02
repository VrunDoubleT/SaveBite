# Storefront — `/shops/:id`

2026-11-02 · `ui-redesign-page` + `ui-ux-pro-max` · commit `ui(/shops/:id): redesign`.

Preview: `http://localhost:4175/shops/00000000-0000-4000-8000-000000000001` with mock mode enabled.

## Complete component inventory

- Breadcrumb back to all deals and the current store name.
- Cover, logo, actual Open/Closed status, name, categories, deal count and rating/review count.
- Address, operating hours and pickup context. Deduplicate address segments already included in the address line.
- More store info / Collapse info, including description, address and hours.
- Flash deals / Reviews tabs, with keyboard ArrowLeft/Right/Home/End support through shared Tabs.
- Deal loading/error/retry/empty/normal states, detail links and client pagination (8 per page).
- Review header/refresh, actual aggregate score and breakdown, rating/photo filters, review cards, purchased product/variant, optional tier, photo modal, store replies and server pagination (10 per page).
- Whole-profile loading/error/retry/unavailable and partial data, broken media and long text.

## Shared UI and approved style

Uses exactly the home `DealGrid` → `DealCard` components with `isSameShop` for store context; there is no separate card design here. Three columns on wide screens, two on tablet and one on mobile follow the shared grid. View deal keeps the previous behavior of opening `/flash-deals/:id` so customers choose a variant before adding to cart. Legacy ShopDealCard is no longer used by this page.

Reviews use the existing `StoreReviewList` shared with detail. The component and its props are unchanged. MediaImage, Tabs, Pagination, Card, Badge, buttons, skeletons and empty/error states come from the shared UI foundation. No new primitive was needed.

Keep primary greens, Inter and light surfaces; category/info accents use indigo and rating/deal accents use warm amber. The store header is compact and the cover/logo have honest media fallbacks. No fabricated response rate, opening state, ratings or five filled stars: the hardcoded 96% response metric is replaced with pickup context supported by the current customer flow, and the score/count use actual API fields. Missing descriptions/hours/addresses use explicit fallback text.

## Services and behavior

Retains the same three existing read services: `shopApi.getShopProfile(shopId)`, `flashDealApi.getDealsByShop(shopId)` and `shopApi.getStoreReviews(shopId, { rating, page, pageSize: 10 })`. No service/DTO/backend/mock implementation or route contract changed. Full review aggregates are preferred for header rating/count when loaded, with profile fields as the initial fallback.

Profile, deals and reviews load independently as before. Async callbacks ignore results after unmount. Retry repeats the corresponding read; whole-profile recovery retries all three resources. Review rating changes reset to page one, photo filtering keeps the shared page-scoped semantics, and switching tabs retains pagination/expanded-info state. The existing usePagination hook keeps deal paging at eight items.

The `/shop-profile` route still has no ID and retains its pre-existing Store not found gate; that state is now styled consistently and regression-tested. This change does not invent an account/default-shop lookup for that legacy route.

## Skill guidance

Ran the design-system search for `storefront shop profile marketplace`, followed by `food shop detail catalog`. Neither returned a verified storefront information architecture (marketing/testimonial and funnel recommendations were not adopted). Project tokens and the approved home/detail style remain authoritative. Used the relevant `tabs keyboard navigation --domain ux` guidance and the existing shared tab implementation.

## Verification

- Targeted ESLint and `npm run build` pass. The three previous synchronous-effect lint errors in this page are resolved without suppressions; the existing bundle-size advisory remains.
- Seven storefront checks plus six shared-review checks pass: `npm run test:mock -- shop-redesign.spec.ts deal-reviews-redesign.spec.ts`.
- 360/768/1280px: no horizontal page overflow or application errors; expandable info, keyboard tabs, 8→2→8 deal paging, rating link/tab activation and navigation to the real detail route.
- Independent profile/deal/review failure and retry; profile recovery restores all resources. Slow loading, no deals, no reviews, closed store, broken cover/logo, missing optional metadata, long content, absent profile data and the legacy no-ID gate.
- Shared review suite covers star/photo filters, paging, photo modal keyboard/focus restore and error/empty/loading states on both detail and storefront.
- Screenshots reviewed: `/tmp/savebite-shop-{360,768,1280}.png`, review captures and state captures. Baseline: `/tmp/savebite-shop-before.png`. These are local verification artifacts.
