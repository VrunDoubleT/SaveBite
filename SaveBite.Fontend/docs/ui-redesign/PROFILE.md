# Profile — `/account/profile`

`ui-redesign-page` + `ui-ux-pro-max`. Uses the approved white/light, green-primary design and the real backend only.

## Complete component inventory

- Account PageHeader and Home › Profile breadcrumb, without a leading icon; existing role-aware account sidebar/navigation.
- Identity header: actual name/email, avatar/initial/broken-photo fallback and Active/Suspended badge. Standard Edit profile and Change password actions.
- Personal information: full name, nullable phone, email and account status. Long values wrap/clamp with title text; missing phone stays explicit.
- Profile edit: name/phone fields, existing client/server validation, avatar picker/preview, original JPG/PNG/WebP and 10 MB limits, invalid-file feedback, pending/save/cancel and success feedback.
- Change password: current/new/confirm fields, existing reveal controls, validation/rules, server errors, disabled/pending actions and focus returned to the opener on cancel.
- Addresses: load skeleton, failed-read retry, empty discovery action, actual list, labels/full address/coordinates, default badge, add/edit, set-default/delete pending/error/success and original default-address guards.
- Complete address form: existing create/update schemas, label/street/ward/district/city, latitude/longitude, first/default-address lock, GPS/reverse-geocode phase/accuracy/error, map/click/drag/map-center action, field/server errors and save/cancel.

## Shared UI and approved style

Card, Badge, Button, IconButton, Input, FormField, Avatar, ErrorState, EmptyState, Skeleton and PageHeader come from the shared foundation. Profile identity has the same understated mint header as cart/shop sections; address/password icons use small indigo accents. Surfaces stay white/light; primary green remains unchanged. Controls use the existing rectangular shared styling rather than separate pill recipes.

Avatar gains an optional className for the larger profile size; its default header size/initial/broken-image behavior remains compatible. AccountLayout now uses the shared PageHeader/breadcrumb for its existing title/description, preserving sidebar routes and the 900px sidebar breakpoint. This shared heading also appears on other account routes; those pages remain todo. ProfilePage keeps all existing profile/address components.

## Data contracts and behavior

Real profile calls retain `profileApi.updateProfile({ fullName, phone })`, optional multipart avatar upload, then auth-store `refreshUser()`. No real service, DTO, schema, API endpoint, route or authentication behavior changed. Profile errors previously swallowed by the parent now reach the form's existing server/field-error handling; the form stays open with its input on failure. Success feedback follows the complete successful update/refresh sequence. This does not claim the two sequential update/upload operations are atomic.

AuthUser fields displayed: fullName, email, phone, avatarUrl, customerStatus. Password requests still send currentPassword/newPassword; confirmPassword remains client-side validation.

Address fields remain id, label, addressLine, ward, district, city, latitude, longitude and isDefault. Existing get/create/update/set-default/delete calls and list reconciliation are preserved. Loading retry repeats the existing read; Add is disabled while the initial list is loading. Default addresses remain non-deletable in the UI, and first/current-default flags stay locked by the existing form rules. Coordinate parsing, reverse-geocode cancellation/revision guards and map event handling are unchanged. The initial map center is still explicitly not the user's detected location.

## Skill decisions

Design-system searches for `account profile settings form` and `account settings form sections` suggested marketing/demo layouts and alternative fonts/colors that do not fit this account page; those parts were not adopted. Relevant form feedback/recovery guidance informs inline errors, visible pending actions and retry. Existing tokens, approved pages and the backend-only workflow remain authoritative.

## Verification with the real backend

- Targeted ESLint and TypeScript/Vite build pass. Existing bundle-size advisory remains.
- Signed in using the user-provided test account against localhost:8080; credentials/tokens were not written to the repository or this document.
- 360/768/1280px: authenticated profile and addresses render from the backend, one Profile h1, no horizontal page overflow or application errors.
- Open/cancel edit; required-name/invalid-phone validation; rejected avatar file; input retained until cancel. No profile update/upload was submitted.
- Open/cancel password form; focus on current password, required-field validation, reveal toggle and focus restoration. No password-change request was sent.
- Open/cancel new address; required street/coordinates validation. Edit the actual default address; default checkbox remains locked; browser-native GPS permission denial produces the real error while manual fields remain usable. No address create/update/default/delete request was sent.
- Read-only network checks distinguish the ordinary SignalR negotiate handshake from data mutations. Profile/address/password mutation request count was zero during UI verification.
- Initial/empty/failed-read/server-write errors and suspended/long readonly data are implemented/reviewed but not forced with fabricated backend responses. Successful saves, uploads, password changes and address mutations were intentionally not exercised on the real account.
- Screenshots reviewed: `/tmp/savebite-profile-before.png`, `/tmp/savebite-profile-{360,768,1280}.png` and edit/password/address form captures. These are local verification artifacts, not application seed data.
