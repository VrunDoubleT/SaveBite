# Cấu hình backend

`SaveBite.Api/appsettings.json` và `.env` chứa cấu hình riêng hiện có; Git bỏ qua hai file. Khóa lồng nhau trong `.env` dùng `__`, ví dụ `Email__Password` tương ứng `Email:Password`.

Khi chạy backend trực tiếp, biến môi trường của host được ưu tiên hơn `.env`, sau đó đến `appsettings.{Environment}.json` và `appsettings.json`. Không bắt buộc có `.env`. EF migrations áp dụng cùng quy tắc không ghi đè biến môi trường đã có.

Bộ triển khai hiện build một image duy nhất theo `Dockerfile` ở gốc. Công cụ C# trong `deployment/SaveBite.Deployment` hợp nhất secrets lúc khởi động, tạo runtime environment riêng và dùng loopback cho các dịch vụ nội bộ. Database connection strings được tạo bằng Npgsql/Redis builders để giữ ký tự đặc biệt. Các giá trị trong file nguồn không bị ghi ngược khi URL tunnel đổi.

`ASPNETCORE_URLS`/`DOTNET_URLS` hoặc `urls` được ưu tiên so với `API_PORT`. Ngoài chế độ proxy, backend giữ HTTPS redirection hiện có. Bật proxy chỉ khi backend nằm sau Nginx và không publish port API trực tiếp.

Data Protection dùng application name `SaveBite`. Trong image, key ring nằm tại `/var/lib/savebite/keys`, cùng volume bền vững với dữ liệu. Cần giữ key ring cũ nếu nhập database có thông tin PayOS đã mã hóa.

Key PayOS shop lấy từ database; key nền tảng ưu tiên database, có fallback `PlatformPayOS` từ cấu hình. Worker `PayOsWebhookSyncWorker` xác nhận webhook tự động theo URL/session và phiên bản credentials; thanh toán chỉ mở cho tài khoản đã đồng bộ. Hangfire tiếp tục phục vụ các jobs nghiệp vụ khác.

Xem hướng dẫn build/chạy tại `docs/deployment.md`. File `compose.yaml` trong thư mục backend vẫn dành cho hạ tầng phát triển local; dùng Compose ở gốc cho bộ một image.
