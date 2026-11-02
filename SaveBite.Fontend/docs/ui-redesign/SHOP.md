# Storefront — `/shops/:id`

2026-11-02 · `ui-redesign-page` + `ui-ux-pro-max` · commit `ui(/shops/:id): redesign`.

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

Profile, deals and reviews load independently as before. Async callbacks ignore results after unmount. Retry repeats the corresponding read; whole-profile recovery retries all three resources. Review rating changes reset to page one, photo filtering keeps the shared page-scoped semantics, and switching tabs retains pagination/expanded-info state. The existing usePagination hook keeps deal paging at eight items.

The `/shop-profile` route still has no ID and retains its pre-existing Store not found gate; that state is now styled consistently and regression-tested. This change does not invent an account/default-shop lookup for that legacy route.

## Skill guidance

Ran the design-system search for `storefront shop profile marketplace`, followed by `food shop detail catalog`. Neither returned a verified storefront information architecture (marketing/testimonial and funnel recommendations were not adopted). Project tokens and the approved home/detail style remain authoritative. Used the relevant `tabs keyboard navigation --domain ux` guidance and the existing shared tab implementation.

## Store identity alignment refinement

The logo now sits directly beside the name/category block in a normal flex row, with a 12–16px token gap. Removed the absolute cover overlap and reserved top/left padding that created the empty space. The logo is 64px on mobile and 80px from tablet upward; name wrapping and the separate rating action remain responsive. Build/targeted lint and four layout/long-content storefront checks passed at 360/768/1280px.

The More store info / Collapse info control is centered and uses a transparent, borderless appearance, including hover; its keyboard focus indication and expansion behavior remain intact.

## Current workflow and verification

The frontend uses the real backend only. Follow [WORKFLOW.md](./WORKFLOW.md) and the frontend AGENTS.md; do not recreate API fixtures, simulated authentication/realtime or seeded page data. Open this page with an actual account and real resource IDs from the backend.

The existing approved UI and its API/DTO contracts are retained. Earlier visual checks were performed during the redesign and are historical evidence, not verification against the current real backend. The retired preview-dependent suites have been removed. Validate current loading/empty/error/normal states with backend-supported data and run the production build and relevant lint checks. Record any unavailable backend/account/state explicitly rather than fabricating it.
