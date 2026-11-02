# Compact Account business pages

Routes redesigned: `/account/shop-application`, `/account/workspace`, `/account/staff-invitations`. Also refines density on the existing Payments/Refunds list/detail and all Trust Score tabs. Uses the approved white/light green-primary style, shared primitives and real backend only.

## Density decisions

- List/detail sections use 12–16px padding, 14px body headings and 40–48px identity images. AmountSummary uses smaller labelled numbers instead of large headline amounts. Payment/refund list actions share the amount row on wider screens; mobile wraps without shrinking touch targets. Desktop financial cards were checked below 260px (roughly 200px with current data).
- Payment/refund details use the shared breadcrumb for return navigation, removing duplicate All payments/All refunds rows. All payment/order/refund fields and conditional actions remain accessible.
- Trust overview score/level icons, numbers and padding are reduced; benefit/progress/history/penalty cards and tab cards are more compact. The fixed 32rem minimum tab-panel height is overridden only on the customer Trust Score page, avoiding oversized empty tabs. Existing equal level-section alignment remains.
- Buttons retain 44px targets. No fixed text-clipping heights, invented values, removed required information or smaller-than-caption financial text. FilterBar placement and native dropdown behavior are unchanged.

## Shop application coverage

- Latest application: real optional cover (compact 80px rather than a 192–240px decorative hero), logo/fallback/name/status/revision/submitted date; no separate duplicate h1.
- Status notice and existing three-step progress; optional description; address, opening/closing hours, business licence, masked bank/account holder and submission date all remain.
- Initial skeleton, read error/retry, no-application entry action, approved/read-only state and original pending/edit/resubmit guards. Cancellation now opens a shared confirmation modal before the same cancel API action; busy/close/error states remain explicit.
- Application status history remains independently loaded, with all status changes/admin notes, date/revision, expand/collapse, real snapshot location/coordinates/hours/payment configuration, revision document inheritance and document links, empty/error/loading/pagination. Layout/padding/tokens are compacted; snapshot fields and file link checks are retained.
- Latest initial loading uses the real request's asynchronous result with unmount guard. History content remounts for a changed refreshKey and resets retry UI from the retry handler, avoiding synchronous effect state updates. No service, DTO, status transition or payload changed.
- Separate `/new` and `/edit/:applicationId` form routes are retained and are not marked redesigned by this task.

## Workspace coverage

- Existing real associated-shop list, logo→cover→icon fallback, name/status/joined date, Open workspace route, Leave shop eligibility/action and five-item client pagination.
- Compact shared cards/buttons/badges and loading skeleton; true empty state. Read failures now have inline ErrorState/retry instead of appearing only as a toast beside an empty list.
- Shared Modal replaces the custom overlay for leave confirmation, with focus trap/Escape/restore, busy guards and inline failure; no removal occurs when merely opening it. The existing leave API and list reconciliation/success toast remain.

## Invitations coverage

- Real sorted invitation list with compact image fallback/shop link/status/address/deadline; pending-only Accept/Decline, all-response disabled guard while busy and existing success/auth refresh behavior; five-item pagination.
- Preserved 30-second/focus read refresh and deadline timer that makes expired invitations non-actionable. Initial read is scheduled at mount and cancelled on unmount; no fake clock or status data.
- Shared skeleton/error/retry/empty states, compact caption reminder and no manual Refresh button. Logo is preferred for compact identity, with cover fallback from the same DTO.

## Verification

- Targeted ESLint for all changed TypeScript and TypeScript/Vite production build pass; existing bundle-size advisory remains.
- Real authenticated backend data: approved latest application with one history entry, empty associated-shop list and one declined invitation. No frontend fixtures, request interception or fabricated roles/data.
- 360/768/1280px: financial list/detail, all four Trust tabs, approved application and expanded history snapshot, empty Workspace and declined invitation; no horizontal overflow or application errors. Expansion/collapse is functional; current approved application has no cancel action and declined invitation has no Accept action.
- Desktop native offline/retry checks restore latest application, Workspace and Invitations from genuine read failures. No payment/refund/order/application/invitation/staff business-write requests were sent.
- Data does not expose pending/revision/rejected/cancelled latest applications, populated Workspace/leave dialog, actionable invitations or a multi-page application history. Those layouts/guards/confirmations were reviewed in source, not forced with seeded frontend data. No application cancellation/resubmission, invitation acceptance/decline, staff departure, payment or refund mutation was performed.

Shop application spacing refinement: restored moderate 16–20px section padding, 56px identity image, 112–128px cover, 18px identity/history headings and more room around the notice/progress/details. Other compact account pages remain unchanged. Targeted ESLint passes.
