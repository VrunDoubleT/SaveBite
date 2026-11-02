# Account sizing consistency

Current pass: 2026-11-02. References: CustomerHomePage and CartPage. Retains the approved light surfaces, green primary and existing accents.

## Shared scale

- Main cards use `Card padding="compact"`: 16px mobile / 20px from sm. Split header/body cards use `padding="none"` with the same insets on their sections. Default Card spacing outside Account is unchanged.
- Section/list spacing: 16px. Nested panels: usually 12px. Trust benefit comparisons retain their compact insets and aligned subgrid sections.
- Page heading: existing 24px PageHeader. Card/store headings: 18px. Item titles/body: 14px. Metadata: 12px. Trust score: 24px; ordinary monetary totals: 14–18px.
- Shop list identity: about 40px; product thumbnails: 48px. Profile/application avatars: 56px. The approved application cover stays 112–128px.
- Buttons/form/filter controls retain 44px targets. Pagination retains the approved 36px controls and full-width Reviews layout.

## Coverage

Profile/address sections and editors; Orders and receipt/products/payment summary; Payments and detail; Refunds and detail; Reviews/replies/editor; all four Trust tabs; application/latest/history/new/edit form presentation; Workspace and Staff invitations. Sizes follow information hierarchy, without forcing unrelated cards to equal heights. API/DTO/auth/roles/realtime/business behavior is unchanged. Card variants are demonstrated in the DEV component reference.

## Verification

- Production build passes; existing bundle-size advisory remains.
- Changed files pass ESLint except ShopApplicationForm's two pre-existing `react-hooks/set-state-in-effect` errors in its preview/form-reset effects. These were reproduced against HEAD before this pass; its other lint rules pass. Hook behavior was left unchanged.
- Real browser checks pass at 360/768/1280px on 12 Account views plus actual order/payment/refund details: 15 routes per width, no document horizontal overflow and one page heading each. Density and wrapping were checked in screenshots.
- Profile/password/Review editors open and cancel correctly. No saves, uploads, payments, cancellations or application/invitation mutations were submitted.
- Current real data includes an approved application, inactive invitation, empty Workspace and no penalties. Populated Workspace/penalty/actionable invitation/application states and new/edit application forms were source-reviewed only. No synthetic responses or guard bypasses were introduced.
