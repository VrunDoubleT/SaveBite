# Checkout — `/checkout`

2026-11-02 · `ui-redesign-page` + `ui-ux-pro-max` · `ui(/checkout): redesign`.

## Complete page inventory

- Shared PageHeader, back-to-cart navigation, pickup badge and the existing simulated-payment notice.
- Shop logo/fallback, linked name, address, actual item count and full read-only CartItemRow list. Edit items returns to cart.
- GPS request/refresh, accuracy/error feedback, route map/legend/background fallback, distance/motorcycle ETA, soft/hard deadlines and capped-buffer explanation.
- Pickup calculation rules remain available through a keyboard-accessible disclosure, including timezone and earliest deal end.
- Native labelled payment radios, online payment availability/restriction guidance and deposit/remaining balance descriptions.
- Full DepositPolicyPanel: tier/default-level explanation, eligibility/restriction, applicable fixed deposit, free threshold, complete deposit ranges table, upper/lower boundary explanation and policy snapshot rules.
- Responsive payment summary: total, selected method, due online, remaining at shop, quote countdown/expiry, availability warning, guarded primary placement action and verification/pickup explanation.
- Shaped initial skeleton, failed-read retry, missing shop/empty checkout recovery and placement-error link to My orders. Retains the distinction between retrying a read and repeating a potentially completed placement.
- `/checkout?orderId=…`: order-code/status/payment overview, payment-review/message warnings, products, totals, recorded policy, pickup deadlines, pending payment verification/redirect, cancellation confirmation and unavailable reason.
- Existing refund eligibility/request forms, bank/QR destination and email-code forms, refund history/evidence and review/create/edit dialogs remain present; their shared surfaces/colors now follow the tokens too. No refund/review field or action was removed.

## Approved style and reuse

Uses the existing light theme, Inter, primary green scale, subtle green shop header, indigo payment/info/tier accents and warm amber timing/availability accents. Shared Card, Badge, Button/ButtonLink, MediaImage, PageHeader, Skeleton, EmptyState and ErrorState provide the foundation. Read-only product rows are the same CartItemRow as cart.

Items now precede GPS, payment choices and complete payment policy. Summary sticks on desktop and follows the main content on small screens. Map failure replaces the blank map rectangle with a compact explicit fallback; GPS and time checks continue to work. Long shop names clamp with a tooltip, addresses truncate with a tooltip, product rows retain their shared long-text behavior and policy tables scroll locally when needed.

Receipt, pickup-code/deadline/status and cancellation components are shared with order-detail routes, so their visual updates also appear there. Refund and review components keep their existing shared contracts and validation. Those other route rows remain todo until their own redesign pass.

## API/data and preserved behavior

Context still loads for the selected shop and aborts on exit. GPS remains mandatory, high-accuracy, fresh and bounded by the original 1,000m accuracy check. Quote creation, expiry, stock/shop eligibility, online availability, method fallback, fixed deposit and remaining balance use the existing checkoutSelection helper. The place request retains its request-ID guard and selection checks; 403/409 clears the quote and rereads context. Safe payment redirects and cart refresh remain unchanged.

Receipt keeps order-specific realtime reconciliation, revision/operation guards, five-second pending-payment polling, payment verification and cancellation rules. Zero required online payment now has an explicit presentation label, including an unpaid pay-at-shop order; this only changes the displayed label. Review/refund processing and permissions remain unchanged.

## Skill guidance

The design-system query `food marketplace checkout form` suggested a marketing hero/testimonials pattern that does not fit this transactional page; it was not adopted. Used focused `form error loading feedback` guidance for shaped loading, clear pending actions and recoverable errors. Existing tokens and the approved cart/detail style govern the design.

## Current workflow and verification

The frontend uses the real backend only. Follow [WORKFLOW.md](./WORKFLOW.md) and the frontend AGENTS.md; do not recreate API fixtures, simulated authentication/realtime or seeded page data. Open this page with an actual account and real resource IDs from the backend.

The existing approved UI and its API/DTO contracts are retained. Earlier visual checks were performed during the redesign and are historical evidence, not verification against the current real backend. The retired preview-dependent suites have been removed. Validate current loading/empty/error/normal states with backend-supported data and run the production build and relevant lint checks. Record any unavailable backend/account/state explicitly rather than fabricating it.
