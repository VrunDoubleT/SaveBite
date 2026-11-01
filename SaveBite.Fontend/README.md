# SaveBite frontend

React + TypeScript + Vite, Tailwind v4, Zustand và React Router.

## Chạy với mock backend

```bash
npm install
cp .env.example .env.local
# Sửa VITE_USE_MOCK=true trong .env.local
npm run dev
```

Hoặc trên WSL/Linux:

```bash
npm run dev:mock
```

Không cần backend, database hay Docker. Chế độ mock chỉ hoạt động khi **development và `VITE_USE_MOCK=true`**. Tắt bằng `VITE_USE_MOCK=false` rồi khởi động lại Vite để dùng API thật từ `VITE_API_URL`. Build production luôn dùng backend thật.

Có thể mở:

- `/?role=user` — khách hàng; `/login?role=guest` — xem form đăng nhập. Mock backend nhận mọi credentials gửi lên; validation trên form thật vẫn giữ nguyên.
- `/store-owner/products?role=storeOwner` — chủ cửa hàng.
- `/staff/shops/00000000-0000-4000-8000-000000000001/flash-deals?role=staff` — nhân viên.
- `/admin/analytics/overview?role=admin` — quản trị.
- `/__ui` — bộ components và trạng thái tham chiếu.

Role được giữ trong session. Đổi bằng `?role=user|storeOwner|staff|admin|guest`. Mặc định là user đăng nhập sẵn. Token mock được lưu ở namespace riêng trong sessionStorage; token thật của trình duyệt không bị ghi đè.

Dùng `?mock=empty`, `?mock=error` hoặc `?mock=slow` để kiểm tra danh sách rỗng, lỗi 500 và độ trễ 1.5 giây. Ghép query bằng `&` khi URL đã có role hoặc params. Auth vẫn hoạt động trong các scenario này. Thêm `?variant=1` (hoặc 0, 2, ...) để xem các giá trị enum hữu hạn khác, trích từ DTO thật; ví dụ standing của Trust Score.

Chi tiết dùng UUID thật trong dữ liệu danh sách, ví dụ `00000000-0000-4000-8000-000000000001`. Checkout cần `?shopId=...`; receipt và simulation cần `?orderId=...`. UI vẫn giữ các guard và điều kiện vốn có.

## Dữ liệu và thay đổi

- MSW chặn request ở tầng mạng; services/API client thật giữ nguyên. Endpoint API chưa có handler trả 501, không chuyển tiếp sang backend thật.
- Fixture wire response trong `src/mocks/factories/` dùng `satisfies` với đúng DTO/envelope ở callsite service, kể cả các kiểu pagination khác nhau. `scripts/generate-mock-fixtures.cjs` tạo baseline từ TypeScript checker. Các record và quan hệ giao dịch được bổ sung trong `factories/core.ts`.
- Faker tiếng Việt được seed 42. Dùng ảnh local, tên/địa chỉ Việt Nam và VND. Thời gian tham chiếu được cố định cho session để deadline không nhảy khi reload.
- Search, filter, sort và pagination chạy trên dữ liệu in-memory. CRUD và luồng giỏ hàng/checkout/order/payment/refund cập nhật dữ liệu và giữ qua reload của tab. Mock là công cụ xem UI, không thay thế test nghiệp vụ backend.
- Realtime dùng emitter theo cùng interface và event names; phát sự kiện khi mutation và nhịp 30 giây cho các trang đơn hàng. Chế độ mock không mở SignalR tới backend.
- GPS, ngân hàng VietQR, route/reverse/style VietMap và OSM tiles có dữ liệu mô phỏng. Thư viện map và font trong index.html vẫn tải từ CDN như app gốc; khi offline, phần bản đồ có thể hiển thị fallback hiện có.
- Reset dữ liệu: `window.__savebiteMock.reset()` trong DevTools; hoặc đóng tab để bắt đầu session mới. Thay đổi mock không ghi vào database thật.

Sau khi DTO/service thay đổi:

```bash
npm run mock:generate
npx prettier --write src/mocks/factories src/mocks/handlers/registry.ts
npm run build
```

Route `/shop-profile` hiện không truyền `:id` cho CustomerShopPage nên vẫn hiển thị trạng thái không tìm thấy như code gốc; dùng `/shops/:id` để xem hồ sơ đầy đủ. `/admin/orders` vẫn là Coming soon vì chưa có module/API. Riêng staff detail, mock bổ sung location.state.staff theo cách trang thật nhận từ navigation.

Không sửa file fixture generated bằng tay; chỉnh generator hoặc bổ sung typed factory nghiệp vụ.

## Kiểm tra

```bash
npm run build
npx eslint src/mocks src/main.tsx
npx playwright install chromium
npm run test:mock
```

Bộ Playwright mock kiểm tra 75 route khai báo, inventory GET, pagination/filter/CRUD, persistence qua reload, checkout/payment, role và các scenario. Cấu hình riêng `playwright.mock.config.ts`; không cần backend. Linux tối giản có thể cần các dependencies hệ thống của Chromium.

Xem tiến độ redesign tại [docs/ui-redesign/PROGRESS.md](docs/ui-redesign/PROGRESS.md), inventory tại [ROUTES.md](docs/ui-redesign/ROUTES.md).
