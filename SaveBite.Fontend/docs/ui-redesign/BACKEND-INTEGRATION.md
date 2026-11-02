# Frontend backend integration after preview removal

The user requested removal of the frontend mock layer and its AI workflow stage. Runtime now uses the existing Axios services, real auth session and real SignalR client in both development and production.

Removed: src/mocks, fixture generator, MSW public worker/config/dependencies, Faker, preview-only startup/realtime branches, obsolete environment flags and dependent preview test suites. Orphaned scripts pointing to nonexistent test files were also removed. Existing UI source, tokens, customer pages, real services/DTOs and auth/cart stores are unchanged.

The optional DEV showcase has its own VITE_ENABLE_UI_SHOWCASE flag. Static primitive examples do not intercept API requests, assign roles or create fake sessions. The real checkout/simulation route remains an existing backend feature; removing the frontend preview layer does not remove API routes or server-provided payment behavior.

## Existing browsers

Bootstrap unregisters only the old worker at BASE_URL/mockServiceWorker.js. A page still controlled by that worker reloads before importing App. Other worker registrations and normal localStorage are retained. There is no worker registration or backend substitute left in startup. Old preview session data is no longer read; real credentials remain in their original storage keys.

## Validation

- TypeScript/Vite production build and targeted lint pass. Existing bundle-size advisory remains.
- No remaining frontend imports of the deleted layer, old flags, package dependencies or obsolete script/config references. No stale removed-document links remain.
- Browser smoke with legacy flags explicitly set true: /cart?role=user&mock=empty still reaches the real sign-in guard; no injected preview object/worker exists.
- Cart summary was requested through the existing Axios transport; the configured backend was unavailable during this check. Authenticated/data/payment/realtime integration against that backend is not claimed.
- Optional /__ui renders independently, without backend fixtures.
- Worker lifecycle check with temporary workers that have no fetch handlers: the old worker was unregistered, its controlled page reloaded, and a separate worker plus unrelated storage survived. Temporary files were removed after verification.

Follow [WORKFLOW.md](./WORKFLOW.md) for real-account/resource-ID acceptance checks when backend access is available. Prior preview-only browser results in page documents are historical, and their retired suites are not current real-backend verification.
