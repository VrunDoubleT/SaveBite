# Customer orders — `/account/orders` and `/orders/:orderId`

Uses ui-redesign-page and ui-ux-pro-max with the approved light surfaces, existing green scale and small semantic accents. Real backend only.

## Account headers

AccountLayout now renders the existing text breadcrumb and a screen-reader h1, without a visible page title/description banner. Removed redundant introductory blocks from Orders, Reviews and Trust Score; removed the payment introduction and refund-detail introduction. Invitations retains its expiry information and Refresh action without its duplicate title. Resource names, form instructions, empty/error messages and section headings remain meaningful content. Sidebar routes, role guards and the global customer header are unchanged. Other account pages are not marked redesigned by this cleanup.

## List and detail

- List: complete native status select with API counts, four quick filters, explicit selected state, refresh, contextual loaded count and GMT+7 reminder. Existing URL status values remain supported.
- Order cards: shop/logo/address, separate order/payment badges, code/date, product preview, review/edit action, count, total/paid/due, detail link and unreviewed-completed-order rating action. Pickup deadlines stay visible for active orders; historical pickup times expand with a native details/summary control.
- Loading skeletons, genuine empty-filter recovery, read-error retry, existing cursor observer and an additional manual Load more action remain accessible. Refresh/retry restarts the existing history reader; no placeholder orders or counters.
- Detail: Home › My orders › Order details breadcrumb, prominent pickup code/date, full independent order/payment state descriptions, pickup information before products, shop/all products, payment amounts/method/required-online/remaining-at-shop and existing checkout policy. Payments requiring review and backend payment messages remain explicit.
- Existing payment continuation, cancellation eligibility/confirmation, refund eligibility/cards/request/destination/evidence, rating/edit modals and image fallbacks are retained. Shared OrderItems and OrderPaymentAmounts keep list/detail presentation consistent; missing feedback IDs disable the edit action instead of showing an active no-op button.
- Terminal-order recommended-arrival copy describes the recorded time rather than telling customers to arrive for an already-ended order. Active pickup urgency logic is unchanged.

## Contracts and behavior

CheckoutOrder and OrderHistoryPage remain authoritative. No endpoint, DTO, schema, role/auth behavior, money formatter, payment redirect validation, cursor reconciliation or realtime subscription contract changed. Product, code, address, deadline, status, policy, review and refund fields come from their existing sources.

OrderPaymentAmounts retains the previous list formula: paidAmount (fallback to successful requiredOnlineAmount), outstandingAmount (fallback to total minus paid except terminal cancellation/expiry/rejection/no-show). The same display now appears in the receipt; it does not reconcile inconsistent financial records or infer cash collection. In the available completed-order data, requiredOnlineAmount/paidAmount can be zero while outstandingAmount remains positive; those backend values are shown as returned.

Receipt read refresh now clears a stale read error after a successful real response. All mutation guards and confirmation flows remain in place. The receipt is also reused by checkout completion, so that view receives the same refreshed presentation.

## Verification

- Production TypeScript/Vite build passes with the existing large-chunk advisory.
- Targeted ESLint passes for the redesigned orders, shared components and header-cleanup files except StaffInvitationsPage: its pre-existing react-hooks/set-state-in-effect error at loadInvitations was reproduced by linting the HEAD version. That logic was not changed or suppressed.
- Authenticated against the supplied local backend/account; no credentials or tokens added to the repository.
- At 360/768/1280px: real list/detail, no page overflow or application errors; open historical pickup times, switch quick filters and recover from an actual empty AwaitingPayment filter; navigate actual order links, inspect refunds and disabled cancellation on completed orders.
- At all three sizes: load all 12 existing orders through the real cursor path, open existing review details, then close without saving. No order/payment/refund/feedback mutation requests were sent.
- Desktop: all 11 status values, URL filter persistence, completed results, native offline failure and successful read retry; Account breadcrumb/screen-reader heading regression on Profile, Reviews, Payments, Refunds, Trust Score, Workspace, Invitations and Shop Application.
- Screenshots reviewed on desktop/mobile for order cards, detail and the existing review modal. Additional code review covers long text, missing images, deadlines and all existing conditional components.
- Available orders were completed and already reviewed. Active preparation/payment, urgent pickup, new-rating, cancellation-confirmation, refundable-balance/request/destination and policy-snapshot variants were not forced with fabricated data. No new order, payment, cancellation, refund, review update/delete or upload was submitted. Live event delivery and successful business writes remain unverified in this run.
