# SaveBite Redis Flash Deal Sync Tool

Công cụ CLI dùng để đồng bộ toàn bộ Flash Deals đang hoạt động (`OnSale`) từ PostgreSQL database (`savebite`) vào Redis Cache (`savebite-redis`).

## Mục đích
- `DbSeeder.cs` trong backend chỉ chịu trách nhiệm seed dữ liệu vào PostgreSQL.
- Trang chủ người dùng (`/api/flash-deals`) và tính năng tìm kiếm món gần vị trí người dùng đọc flash deals theo thời gian thực trực tiếp từ **Redis** (dựa trên Redis Geo `savebite:geo:active-shops` và Sorted Set theo shop).
- Công cụ này đọc toàn bộ Flash Deal + Shop + Product + Variants từ PostgreSQL và nạp đầy đủ vào Redis để người dùng thấy ngay trên Home Page.

## Cách chạy

### Cách 1: Sử dụng terminal (PowerShell hoặc CMD)
```bash
# Di chuyển vào thư mục tool
cd "c:\Work Space\Project_Official\Main_Project\SaveBite\SaveBite.Backend\tool"

# Chạy công cụ
dotnet run
```

### Cách 2: Chạy script nhanh
- **Windows Batch**: Nhấp đúp vào file [run.bat](file:///c:/Work%20Space/Project_Official/Main_Project/SaveBite/SaveBite.Backend/tool/run.bat) hoặc gõ `.\run.bat` trong terminal.
- **PowerShell**: Gõ `.\run.ps1` trong terminal.

## Dữ liệu được đưa vào Redis:
1. `savebite:geo:active-shops`: Lưu tọa độ các cửa hàng có flash deal đang mở bán.
2. `savebite:shop:{shopId}:deals`: Sorted Set chứa danh sách deal của cửa hàng, sắp xếp theo thời gian kết thúc (`orderEndTime`).
3. `savebite:deal:{dealId}`: Hash chứa đầy đủ thông tin chi tiết của deal và sản phẩm.
4. `savebite:deal:{dealId}:variants`: Hash chứa danh sách các biến thể món ăn và giá ưu đãi.
5. `savebite:deal:{dealId}:stock`: Hash lưu số lượng tồn kho theo thời gian thực của từng biến thể.

## Seed deal còn hạn và cấu hình vị trí tùy chọn

Chạy từ thư mục gốc repository:

```sh
# Cách cũ: chỉ đồng bộ Redis, giữ nguyên tọa độ shop trong PostgreSQL.
dotnet run --project SaveBite.Backend/tool

# Seed deal OnSale còn 119 phút, sau đó đồng bộ Redis; vẫn giữ tọa độ cũ.
dotnet run --project SaveBite.Backend/tool -- --seed-active

# Chỉ khi chủ động bật cờ này mới chuyển shop về gần tọa độ mới.
dotnet run --project SaveBite.Backend/tool -- --seed-active --use-custom-location
```

Hoặc sửa `toolsettings.json`: `Seed:UseCustomLocation = true`. Mặc định là `false`.
Tọa độ mới đã cấu hình sẵn: **10.016760913098414, 105.34127364471898**. Các shop có độ lệch nhỏ để không chồng điểm trên bản đồ. Khi bật, tool cập nhật tọa độ shop được sync ở cả PostgreSQL và Redis GEO, giúp checkout và tìm kiếm gần đây nhất quán. Tắt cấu hình sẽ giữ vị trí hiện đang lưu trong DB, không tự khôi phục tọa độ trước lần di chuyển.

`--seed-active` dùng tối đa 3 shop active có sản phẩm/variant active, tối đa 3 sản phẩm mỗi shop. Deal mới có 200 suất mỗi variant. Deal còn hơn 20 phút và còn hàng được tái sử dụng; không reset số đã bán/giữ, không sửa đơn hàng cũ. Tool cần dữ liệu shop/product ban đầu từ backend; nó không tạo tài khoản mới. Chạy lại lệnh seed khi hết hạn.
