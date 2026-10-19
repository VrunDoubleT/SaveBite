# SaveBite - System & End-to-End (E2E) Testing Suite

Bộ kiểm thử hệ thống (System Testing) độc lập sử dụng **Playwright** cho hệ thống SaveBite.

## 📁 Cấu trúc thư mục

```text
SaveBite.Tests.E2E/
├── pages/                    # Page Object Model (POM)
│   ├── BasePage.ts          # Base class dùng chung
│   └── LoginPage.ts         # Quản lý selectors & actions trang Login
├── tests/                    # Kịch bản kiểm thử (Test Specs)
│   ├── smoke/               # Smoke tests (kiểm tra tính sẵn sàng của hệ thống)
│   │   └── system-health.spec.ts
│   └── auth/                # Kiểm thử phân hệ xác thực
│       └── login.spec.ts
├── playwright.config.ts      # Cấu hình Playwright, baseURL, webServer
├── package.json              # Scripts và dependencies riêng cho E2E
└── .env.example              # Biến môi trường mẫu
```

---

## 🚀 Hướng dẫn cài đặt & Chạy test

### 1. Cài đặt thư viện & Browser
Tại thư mục `SaveBite.Tests.E2E`:
```bash
npm install
npx playwright install
```

### 2. Các lệnh chạy test

- **Chạy tất cả test ở chế độ ngầm (headless):**
  ```bash
  npm test
  ```

- **Mở giao diện trực quan (Playwright UI Mode - KHUYÊN DÙNG khi viết & debug test):**
  ```bash
  npm run test:ui
  ```

- **Chạy có mở trình duyệt (Headed):**
  ```bash
  npm run test:headed
  ```

- **Tự động sinh code khi click chuột (Playwright Codegen):**
  ```bash
  npm run test:codegen
  ```

- **Xem báo cáo HTML chi tiết sau khi test xong:**
  ```bash
  npm run report
  ```

---

## ⚙️ Cơ chế hoạt động với Frontend & Backend

- `playwright.config.ts` đã cấu hình `webServer`: nếu bạn chưa bật frontend Vite, Playwright sẽ tự động bật `npm run dev` ở thư mục `../SaveBite.Fontend`.
- Nếu bạn đã bật sẵn Backend (`http://localhost:8080`) và Frontend (`http://localhost:5173`), Playwright sẽ tự động tái sử dụng các port đó để kiểm thử.
