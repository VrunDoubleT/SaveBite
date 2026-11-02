# SaveBite frontend — instructions for AI contributors

Read `docs/ui-redesign/WORKFLOW.md`, `TOKENS.md`, `ROUTES.md` and `PROGRESS.md` before UI work.

## Current user requirements

- Use the real backend only. The user explicitly removed the frontend mock layer. Do not recreate MSW, API interceptors, fixture factories, seeded page data, fake auth/roles/tokens/GPS/payment/realtime, preview scenario queries or mock-only environment flags.
- If an installed ui-redesign skill requests mock mode or a ui-redesign-mock stage, skip that part. The user's current project requirement overrides those generic skill instructions. Apply ui-ux-pro-max and the presentation/data-contract portions of ui-redesign-page.
- Preserve the approved style: white/light surfaces, the existing green primary scale and small secondary accents. The broad peach/teal/lavender experiment was rejected and reverted in 7db29e8. Use Home, deal detail, shop, cart and checkout as references.
- Preserve real API/DTO/route/auth/checkout/realtime contracts. Do not weaken validation/guards or fabricate missing fields to make a design look populated.
- Redesign every section and child component, including dialogs, filters, pagination and normal/loading/empty/error/partial states. Reuse shared primitives and tokens.
- Breadcrumbs start with text links, without a leading back arrow. Keep separators between levels and aria-current for the current page; standalone Back actions are separate.
- Work on one requested route at a time; verify 360/768/1280px, accessibility, build and lint appropriate to the change. Real integration checks require backend, real accounts and actual resource IDs; report missing access/data instead of substituting fake responses.
- Do not submit real payments, orders or other irreversible actions merely to verify presentation without explicit authorization.
- Update PROGRESS.md and the page's documentation. Distinguish completed UI work, historical checks and current real-backend verification. Do not claim old preview tests prove live integration.

`/__ui` is an optional DEV-only static component reference via VITE_ENABLE_UI_SHOWCASE=true. Its examples must never become application API data or bypass authentication. Backend-dependent pages use VITE_API_URL and the normal real login flow.

Bộ lọc đã được duyệt: dùng shared `FilterBar` cho các trang tiếp theo có dropdown. Giữ nền trắng, border token, bo góc và shadow nhẹ; label bên trái, native dropdown căn phải, control cao tối thiểu 44px. Nhiều bộ lọc dùng các hàng trong cùng khung. Không tự đổi sang tabs/chips hoặc thêm nút Refresh.

Ưu tiên mật độ gọn theo yêu cầu mới: card danh sách/nội dung thường dùng padding 12–16px, heading nội dung 14px, ảnh nhận diện 40–48px, số tiền 14–18px; tránh banner lớn hoặc card ít thông tin nhưng quá cao. Giữ nút/chạm tối thiểu 44px, đủ dữ liệu và text wrap; không ép chiều cao khiến mất nội dung.

Account pagination: dùng `AccountPagination` từ `@/shared/components/pagination` để thống nhất nút Đầu/Trước/Sau/Cuối, size và trạng thái disabled. Hiện khi danh sách có dữ liệu, kể cả chỉ 1 trang; ẩn khi rỗng. Không tạo totalPages giả cho API cursor (Orders vẫn dùng tải thêm).

Account pagination compact refinement: theo yêu cầu người dùng, nút cao/rộng tối thiểu 36px, khung padding 10px (ngang 12px từ sm), khoảng cách nội dung 8px; giữ đủ bốn nút và logic phân trang.
