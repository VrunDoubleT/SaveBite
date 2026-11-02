# Workflow UI hiện hành — backend thật

Yêu cầu hiện tại của người dùng: đã gỡ lớp mock khỏi frontend và bỏ bước mock trong quy trình AI. Giữ style được duyệt ở commit 7db29e8: nền trắng/sáng, xanh lá chủ đạo, điểm nhấn nhỏ. Không áp dụng lại thử nghiệm phối màu 2036b14.

## Trình tự làm việc

1. Đọc AGENTS.md, TOKENS.md, ROUTES.md, PROGRESS.md và trang/component/hook/service/DTO của route được yêu cầu.
2. Nếu inventory thiếu, audit bổ sung. Giữ tokens/style hiện có; không chạy lại theme toàn app nếu không được yêu cầu.
3. Dùng ui-ux-pro-max cho phân cấp, layout, tương tác và accessibility; dùng ui-redesign-page cho từng trang. Bỏ ui-redesign-mock và các chỉ dẫn mock trong orchestrator/page/qa. Quy định của dự án này là yêu cầu mới hơn của người dùng.
4. Chạy backend theo cấu hình của nhóm; đặt VITE_API_URL đúng URL thật, dùng tài khoản thật và resource ID từ API/danh sách. Không đổi role bằng query, tự sinh token hoặc seed dữ liệu frontend.
5. Triển khai đủ các section/child/dialog và normal/loading/empty/error/partial states, giữ nguyên API/DTO/route/logic. Nếu DTO thiếu dữ liệu, thể hiện fallback trung thực hoặc dùng nguồn API hiện có; không tạo response giả.
6. Chạy build, lint các file thay đổi; kiểm tra 360/768/1280px và bàn phím/focus. Dùng dữ liệu/states do backend hỗ trợ. Khi backend hoặc tài khoản không sẵn có, vẫn hoàn thành code review/build và ghi rõ phần integration chưa kiểm chứng. Không tự tạo backend giả để vượt qua thiếu dữ liệu.
7. Kiểm tra UI không cần tạo đơn/thanh toán thật. Chỉ thử hành động không thể hoàn tác khi có ủy quyền cụ thể. Cập nhật PROGRESS.md và tài liệu trang, ghi rõ đã kiểm tra gì/chưa kiểm tra gì.

## Chạy frontend

```bash
cd SaveBite.Fontend
npm install
# Tạo .env.local khi cần, cấu hình VITE_API_URL theo backend của nhóm.
npm run dev
npm run build
```

Không ghi đè .env/.env.local hoặc credentials của người khác. Không dùng tài khoản/mật khẩu mẫu tự bịa. Route detail phải dùng ID thật; checkout bắt đầu từ shop trong cart thật.

Showcase tùy chọn: VITE_ENABLE_UI_SHOWCASE=true npm run dev, mở /__ui trong DEV. Đây chỉ là tài liệu primitives, không chặn request hoặc thay session/realtime.

## Prompt bàn giao cho AI

```text
Tiếp tục redesign route [ROUTE] của SaveBite frontend.
Đọc SaveBite.Fontend/AGENTS.md và docs/ui-redesign/{WORKFLOW,TOKENS,ROUTES,PROGRESS}.md.
Dùng ui-redesign-page và ui-ux-pro-max; bỏ toàn bộ bước mock trong các skill.
Giữ style hiện tại, reuse shared components, thiết kế đầy đủ mọi section/modal/state.
Chỉ dùng backend thật qua VITE_API_URL, tài khoản và ID thật. Không tạo fixture,
interceptor, seeded data, auth/role/GPS/payment/realtime giả hoặc bypass guard.
Giữ API/DTO/route/logic. Kiểm tra build/lint/responsive và ghi rõ phần backend chưa
kiểm chứng nếu thiếu truy cập/dữ liệu. Cập nhật PROGRESS.md sau khi xong.
```
