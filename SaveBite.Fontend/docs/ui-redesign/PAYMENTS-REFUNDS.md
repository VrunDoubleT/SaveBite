# Customer Payments and Refunds

Routes: `/account/payments`, `/account/payments/:paymentId`, `/account/refunds`, `/account/refunds/:refundId`. Uses ui-redesign-page + ui-ux-pro-max, the approved light/green style, existing semantic tokens and real backend only.

## Approved reusable filter format

Shared `FilterBar` provides a white surface, token border, rounded corners and subtle shadow; the label stays left and a native dropdown aligns right. Controls are at least 44px high. Multiple filters occupy rows within the same surface. It is used by Orders, Payments and Refunds and recorded in AGENTS.md, TOKENS.md and WORKFLOW.md for future pages. No Refresh button or replacement tabs/chips were added.

Shared `AmountSummary` renders labelled requested/collected/transferred/order amounts with normal wrapping, tabular numbers and small green/amber accents. Both additions have DEV-only showcase examples; examples never become API data or authentication state.

## Page/component coverage

- Payments: shared status filter, real total count/GMT+7 hint, loading skeletons, empty state with Orders link, ErrorState retry, shop image fallback/name/order code, payment type, status, created/paid timestamps, requested/collected amounts, detail link and existing pagination/URL page clamping.
- Payment detail: one shared account heading and text breadcrumb Home › Payments › Payment details; All payments action, payment type/status/ID/method/timestamps, requested/collected amounts, associated order link/code, actual shop/address/items/variants/quantity/unit price/subtotal, independent order/payment badges, recorded total/paid/due and pickup deadlines. Missing paidAt stays “Not recorded”.
- Refunds: two shared filter rows for type/status; URL filters reset page; count/timezone, order-origin request link, skeleton/error retry/empty state, shop/order links, status/overdue, type/date, requested/transferred amounts, customer-details deadline hint, detail link and original pagination/realtime updates.
- Refund detail: one shared heading and Home › Refunds › Refund details breadcrumb, All refunds action, actual shop/code/request time/status. Outcome and next action precede progress/items. Two-column amount/request information on wide screens; destination, transfer and history sections use full content width. Mobile progress uses a two-column grid instead of compressed labels. Destination keeps masked account number, email-confirmation status, real bank/holder, missing-field fallbacks and existing conditional forms.
- Refund detail children retain item quantity/purchased count/reason/evidence/net order amount; transfer reference/time/proof; reverse chronological decision/actor/note/time/proof history; empty history; deadline/decline/withdrawal/required-shop-refund branches. Withdrawal confirmation now uses shared buttons and states the one-request consequence. No withdrawal occurs when opening/cancelling confirmation.
- Refund destination form uses shared Login-style inputs, shared pending/disabled buttons, clear active bank-vs-QR mode and 44px controls. Existing bank lookup/retry, field validation, QR upload/detection, email send/resend/code confirmation rules remain. The bank picker shares the same focus recipe. Evidence thumbnail buttons reuse MediaImage fallback; the existing accessible modal/Escape/focus restoration and HTTPS filtering remain.
- Mobile shop identities receive a full row so names are not squeezed by status badges. Long values wrap and financial IDs break safely. Account sidebar/global header/role guards are unchanged.

## Contracts preserved

CustomerPayment, CustomerPaymentPage, CustomerPaymentDetail, Refund and RefundPage remain unchanged. No service, endpoint, API payload, monetary fallback, checkout/refund eligibility, auth role, file limit or realtime event contract changed. List filters still use the original status/type strings and page query. Existing payment detail amounts remain `paidAmount ?? 0` and `outstandingAmount ?? 0`; requested payment money and collected money are displayed separately rather than inferred from status.

Shared RefundItems, RefundDestinationForm, RefundBankSelect and evidence rendering also appear in the existing order-refund flow; those views receive consistent presentation without changing actions. PaymentStatusBadge now uses shared semantic Badge tones for existing statuses. AccountLayout recognizes payment/refund detail routes and preserves a single shared h1 with proper breadcrumb levels; other account headings retain their original behavior.

## Validation and limits

- Targeted ESLint on all changed TypeScript files passes. TypeScript/Vite production build passes; the pre-existing large-chunk advisory remains.
- Real authentication/backend used. Available account data: three payments (paid and pending) and one withdrawn customer refund. No fixtures, response replacement, role/session fabrication or seeded frontend values.
- 360/768/1280px: both lists/details, correct single shared headings/breadcrumbs, filters and genuine empty results, return to results, refund type filter, pending payment “Not recorded”, evidence dialog open/Escape/focus restoration, no horizontal overflow or application errors.
- Desktop: page=999 clamps through the real backend response to page=1; native offline list failure and retry for both resources; offline SPA detail navigation and retry for both; Orders shared-filter/no-Refresh regression. Guest refund detail still requires login.
- Network assertion: zero payment/refund/order business-write requests. No real payment, refund submission, bank destination save, email send/code confirmation, upload or withdrawal performed.
- Current data does not expose an active refund destination form, new withdrawal confirmation or shop-transfer/declined/overdue variants. Those branches were reviewed in source; their live interactions and successful writes remain unverified. Multi-page pagination navigation is not asserted because available lists have only one page; controls and existing query logic are retained. Live event delivery is not forced.

Filter layout refinement: Refund type and Refund status share one horizontal row from tablet width via FilterBar inline mode. Small screens stack the groups; other filter bars keep their existing layout. Targeted ESLint passes.

Refund count/action spacing: reduced the visible action row from 44px to 24px so spacing matches nearby metadata; the link keeps a 44px pointer target using an expanded pseudo-element. Targeted ESLint passes.
