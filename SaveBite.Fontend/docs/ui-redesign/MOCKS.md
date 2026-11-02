# Frontend mock backend

Hoàn tất bước `ui-redesign-mock` ngày 2026-11-02. Hướng dẫn chạy và query switches: [README](../../README.md).

## Phạm vi

- Biến `VITE_USE_MOCK=true`, chỉ trong DEV. Đã bật trong `.env` local; không commit file chứa cấu hình cá nhân.
- MSW 3 chặn network trước khi import App. Không đổi feature services, DTO, validation, business logic hoặc auth store. Realtime factory chỉ thêm nhánh chọn fake transport khi flag bật.
- Inventory được sinh từ 31 module API: **201 callsite/path variants, 198 cặp method/path duy nhất, 98 đường dẫn GET duy nhất**; bổ sung `/auth/token-renewals` của refreshClient. Bao phủ các endpoint trong ROUTES.md và các endpoint service khác tìm được. Endpoint API chưa khai báo trả 501 và được ghi trong `window.__savebiteMock.unhandledRequests`; không fallback về backend thật.
- Wire fixtures dùng `satisfies` trực tiếp với type argument thật tại httpClient callsite, có cả interface envelope cục bộ trong service. Không ép mọi API vào một dạng pagination. Không dùng `any` cho factories/handlers.
- Faker VI seed 42, ảnh local, UUID ổn định, tên/địa chỉ Việt Nam; có tên rất dài, field optional thiếu, list rỗng/một/nhiều, trạng thái enum, đơn hàng giá trị lớn. Date tương đối theo epoch session; giữ deadline nhất quán khi reload.
- Các union enum hữu hạn được trích từ checker vào enum-fields.ts. `?variant=0|1|2|...` chọn biến thể trên response tương ứng, không thêm field ngoài DTO.
- Query `?mock=empty|error|slow`: empty list và metadata đi kèm; error 500 theo ApiErrorResponse; slow 1.5s. Auth vẫn dùng được để duyệt các role.
- Search, status/type/category/rating/date/role-resource filters theo field hiện có, sort và page/cursor thực hiện trên in-memory data. Chỉ phân trang đúng các field có trong từng envelope.
- Mutation giữ dữ liệu trong sessionStorage. Các projection sản phẩm/owner/admin, giỏ hàng, checkout, order/payment/refund và feedback được nối theo ID. Luồng thực tế của backend vẫn cần integration test riêng; đây là backend mô phỏng cho UI.
- Preview mặc định đăng nhập user, chọn `?role=user|storeOwner|staff|admin|guest`. Auth credentials được giả lập ở network; validation form thật vẫn giữ nguyên. Storage shim chỉ tách hai auth token key sang namespace session mock, không ghi đè token thật.
- Emitter realtime theo cùng interface, tên event và envelope; phát khi mutation và tick 30s. Không kết nối SignalR backend khi mock bật.
- GPS HCM, VietQR bank directory, VietMap reverse/route/style và OSM tiles có fixture. Font/map library CDN vẫn giữ nguyên như index.html; có thể dùng fallback khi offline.

## Kiểm chứng

- Build TypeScript + Vite production đạt.
- ESLint toàn bộ mock code, bootstrap, router/transport thay đổi và test/config mới đạt.
- Playwright: kiểm tra **75 route** khai báo không có application error; inventory GET được intercept, không có endpoint mock bị bỏ sót.
- Search/filter/page khác nhau, category CRUD, product update và reload persistence, cart quantity/subtotal, quote/place/payment và order detail đạt.
- Error/empty/slow không chặn auth; mock login và admin role đạt.
- Owner fulfillment confirm/preparation/ready/complete, remaining-payment simulation, refund transfer amount, feedback reply/edit projection/delete đạt.
- Kiểm tra riêng flag false: không có service worker, mock DB/epoch hay debug object; request auth đi qua transport bình thường. Test chặn request trước khi gửi, không dùng backend thật.
- Production JS không chứa database/credentials/emitter/worker bootstrap hoặc route `/__ui`, kể cả khi build từ môi trường local có VITE_USE_MOCK=true. File worker tĩnh được copy như public asset nhưng không được đăng ký ở production.
- Đối chiếu Git: không thay đổi source trong `src/features/`, các real service/DTO, auth store và API client.

## Những giới hạn giữ nguyên có chủ đích

- `/shop-profile` không có `:id`, CustomerShopPage hiện yêu cầu params.id nên hiển thị trạng thái not found; dùng `/shops/:id` để xem nội dung. Không đổi route hoặc logic trang để che vấn đề có từ trước.
- `/admin/orders` vẫn Coming soon, không tạo API/admin module chưa tồn tại.
- StaffDetailPage lấy staff card từ location.state. Mock bootstrap cung cấp typed navigation state khi vào trực tiếp route này; app thật không đổi.
- Checkout cần shopId; receipt/simulation cần orderId. Các điều kiện edit application và edit deal được giữ nguyên.
- Tất cả endpoint audit đã có handler; **không có endpoint HTTP cần backend thật còn bị chặn**. Chưa coi đây là QA nghiệp vụ backend hoặc visual QA toàn bộ màn hình.
- npm install đã khai báo thêm Recharts vì source dashboard vốn import thư viện này nhưng package.json còn thiếu; không thêm UI library mới cho redesign. Cảnh báo bundle chunk lớn vẫn có như trước.

Các route chức năng tiếp tục `todo` trong PROGRESS.md vì chưa bắt đầu bước redesign từng trang.

## Interactive detail gallery

Deal `00000000-0000-4000-8000-000000000002` includes five local bánh mì photos and four combinations of Size (Thường/Lớn) and Độ cay (Không cay/Cay vừa). Prices, available quantities, variant images and the linked Product attributes/value IDs are consistent. Open `/flash-deals/00000000-0000-4000-8000-000000000002` in mock mode and select thumbnails or options.

A `mediaSeedVersion` marker lives only in the internal mock core. Older sessions upgrade this featured product/deal's photos/options once while retaining cart, orders, other products and deal status/timestamps. Reload the page to load the upgraded seed; clearing storage is unnecessary.

## Customer review preview

The existing store-review endpoint now has 12 typed local demo reviews, all five star levels, photo/no-photo entries and store replies. Average, total and breakdown derive from those records. Rating filtering and pagination use the existing query contract; photo filtering is page-scoped in the UI. The mock database carries an internal review-seed version and upgrades only review data/profile rating aggregates for older sessions. Empty mode clears review totals and breakdowns as well as items. No new backend endpoint or review-write capability was introduced.


## Multi-shop cart preview

Default `/cart` now has three shops and six item rows (nine units): Bếp Xanh Sài Gòn — 53.000đ, Tiệm Bánh An Nhiên — 94.000đ, Góc Rau Đà Lạt — 164.000đ. Each new shop has two linked local-media products/deals with its own IDs, prices and quantities. The original 18-product store catalog remains intact; four samples extend the catalog to 22 products/deals. Store deal lists now filter by the requested shop ID.

Internal `cartSeedVersion` upgrades an older mock session once, adding the new catalog records and missing shop groups without replacing edited cart rows, existing products/deals or orders. Reload `/cart` to see the extra shops. Later removals/checkout persist without re-seeding those groups on reload. Mock reset still restores the complete seed.

Mock add-to-cart groups by the deal's shop, and quote IDs identify the selected shop so place-order removes only that group. This fixes the previous single-shop assumptions in the mock handler; real APIs, services, types and page code are unchanged.

Verification covers default totals and deal/shop links, checkout of the second shop, add-back into the correct group, session persistence and one-time migration preserving edited rows. Cart/commerce/storefront regression checks, targeted lint and build pass.
