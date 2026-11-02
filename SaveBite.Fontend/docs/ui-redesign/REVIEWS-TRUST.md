# Account Reviews and Trust Score

Routes: `/account/reviews`, `/account/trust-score/:tab?` (overview, levels, history, penalties). Uses ui-redesign-page and ui-ux-pro-max, the approved light surfaces/green primary and small secondary/amber accents. Real backend only.

## Reviews and edit coverage

- Review cards: real shop/logo, order receipt link, GMT+7 date, actual product/photo/variant, existing verified-order text, rating, optional comment/photos, shop reply/actor/date and Edit/Delete actions. Long text wraps; MediaImage handles unavailable images. Existing feedback-ID deep-link highlighting and real pagination remain.
- Inline edit and the order-page EditReviewModal now reuse feature-level ReviewEditor: labelled feedback/count, rating with explicit selected state, current photos/zoom/remove/add, upload information, pending/disabled actions, inline save errors, shop reply and Cancel/Save. Inline editor retains its original 2,000-character limit; modal retains its original 1,000-character limit. Existing five-image and 5 MB rules, upload-on-selection behavior and trimmed payloads remain. No image is uploaded merely by opening the editor.
- Inline Cancel restores original view data and focus to Edit review. Modal closes/discards its mounted draft; a keyed content component loads the real feedback when opened, supports read-error retry and avoids stale drafts from another feedback ID. Parent close/Escape/backdrop is blocked during save/upload.
- Shared Modal provides review-photo zoom/Escape/focus return for view/edit; removed custom untrapped lightboxes. Star controls and image removal controls meet 44px targets and use tokens.
- List skeletons, genuine empty state and completed-order action remain. Failed initial history reads now show ErrorState/retry rather than an empty-review message. Delete confirmation retains its existing API flow, shared danger/secondary pending buttons and inline error; no delete happens just by opening it.
- Existing successful-update/delete toast/list reconciliation remain; failed inline saves are caught and displayed in the editor rather than leaving a rejected event-handler promise. Deep-link highlight starts with its scheduled scroll; removed synchronous effect-state reset by mounting modal content per open feedback.

## Trust Score: all four tabs and children

- Overview: actual rank/name, score as points (no invented maximum), standing/lock message, completed orders, accumulated order value, active penalties, updated time and listed benefits. Next-level progress shows backend overallPercent and every current/required/remaining metric; achieved conditions have explicit text/icon, missing conditions state how much remains. Existing highest-level, missing-benefit, independent progress read-error/loading and active order-completion rule branches remain.
- Levels & benefits: actual rank/name, current/unlocked/locked state, description/benefits, all existing pay-at-shop/full-online/deposit-free/range/upper-limit policies and all score/order/value requirements. Current level has a light green cue; locked levels remain readable with a lock/text label. Grid items align at their tops rather than stretching short content into excess whitespace. No new membership names, perks or prices were invented.
- Score history: approved shared FilterBar for event, real paginated entries with event/date/reason/order, signed delta, before/after score and order link. Responsive cards replace the horizontal table without dropping fields. Empty/error/loading and invalid-page redirect remain.
- Penalties: approved FilterBar, empty/error/loading/pagination, real no-show occurrence/date/order/reason/points/status, recovery order count/value and existing recovery modal. Permanent offline lock is explicit when returned. Recovery modal retains overall/per-metric progress, recovered state/time, lock notice and counted orders/value/date; long rows wrap.
- Four-card tab navigation uses shared Tabs with icons/descriptions, URL navigation and selected state; keyboard ArrowLeft/Right/Home/End, roving focus and one labelled tabpanel remain. Default/unknown/trailing-slash paths redirect to overview as before. Shared default line-tab appearance is retained for other consumers; card appearance has a DEV-only showcase example.
- ProgressBar wraps long labels/values, clamps non-finite percentages for rendering, exposes value text and respects reduced motion. Shared trust PanelState now uses the common skeleton/error/empty primitives; shared admin consumers receive the same presentation, without changed calls or rules.

## Contracts

No service, endpoint, DTO, schema, auth/role guard, real earning/recovery/deposit rule, page size or payload shape changed. Reviews still use feedbackApi history/detail/edit/delete/uploadImage. Trust uses the existing overview/level-progress/levels/history/penalties/recovery GET calls, useApiResource user-keyed cache and useListParams URL filters/page clamping. No fixtures, seeded frontend data, fake eligibility, role/session injection or replacement API responses.

## Verification

- Targeted ESLint on all changed TypeScript and TypeScript/Vite production build pass; pre-existing large-chunk advisory remains.
- Real account/backend: 14 reviews across two pages, Bronze membership, five configured levels, three next-level metrics, two score events and no penalties in the available data.
- 360/768/1280px: Reviews list, inline editor typing/rating change then Cancel/focus restore; delete dialog then Cancel; both real review pages; feedback-ID deep link; real order review modal with the original 1,000-character limit then Cancel. No update/delete/upload was submitted.
- All three widths: four Trust tabs, actual progress/level requirements, keyboard tab change/focus, one panel, event NoShow empty filter and recovery to actual history, penalty Recovering empty filter and no page overflow/application errors. Desktop checks default/unknown-tab redirects. Photos were opened/Escape-closed where present; dialogs retain focus restoration.
- Native offline/retry checks pass for Reviews history, EditReviewModal detail read, Trust overview and independent next-level progress. Network assertions record zero feedback/trust business-write requests during UI validation.
- Current data does not expose a highest-level account, restricted/permanently locked account, populated penalties/recovery modal or successful edit/delete/upload. Those conditional views and rules were reviewed in source; live integration remains unverified. No penalties/reviews were created merely to populate them. Trust history/penalty multi-page navigation is not asserted because available results fit one page or are empty; original pagination logic remains.

Benefits policy refinement: Pay at shop policy is now a two-column definition list separating order-value ranges from the required payment. No-deposit and full-online thresholds are distinct; fixed-deposit amounts are highlighted with a caption. Full-online-only levels use a concise icon/message panel. All returned ranges, thresholds and amounts are retained. Real 360/768/1280px rendering checks, targeted lint and production build pass.

Level-height refinement: desktop/tablet level cards share header, Benefits, policy and Requirements row tracks through CSS subgrid. Paired cards and policy panels have equal heights with aligned section starts; no fixed text-clipping heights. Mobile remains one natural-height column. Real 360/768/1280px checks pass; bounding-box assertions verify matching paired card/policy heights and aligned Requirements at 768/1280px. Targeted lint/build pass.

Compact-height follow-up: reduced level-card header/section padding, icon size and policy-row padding; consolidated repeated deposit captions into one policy subtitle; removed padded requirement tiles in favor of compact lines. Shared row alignment remains, with all monetary thresholds/requirements visible and no fixed clipping heights. Targeted ESLint passes.

Compact-density follow-up: reduced requested card/section padding and visual sizes; real responsive regression checks pass. See ACCOUNT-BUSINESS.md for current changes, including duplicate back-action removal on finance details and reduced Trust panel minimum height.

Reviews pagination width correction: removed the centered flex wrapper that shrank the pager and explicitly set full width; compact shared control sizing retained. Targeted ESLint passes; no new real-browser check for this CSS-only correction.
