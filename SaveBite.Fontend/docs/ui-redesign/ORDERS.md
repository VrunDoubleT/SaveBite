# Customer orders — `/account/orders` and `/orders/:orderId`

Uses ui-redesign-page and ui-ux-pro-max with the approved light surfaces, existing green scale and small semantic accents. Real backend only.

## Account headers

AccountLayout renders the shared PageHeader with breadcrumb, visible title and existing description. The temporary removal of this shared heading was corrected after user clarification; only duplicate page-specific introductions are removed. Removed redundant introductory blocks from Orders, Reviews and Trust Score; removed the payment introduction and refund-detail introduction. Invitations retains its expiry information and Refresh action without its duplicate title. Resource names, form instructions, empty/error messages and section headings remain meaningful content. Sidebar routes, role guards and the global customer header are unchanged. Other account pages are not marked redesigned by this cleanup.

## List and detail

- List: one compact row containing the complete native status select with API counts and Refresh; contextual loaded count and GMT+7 reminder. Duplicate quick-filter controls were removed. Existing URL status values remain supported.
- Order cards: shop/logo/address, separate order/payment badges, code/date, product preview, review/edit action, count, total/paid/due, detail link and unreviewed-completed-order rating action. Pickup deadlines stay visible for active orders; historical pickup times expand with a native details/summary control.
- Loading skeletons, genuine empty-filter recovery, read-error retry, existing cursor observer and an additional manual Load more action remain accessible. Refresh/retry restarts the existing history reader; no placeholder orders or counters.
- Detail: Home › My orders › Order details breadcrumb, pickup code/date and a compact cancellation button in the pickup card, independent order/payment badges grouped with deadlines, expandable status explanations, pickup information before products, shop/all products, payment amounts/method/required-online/remaining-at-shop and existing checkout policy. Payments requiring review and backend payment messages remain explicit.
- Existing payment continuation, cancellation eligibility and modal confirmation, expandable refund eligibility/cards/request/destination/evidence, rating/edit modals and image fallbacks are retained. Shared OrderItems and OrderPaymentAmounts keep list/detail presentation consistent; missing feedback IDs disable the edit action instead of showing an active no-op button.
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
- Desktop: all 11 status values, URL filter persistence, completed results, native offline failure and successful read retry; historical Account breadcrumb/screen-reader heading regression (visible shared headings restored in the follow-up below) on Profile, Reviews, Payments, Refunds, Trust Score, Workspace, Invitations and Shop Application.
- Screenshots reviewed on desktop/mobile for order cards, detail and the existing review modal. Additional code review covers long text, missing images, deadlines and all existing conditional components.
- Available orders were completed and already reviewed. Active preparation/payment, urgent pickup, new-rating, cancellation-confirmation, refundable-balance/request/destination and policy-snapshot variants were not forced with fabricated data. No new order, payment, cancellation, refund, review update/delete or upload was submitted. Live event delivery and successful business writes remain unverified in this run.

## Compact layout follow-up

- Restored the shared Account title/description beside the breadcrumb area across all Account routes. Duplicate inner page introductions remain removed.
- List filters use a single compact row rather than a padded card plus repeated quick filters. All 11 status values/counts and URL filtering remain available.
- Receipt uses two desktop columns: pickup code/date, compact status badges/deadlines and products on the left; payment summary and expandable Refunds on the right. Mobile stacks these sections. Full status explanations remain accessible under “About these statuses”.
- Replaced the cancellation card with one button in the pickup header and a shared accessible Modal. Backend eligibility/deadline checks still gate the destructive confirmation. For unavailable cancellation, the button opens the actual unavailability reason without a confirm action. No cancellation request is sent just by opening it. Busy state prevents closing/submitting again.
- Refund details/forms/history are retained in a native disclosure; existing-refund count/active indicator remains visible in its summary, and read errors stay visible when collapsed.
- Targeted ESLint and production build pass. Real authenticated checks at 360/768/1280px cover restored Orders title, select filtering/empty recovery, grouped status placement, status/refund expansion, unavailable cancellation dialog/Escape/focus return and zero page overflow or business writes. Desktop also confirms restored visible shared titles on Profile, Reviews, Payments, Refunds, Trust Score, Workspace, Invitations and Shop Application. Active cancellation confirmation/refund mutations remain code-reviewed rather than exercised against completed account orders.

## Refund request usability follow-up

- Refund management now occupies the full receipt width beneath the main columns. Existing destination/bank/QR/history flows therefore no longer inherit the narrow payment sidebar. The section retains its disclosure and eligibility guards.
- Eligible requests open shared Modal at max-w-4xl, with scroll, Escape/focus restoration and close blocked during submission. Request/server errors appear inside the modal; errors outside a visible request remain on the receipt. Existing one-request/6-hour/balance/active-refund conditions remain unchanged.
- Each item contains its selection checkbox, actual image/name/variant/purchased count, integer refund quantity, calculated amount, reason and local evidence picker in one section. Removed nested interactive quantity inputs inside checkbox labels. Visible reason labels/counts, per-item errors and photo requirements keep inputs next to their context.
- Sticky footer shows selected item count/amount and Cancel/Submit actions. Invalid submissions scroll to the first inline error. Input/focus styling reuses the Login recipe. Photo removal controls have 44px targets; file type/5 MB/count limits, local URL cleanup, distinct-photo validation and upload-on-submit behavior remain unchanged. No endpoint/DTO/payload changed.
- Targeted ESLint and production build pass (existing chunk-size advisory). Real completed-order refund sections render without horizontal overflow at 360/768/1280px. Checked 10 actual completed orders on desktop for an eligible request; none exposed the request action, so modal/form interaction and successful request/upload could not be exercised with current real data. Form layout/validation were reviewed in code, with no fabricated eligibility or replacement API responses. No refund/payment/order mutations or uploads were sent.

## Dedicated refund request route

The request action now links to `/orders/:orderId/refund-request` within the existing authenticated customer layout, replacing the request Modal. The page loads the actual order/refunds, shares eligibility calculations with the receipt, supports loading/error/retry/unavailable states, and shows shop/order/deadline context plus the existing item/evidence form. Breadcrumb and Cancel return to order details. The footer now sticks to the page viewport bottom.

Before uploading/submitting, the page rereads authoritative order/refunds and checks the same completion/deadline/balance/active/request rules. A busy guard prevents duplicate submission. Errors retain the form; success navigates to the returned refund ID’s existing detail route with a success toast. API endpoints/payloads and the one-request policy remain unchanged. Existing refund management stays on the receipt.

Verification: targeted ESLint and production build pass. A real order became eligible during this run. Authenticated direct-route checks at 360/768/1280px confirm the actual eligible form, empty-selection validation, item selection/quantity, required reason/evidence feedback, draft typing, breadcrumb/title, no request modal or horizontal overflow, and Cancel returning to the actual order details. No upload or valid request was submitted; successful submission remains unverified. No artificial eligibility or API responses were introduced.

## Refund placement and request-page alignment correction

Per user feedback, the receipt Refunds section/action is back beneath Payment summary in the right column. The separate request route remains unchanged. Its title, context and form now align with the normal customer content frame rather than a narrower centered 4xl frame. Mobile quantity labels wrap and the sticky action footer stacks into full-width actions; desktop retains an inline footer.

Targeted ESLint and production build pass. Real eligible-order checks at 360/768/1280px verify Refunds shares the payment-column wrapper, Request refund opens the standalone route, form/card aligns with the page heading, item selection/draft input fits without horizontal overflow, and Cancel returns to the actual order. No upload or valid refund submission performed.

Removed the manual Refresh button from the order-list filter row at user request. Existing realtime updates, read-error retry and refresh after review changes remain. Targeted ESLint passes.

Order-filter visual refinement: replaced the native dropdown with one horizontally scrollable status strip. All 11 existing filters/counts remain; selected filter has a mint surface, green underline and contrasting count badge. aria-pressed, native button keyboard interaction and contained scrolling preserve accessibility without page overflow. Selected filter scrolls into view on mount. No Refresh button restored. Real 360/768/1280px checks cover status selection, genuine empty result and return to all orders; lint/build pass.
