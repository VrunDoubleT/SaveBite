# SaveBite frontend

React + TypeScript + Vite, Tailwind v4, Zustand và React Router. Frontend hiện chỉ dùng backend thật.

## Chạy local

```bash
npm install
# Tạo .env.local khi cần; xem .env.example và đặt VITE_API_URL theo backend của nhóm.
npm run dev
```

Khởi động backend và cấu hình URL/CORS phù hợp. VITE_API_URL mặc định là /api; Vite không tự proxy URL này, nên hãy đặt URL backend thật hoặc cấu hình reverse proxy của môi trường. Giữ các biến cấu hình VietMap hiện có. Không ghi đè file .env cá nhân chỉ để đổi giao diện.

Đăng nhập bằng tài khoản thật. Quyền lấy từ backend; query role/scenario không còn tác dụng. Dùng ID thật lấy từ API/danh sách cho trang detail, chọn shop trong cart cho checkout. API, auth/session và SignalR đều dùng implementation thật.

## UI reference và workflow AI

Showcase chỉ gồm ví dụ primitives tĩnh, không thay API hoặc đăng nhập:

```bash
VITE_ENABLE_UI_SHOWCASE=true npm run dev
```

Mở /__ui; route không được đưa vào production. Style hiện tại là nền trắng/sáng, xanh lá chủ đạo và điểm nhấn nhỏ. Xem [AGENTS.md](./AGENTS.md), [WORKFLOW.md](./docs/ui-redesign/WORKFLOW.md), [TOKENS.md](./docs/ui-redesign/TOKENS.md), [ROUTES.md](./docs/ui-redesign/ROUTES.md) và [PROGRESS.md](./docs/ui-redesign/PROGRESS.md).

Lớp backend giả, seed/generator, MSW worker và các bộ kiểm tra phụ thuộc đã được gỡ. Khi mở origin từng dùng preview worker cũ, bootstrap chỉ gỡ đúng worker đó rồi tải lại để tránh còn chặn request; các service worker khác và token đăng nhập thật được giữ nguyên. Nếu đang mở nhiều tab preview cũ, đóng/tải lại chúng sau cập nhật.

## Kiểm tra

```bash
npm run build
npx eslint src/main.tsx src/shared/realtime/realtimeClient.ts src/app/router/routes.tsx src/pages/dev/UiShowcasePage.tsx
```

Kiểm tra UI/tích hợp dùng backend và tài khoản thật. Các kết quả kiểm tra trong tài liệu trang trước khi gỡ preview là lịch sử; không thay thế acceptance test với backend thật. Các script kiểm tra đã trỏ đến file không tồn tại cũng được gỡ, không để lại lệnh kiểm tra hỏng.
