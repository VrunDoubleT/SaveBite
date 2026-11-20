# SaveBite — cân chỉnh Complexity và Story Points

Ngày đánh giá: 16/11/2026. Nguồn đầu vào: 186 UC trong tài liệu người dùng cung cấp; đối chiếu source React/TypeScript và ASP.NET Core trong workspace. Đây là đề xuất phục vụ refinement, chưa phải cam kết của đội phát triển.

## Kết quả

- Tổng: **624 → 742 SP**, chênh **+118 SP (18.9%)**.
- 81 UC thay đổi SP; 40 UC thay đổi Complexity.
- Giữ nguyên UC, tên, user story, Feature và Priority P2; chỉ điều chỉnh hai cột được yêu cầu. Không suy ra ưu tiên từ SP.
- CSV chứa điểm cũ/mới, chênh lệch và lý do; TSV giữ đúng 7 cột để copy vào bảng gốc.

## Cách chấm

Story Points là ước lượng tương đối cho tổng công sức frontend + backend + dữ liệu + tích hợp + kiểm thử nghiệp vụ, xét cả độ khó và mức chưa rõ. Không quy đổi trực tiếp sang giờ/ngày. Không chấm theo số dòng code hoặc chỉ vì có nhiều màn hình.

Để bảng dễ dùng và nhất quán, đề xuất quy ước nội bộ:

| Complexity | SP | Mốc tham chiếu |
|---|---|---|
| Simple | 1 | Một thao tác nhỏ qua hạ tầng đã có: đọc một notification, xóa dòng giỏ. |
| Simple | 2 | View/form nhỏ hoặc đổi trạng thái ít ràng buộc. |
| Medium | 3 | CRUD có validation/quyền; danh sách có lọc/phân trang thông thường. |
| Medium | 5 | Nhiều quy tắc, nhiều quan hệ, bản đồ/upload/OTP hoặc aggregation cần đối chiếu. |
| Complex | 8 | Nhiều miền dữ liệu, transaction, cạnh tranh cập nhật, payment hoặc vòng đời bất đồng bộ. |
| Complex | 13 | Luồng rộng, nhiều rủi ro liên kết; cần tách trước khi đưa vào sprint. |

Đây là quy ước cho backlog này, không phải yêu cầu của Scrum. Complexity và SP có thể độc lập ở đội khác. Không dùng 6 vì đã chọn thang Fibonacci.

## Giả định để tránh đếm trùng

1. Ước lượng phạm vi hoàn chỉnh của tính năng trên kiến trúc hiện có, gồm các hành vi còn thiếu trong mô tả. Không phải remaining effort và không xác nhận UC đã hoàn tất chỉ vì có service tương ứng.
2. Có sẵn nền tảng auth, phân quyền, DB, upload, email, SignalR, scheduler, gateway adapter và audit. Công dựng các nền tảng chung này chưa được ước lượng riêng trong bảng; phải thêm technical stories nếu lập kế hoạch xây dự án từ đầu.
3. UC-030 sở hữu quote/ETA/giữ tồn/tạo đơn; UC-031 sở hữu vòng đời khởi tạo payment phía người dùng; UC-185 sở hữu xác minh và chốt giao dịch từ webhook. Không tính lại toàn bộ gateway ở cả ba UC.
4. UC-069 sở hữu hành động xác nhận của shop; job hết hạn/xử lý No-Show và recovery dùng chung phải có technical stories riêng nếu chưa tồn tại. Vì vậy giảm 8 xuống 5 cho hành động confirm không có nghĩa toàn bộ vòng đời đơn chỉ mất 5 SP.
5. UC-122 sở hữu khởi tạo thanh toán bảng kê; UC-186 sở hữu callback. Monthly statement generation, overdue suspension và reconciliation dùng chung cần được theo dõi riêng nếu triển khai mới.
6. UC view trust/analytics chỉ tính truy vấn/tính toán và hiển thị tương ứng; engine tự động cộng điểm, phục hồi và xử lý vi phạm cần có owner rõ ràng, không mặc nhiên nằm trong màn hình view.
7. Kiểm thử cần cho trạng thái sai, quyền, hết hạn, yêu cầu lặp và cạnh tranh dữ liệu nằm trong điểm của hành vi tương ứng; không cộng thêm một tỷ lệ QA cố định.

## Chứng cứ từ source và điểm cần chốt scope

Các đường dẫn dưới đây là căn cứ tiêu biểu theo miền; bảng chi tiết là ước lượng từ mô tả UC kết hợp source, không phải kiểm toán hoàn thành từng acceptance criterion.

| Miền | Source đã đối chiếu | Ảnh hưởng ước lượng |
|---|---|---|
| Kiến trúc | `SaveBite.Backend/SaveBite.Api/SaveBite.Api.csproj`, `SaveBite.Fontend/package.json` | React, EF/PostgreSQL, Redis, Hangfire, RabbitMQ, Cloudinary và SignalR làm cơ sở tái sử dụng. |
| Checkout | `SaveBite.Backend/SaveBite.Api/Services/Implementations/CheckoutService.cs` | Quote có hạn, GPS/ETA, fingerprint giá/chính sách và lock/chống trùng đơn: UC-030 = 13. |
| Thanh toán | `Services/Implementations/CheckoutPaymentService.cs`, `PayOsWebhookDispatcher.cs` | Tách kênh đơn/phí; không xem callback như một update trạng thái đơn giản: UC-185/186 = 8. |
| Vận hành đơn | `Services/Implementations/ShopOrderService.cs` | Confirm dùng lock và deadline; complete có cash/online conflict, kiểm tra đủ tiền và event: UC-069 = 5, UC-072 = 8. |
| Restock | `Services/Implementations/OrderResaleService.cs` | Quyết định theo item, thời hạn, retry idempotent và inventory outbox: UC-081 = 8. |
| Refund | `Services/Implementations/RefundService.cs` | OTP destination, deadline, review và transfer tạo nhiều nhánh kiểm thử: UC-039/040/117 = 5. |
| Địa chỉ | `Services/Implementations/UserAddressService.cs` | Create/update dùng cùng cấu trúc địa chỉ và tọa độ; UC-018/019 cùng 5 khi bao gồm UI bản đồ. |
| Shop suspension | `Services/Implementations/ShopSuspensionCleanupService.cs` | Hủy deal đang mở và đơn pending; UC-144 = 8. |
| Trust | `Services/Implementations/TrustConfigService.cs` | Quy tắc level/benefit/penalty có nhiều validation và khoảng hiệu lực: các UC cấu hình tương ứng = 5. |
| Phí nền tảng | `Services/Implementations/PlatformFeeStatementService.cs` | Payment link, transaction, webhook và reconciliation; tách phần hành động UI và callback. |
| Credential | `Services/Implementations/PlatformPayOsConfigService.cs` | Bảo vệ secret, validate credential và thay đổi kết nối: UC-177 = 8; UC-112 giả định mức bảo vệ tương đương. |
| Analytics | `Services/Implementations/PlatformAnalyticsService.cs` | Dùng cùng period resolver và định nghĩa doanh thu; không cộng lại toàn bộ analytics framework ở từng chart. |
| Archive | `Audit/Archives/AuditArchiveFile.cs`, `Controllers/AdminAuditArchivesController.cs` | Streaming ZIP/JSONL, checksum, tệp tạm và filter archive server. |
| Local archive | `SaveBite.Fontend/src/features/audit-logs/components/AuditArchivePanel.tsx` | Hiện đọc bằng file.text() và parse JSONL, chưa phải mở encrypted ZIP trực tiếp. |

Các đường dẫn rút gọn `Services/`, `Audit/`, `Controllers/` tính từ `SaveBite.Backend/SaveBite.Api/`.

### Các khác biệt mô tả–code ảnh hưởng điểm

- **UC-073:** mô tả nói VietQR; `ShopOrderService.RemainingPaymentAsync` hiện là tạo/tái sử dụng payment link PayOS cho số dư. Chọn **8 SP** theo luồng payment có đối soát. Nếu chỉ render VietQR từ dữ liệu ngân hàng, không xác nhận đã trả, có thể giảm còn **3 SP**. Cần chọn acceptance criteria trước sprint.
- **UC-118:** chỉ sinh VietQR refund từ thông tin đã xác nhận, không tự chuyển tiền hay đối soát ngân hàng: **3 SP**.
- **UC-182:** tài liệu yêu cầu encrypted ZIP; `AuditArchiveFile` đang dùng `System.IO.Compression.ZipArchive` để tạo ZIP thường, không thấy triển khai mật khẩu/mã hóa ZIP trong luồng download đã đọc. Chọn **5 SP** với giả định tái sử dụng archive/storage và bổ sung định dạng mã hóa được chấp nhận. Nếu chỉ download ZIP thường: **3 SP**. Cần chốt cơ chế cấp/nhận khóa và công cụ mở file.
- **UC-184:** **5 SP** cho đọc JSONL đã giải nén tại máy, validate schema, lọc/phân trang và lỗi file. Nếu phải mở encrypted ZIP ngay trong trình duyệt: cần refinement riêng, dự kiến **8 SP** khi đã chốt định dạng và giới hạn dung lượng; chưa cộng vào tổng.
- **UC-161:** mô tả gồm retention, nhưng API analytics hiện có không tự chứng minh đã có đầy đủ cohort/retention theo yêu cầu. **5 SP** giả định retention cơ bản; dashboard cohort nhiều chiều nên tách story.
- **UC-012:** mô tả yêu cầu revoke access token lẫn refresh token. **3 SP** giả định có cơ chế invalidation dùng chung; nếu phải xây denylist/session version từ đầu cần technical story riêng. Không xác nhận yêu cầu này đã được đáp ứng.

## Tổng theo Feature

| Feature | UC | SP cũ | SP đề xuất | Chênh lệch |
|---|---:|---:|---:|---:|
| FE-01 Account & Authentication | 7 | 26 | 29 | +3 |
| FE-02 User Profile & Address Management | 8 | 23 | 24 | +1 |
| FE-03 PayOS Management | 5 | 16 | 23 | +7 |
| FE-04 Store Registration Management | 8 | 31 | 31 | +0 |
| FE-05 Staff & Shop Membership Management | 12 | 32 | 34 | +2 |
| FE-06 Store Profile & Operating Management | 5 | 15 | 21 | +6 |
| FE-07 Product Management | 10 | 37 | 46 | +9 |
| FE-08 Flash Deal Management | 7 | 28 | 33 | +5 |
| FE-09 Flash Deal Discovery & Browsing | 6 | 19 | 24 | +5 |
| FE-10 Store Information & Feedback Management | 8 | 21 | 24 | +3 |
| FE-11 Shop Follow Management | 4 | 12 | 10 | -2 |
| FE-12 Order Management | 16 | 66 | 81 | +15 |
| FE-13 Payment & Deposit Management | 5 | 18 | 29 | +11 |
| FE-14 Refund & Refund Request Management | 12 | 44 | 49 | +5 |
| FE-15 Notification Management | 4 | 10 | 9 | -1 |
| FE-16 Trust Score & Reputation Management | 22 | 62 | 74 | +12 |
| FE-17 Platform Fee Management | 11 | 37 | 40 | +3 |
| FE-18 Store Dashboard & Analytics | 4 | 16 | 18 | +2 |
| FE-19 User Management | 3 | 8 | 13 | +5 |
| FE-20 Food Category Management | 5 | 13 | 14 | +1 |
| FE-21 Content Moderation | 8 | 28 | 40 | +12 |
| FE-22 Feedback Moderation | 2 | 6 | 8 | +2 |
| FE-23 Audit Log | 6 | 20 | 30 | +10 |
| FE-24 Platform Dashboard & Analytics | 8 | 36 | 38 | +2 |

FE-09 có chứa cả discovery cửa hàng, FE-13 chứa webhook phí theo tài liệu gốc; giữ nguyên phân nhóm để bạn dễ đối chiếu.

## Bảng đủ 186 UC

| UC | Tên | Complexity / SP cũ | Complexity / SP đề xuất | Căn cứ |
|---|---|---|---|---|
| UC-001 | Login | Medium / 5 | Medium / 5 | Đăng nhập, JWT/refresh token, điều hướng theo vai trò và trạng thái tài khoản. |
| UC-002 | Forget Password | Medium / 3 | Medium / 3 | Gửi email reset, giới hạn yêu cầu và xử lý địa chỉ email. |
| UC-003 | Verify Password Reset | Medium / 5 | Medium / 5 | OTP hết hạn/số lần thử, đổi mật khẩu và vô hiệu hóa phiên theo chính sách. |
| UC-004 | Register | Medium / 5 | Medium / 5 | Kiểm tra trùng email, lưu đăng ký chờ và gửi OTP. |
| UC-005 | Verify Registration | Medium / 3 | Medium / 5 | Xác minh OTP, kích hoạt và khởi tạo Trust Score/Level nhất quán. |
| UC-006 | View Nearby Stores | Medium / 3 | Medium / 5 | GPS/địa chỉ, truy vấn khoảng cách, quyền vị trí và trạng thái cửa hàng. |
| UC-007 | View Store Profile | Simple / 2 | Simple / 2 | Hiển thị hồ sơ công khai từ dữ liệu cửa hàng và điểm đánh giá. |
| UC-008 | View Store's Flash Deals | Medium / 3 | Medium / 3 | Danh sách deal theo cửa hàng, lọc trạng thái và thời gian hiệu lực. |
| UC-009 | View Store Reviews | Medium / 3 | Medium / 3 | Phân trang review, ảnh và thông tin phản hồi. |
| UC-010 | View Flash Deals By Current Location | Complex / 5 | Complex / 8 | Kết hợp tìm kiếm, nhiều bộ lọc, khoảng cách và các chế độ vị trí. |
| UC-011 | View Flash Deal Details | Medium / 3 | Medium / 3 | Tổng hợp biến thể, giá, tồn khả dụng và khung giờ pickup. |
| UC-012 | Logout | Simple / 2 | Medium / 3 | Thu hồi phiên/refresh token và dọn trạng thái xác thực ở frontend. |
| UC-013 | Change Password | Medium / 3 | Medium / 3 | Xác thực mật khẩu cũ, hash mật khẩu mới và kiểm tra chính sách. |
| UC-014 | View Profile | Simple / 2 | Simple / 2 | Đọc và hiển thị thông tin cá nhân có kiểm tra quyền sở hữu. |
| UC-015 | Edit Profile | Simple / 3 | Simple / 2 | Form thông tin cơ bản và validation dùng chung. |
| UC-016 | Update Avatar | Medium / 3 | Medium / 3 | Upload ảnh, kiểm tra file và đồng bộ avatar. |
| UC-017 | View addresses | Simple / 2 | Simple / 2 | Danh sách địa chỉ đã lưu của người dùng. |
| UC-018 | Add New Address | Medium / 5 | Medium / 5 | Form địa chỉ, chọn tọa độ trên bản đồ và xử lý địa chỉ mặc định. |
| UC-019 | Update Address | Medium / 3 | Medium / 5 | Chỉnh form và tọa độ, tái sử dụng bộ chọn vị trí của UC-018. |
| UC-020 | Delete Address | Simple / 2 | Simple / 2 | Xóa có quyền sở hữu và xử lý trường hợp địa chỉ mặc định. |
| UC-021 | Set Default Address | Simple / 3 | Medium / 3 | Bảo đảm một địa chỉ mặc định và đồng bộ ngữ cảnh vị trí. |
| UC-022 | View Notifications | Medium / 3 | Medium / 5 | Danh sách, badge chưa đọc và đồng bộ sự kiện SignalR. |
| UC-023 | Mark Notification as Read | Simple / 2 | Simple / 1 | Đổi cờ đọc của một bản ghi và cập nhật badge qua hạ tầng chung. |
| UC-024 | Delete Notification | Simple / 2 | Simple / 1 | Xóa một thông báo thuộc người dùng qua API dùng chung. |
| UC-025 | Mark All Notifications as Read | Medium / 3 | Simple / 2 | Cập nhật hàng loạt thông báo thuộc người dùng và badge. |
| UC-026 | View items in cart | Medium / 3 | Medium / 3 | Nhóm giỏ theo cửa hàng, tính tiền và hiển thị khả dụng. |
| UC-027 | Update Item Quantity | Medium / 3 | Medium / 3 | Kiểm tra tồn hiện tại, số lượng hợp lệ và cập nhật tổng tiền. |
| UC-028 | Remove Item from Cart | Simple / 2 | Simple / 1 | Xóa dòng giỏ và cập nhật tổng qua hạ tầng giỏ chung. |
| UC-029 | Add Flash Deal to Cart | Medium / 5 | Medium / 5 | Kiểm tra deal/variant, tồn, trùng dòng và đồng bộ giỏ. |
| UC-030 | Checkout Order | Complex / 8 | Complex / 13 | Quote GPS/ETA, deadline, chính sách cọc, khóa tồn và chống tạo đơn trùng. |
| UC-031 | Payment | Complex / 6 | Complex / 8 | Tạo/tái sử dụng payment link, số tiền/cọc, hết hạn và phục hồi lỗi gateway. |
| UC-032 | View Order History | Medium / 3 | Medium / 3 | Danh sách phân trang và bộ lọc trạng thái đơn. |
| UC-033 | View Order Details | Medium / 3 | Medium / 3 | Tổng hợp items, số dư, cửa hàng và deadline của đơn. |
| UC-034 | Cancel Order | Medium / 5 | Complex / 8 | Hủy theo trạng thái/deadline, xử lý payment, tồn, refund và tác động trust. |
| UC-035 | View Customer Payment History | Medium / 3 | Medium / 3 | Danh sách giao dịch và bộ lọc trạng thái/loại thanh toán. |
| UC-036 | View Payment Details | Medium / 3 | Simple / 2 | Hiển thị một giao dịch đã ghi nhận, có kiểm tra sở hữu. |
| UC-037 | View Refund History | Medium / 3 | Medium / 3 | Phân trang refund và hiển thị nhiều trạng thái xử lý. |
| UC-038 | Request Refund | Complex / 8 | Complex / 8 | Điều kiện Completed/6 giờ, ảnh bằng chứng, chống trùng và khởi tạo deadline. |
| UC-039 | Provide Bank Information | Medium / 3 | Medium / 5 | Thông tin ngân hàng hoặc đọc VietQR, validation và gửi mã xác nhận. |
| UC-040 | Confirm Refund via Email | Medium / 3 | Medium / 5 | OTP tài khoản nhận tiền, giới hạn thử, hết hạn và chuyển trạng thái refund. |
| UC-041 | Withdraw Refund Request | Medium / 3 | Medium / 3 | Rút đúng trạng thái cho phép, lưu lịch sử và thông báo. |
| UC-042 | View Refund Details | Simple / 2 | Medium / 3 | Tổng hợp timeline, bằng chứng và dữ liệu chuyển khoản có kiểm tra quyền. |
| UC-043 | Add Feedback | Medium / 3 | Medium / 5 | Kiểm tra đơn đủ điều kiện, upload nhiều ảnh và cập nhật rating cửa hàng. |
| UC-044 | View Feedback History | Simple / 2 | Medium / 3 | Danh sách review cá nhân có ảnh và phản hồi cửa hàng. |
| UC-045 | Edit Feedback | Medium / 2 | Medium / 3 | Sửa nội dung/ảnh, quyền sở hữu và đồng bộ rating nếu đổi điểm. |
| UC-046 | Delete Feedback | Simple / 2 | Medium / 3 | Xóa review, xử lý ảnh và cập nhật số lượng/điểm đánh giá. |
| UC-047 | View Trust Score and Level | Medium / 3 | Medium / 3 | Hiển thị điểm, level và chính sách cọc hiện hành. |
| UC-048 | Track Level Progress | Medium / 3 | Medium / 3 | Tính khoảng cách tới level tiếp theo từ nhiều tiêu chí. |
| UC-049 | View Trust Score History | Medium / 3 | Medium / 3 | Lịch sử cộng/trừ điểm, phân trang và mô tả sự kiện. |
| UC-050 | View Level Info & Benefits | Simple / 2 | Simple / 2 | Hiển thị cấu hình quyền lợi và điều kiện level. |
| UC-051 | View Penalty History | Medium / 3 | Medium / 3 | Lịch sử vi phạm, mức phạt và trạng thái phục hồi. |
| UC-052 | Track Penalty Recovery Progress | Medium / 5 | Medium / 5 | Tổng hợp chuỗi pickup thành công và điều kiện phục hồi hiện hành. |
| UC-053 | Register Shop | Complex / 8 | Complex / 8 | Form nhiều phần, giờ mở cửa, bản đồ và nhiều giấy tờ/ảnh. |
| UC-054 | View Shop Registration Status | Simple / 2 | Simple / 2 | Hiển thị trạng thái hồ sơ và phản hồi của admin. |
| UC-055 | Edit and Resubmit Shop Registration | Medium / 5 | Medium / 5 | Sửa hồ sơ theo trạng thái, thay giấy tờ và gửi duyệt lại. |
| UC-056 | Cancel Shop Registration | Simple / 2 | Simple / 2 | Rút hồ sơ pending với kiểm tra quyền và trạng thái. |
| UC-057 | View Received Staff Invitations | Simple / 2 | Simple / 2 | Danh sách lời mời nhận được và trạng thái/hạn hiệu lực. |
| UC-058 | Accept Staff Invitation | Complex / 5 | Medium / 5 | Nhận lời mời còn hạn, ràng buộc một cửa hàng và cập nhật membership. |
| UC-059 | Decline Staff Invitation | Simple / 2 | Simple / 2 | Từ chối lời mời hợp lệ và cập nhật trạng thái. |
| UC-060 | Follow Shop | Medium / 3 | Medium / 3 | Tạo follow không trùng và tích hợp tùy chọn nhận thông báo. |
| UC-061 | Unfollow Shop | Medium / 3 | Simple / 2 | Xóa follow thuộc người dùng và dừng nhận alert. |
| UC-062 | Manage Shop Notification Settings | Medium / 3 | Simple / 2 | Toggle tùy chọn trên follow hiện có. |
| UC-063 | View Followed Shops | Medium / 3 | Medium / 3 | Danh sách shop đã follow, thông tin shop và liên kết deal. |
| UC-064 | View Flash Deals for Shop | Medium / 3 | Medium / 3 | Phân trang deal của shop theo vòng đời và quyền owner/staff. |
| UC-065 | View Flash Deal Details for Shop | Medium / 3 | Medium / 3 | Chi tiết cấu hình deal, biến thể, tồn và pickup. |
| UC-066 | View Shop Orders | Medium / 3 | Medium / 5 | Bộ lọc ngày/trạng thái, bộ đếm tab và đồng bộ đơn theo thời gian thực. |
| UC-067 | Search Order by ID | Simple / 2 | Simple / 2 | Tìm mã/ID chính xác trong phạm vi shop có kiểm tra quyền. |
| UC-068 | View Order Details for Shop | Medium / 3 | Medium / 3 | Thông tin đơn, khách hàng, thanh toán và deadline theo quyền shop. |
| UC-069 | Confirm Order | Complex / 8 | Medium / 5 | Xác nhận dưới khóa đơn, kiểm tra cọc và deadline; dùng chung job hết hạn. |
| UC-070 | Reject Order | Medium / 5 | Complex / 8 | Từ chối đơn gắn với đóng payment, refund và giải phóng/xử lý tồn. |
| UC-071 | Update Preparation Status | Medium / 3 | Medium / 3 | Chuyển trạng thái hợp lệ và phát sự kiện qua outbox dùng chung. |
| UC-072 | Complete Order | Medium / 5 | Complex / 8 | Thu tiền còn lại, chống thu trùng, hoàn tất và phát sự kiện phí/trust. |
| UC-073 | Generate Remaining Payment QR | Complex / 5 | Complex / 8 | Code hiện tại tạo link PayOS cho số dư, có tái sử dụng và đối soát thanh toán. |
| UC-074 | View Shop and Staff Info | Simple / 2 | Simple / 2 | Đọc shop và membership hiện tại của staff. |
| UC-075 | Leave Shop | Medium / 3 | Medium / 3 | Kết thúc membership và thu hồi quyền vận hành. |
| UC-076 | View Feedback for Shop | Medium / 3 | Medium / 3 | Danh sách feedback của shop có bộ lọc và phân trang. |
| UC-077 | Reply Feedback | Medium / 3 | Medium / 3 | Thêm phản hồi với quyền shop, validation và thông báo. |
| UC-078 | Edit Reply | Medium / 3 | Simple / 2 | Sửa phản hồi hiện có, tái sử dụng form và kiểm tra quyền. |
| UC-079 | Delete Reply | Medium / 3 | Simple / 2 | Xóa phản hồi thuộc shop và cập nhật giao diện. |
| UC-080 | View Restock Tasks | Medium / 3 | Medium / 3 | Danh sách tác vụ theo hạn, trạng thái và item cần xử lý. |
| UC-081 | Make Resale / Restock Decision | Medium / 3 | Complex / 8 | Quyết định từng item, khóa đơn, chống xử lý lặp và đồng bộ tồn/deadline. |
| UC-082 | View Shop Profile | Medium / 3 | Simple / 2 | Hiển thị hồ sơ và trạng thái vận hành hiện tại. |
| UC-083 | Edit Business Info | Medium / 3 | Medium / 5 | Cập nhật địa chỉ/tọa độ, giờ hoạt động và validation liên trường. |
| UC-084 | Edit Shop Images and Logo | Medium / 3 | Medium / 3 | Upload/thay logo và banner qua dịch vụ ảnh chung. |
| UC-085 | View Products | Medium / 3 | Medium / 3 | Danh sách catalog có phân trang, category và trạng thái. |
| UC-086 | View Product Detail | Medium / 3 | Medium / 3 | Tổng hợp attributes, variants, ảnh và giá sản phẩm. |
| UC-087 | Create New Product | Complex / 8 | Complex / 8 | Tạo aggregate sản phẩm, thuộc tính, tổ hợp variant và ảnh. |
| UC-088 | Update Product Info | Medium / 3 | Medium / 3 | Chỉnh nội dung cơ bản/category với kiểm tra quyền sở hữu. |
| UC-089 | Delete Product | Medium / 3 | Medium / 5 | Kiểm tra liên kết deal/order và bảo toàn lịch sử khi xóa. |
| UC-090 | Update Product Images | Medium / 3 | Medium / 5 | Upload nhiều ảnh, sắp xếp thứ tự và dọn ảnh thay thế. |
| UC-091 | Add Attributes Values | Medium / 5 | Complex / 8 | Sinh tích Descartes variant, chống trùng và bảo toàn biến thể đang dùng. |
| UC-092 | Update Variant | Medium / 3 | Medium / 3 | Validation SKU/giá/trạng thái và ràng buộc biến thể. |
| UC-093 | Update Product Status | Medium / 3 | Medium / 5 | Ảnh hưởng khả năng bán và deal liên quan, bảo toàn lịch sử. |
| UC-094 | View Product Flash Deal History | Medium / 3 | Medium / 3 | Danh sách lịch sử deal và thông tin kết quả cơ bản. |
| UC-095 | Create Flash Deal | Complex / 8 | Complex / 8 | Giá giảm, tồn từng variant, khung giờ và lập lịch vòng đời deal. |
| UC-096 | Update Flash Deal | Medium / 5 | Complex / 8 | Sửa tồn/giờ trong khi có đơn giữ chỗ và cập nhật job/realtime. |
| UC-097 | Cancel Flash Deal | Medium / 5 | Medium / 5 | Ngừng nhận đơn, xử lý trạng thái deal và đồng bộ giỏ/tồn. |
| UC-098 | Unhide Flash Deal | Simple / 2 | Medium / 3 | Kiểm tra thời gian, trạng thái sản phẩm/shop trước khi mở lại. |
| UC-099 | Hide Flash Deal | Simple / 2 | Medium / 3 | Ẩn deal, ngăn đặt mới và đồng bộ discovery/giỏ qua cơ chế chung. |
| UC-100 | View Sent Staff Invitations | Simple / 1 | Simple / 2 | Danh sách lời mời đã gửi, trạng thái và hạn hết hiệu lực. |
| UC-101 | Revoke Staff Invitation | Simple / 2 | Simple / 2 | Thu hồi lời mời pending và xử lý cạnh tranh với thao tác accept. |
| UC-102 | Invite Customer as Staff | Complex / 5 | Medium / 5 | Kiểm tra ứng viên, tránh trùng, gửi email và lập lịch hết hạn. |
| UC-103 | View Staff List | Simple / 2 | Simple / 2 | Danh sách staff và trạng thái membership trong shop. |
| UC-104 | View Staff Activity Log | Medium / 3 | Medium / 3 | Lịch sử hành động theo staff, bộ lọc và phân trang. |
| UC-105 | Update Staff Info | Simple / 2 | Medium / 3 | Đổi trạng thái staff và bảo đảm kiểm tra quyền ở thao tác tiếp theo. |
| UC-106 | Remove Staff Member | Medium / 3 | Medium / 3 | Kết thúc membership, thu hồi quyền và lưu lịch sử. |
| UC-107 | View Revenue Overview | Medium / 5 | Medium / 5 | Định nghĩa doanh thu ròng, kỳ thời gian và đối chiếu hoàn tiền. |
| UC-108 | View Order Statistics | Medium / 5 | Medium / 3 | Đếm đơn theo trạng thái/kỳ qua nền tảng analytics chung. |
| UC-109 | View Shop Flash Deal Statistics | Medium / 3 | Medium / 5 | Tỷ lệ bán hết và lượng thực phẩm cứu cần công thức/đơn vị nhất quán. |
| UC-110 | View Revenue by Payment Method | Medium / 3 | Medium / 5 | Tách kênh thanh toán, cọc/số dư và tránh cộng trùng doanh thu. |
| UC-111 | View PayOS Info | Simple / 2 | Simple / 2 | Hiển thị trạng thái kết nối, thông tin ngân hàng và credential đã che. |
| UC-112 | Update PayOS Info | Medium / 5 | Complex / 8 | Kiểm thử credential thật, bảo vệ secret và xử lý payment link khi đổi cấu hình. |
| UC-113 | View Refund Requests for Shop | Medium / 3 | Medium / 3 | Danh sách refund theo shop với trạng thái và phân trang. |
| UC-114 | Review Refund Details for Shop | Medium / 3 | Medium / 3 | Chi tiết refund, đơn và bằng chứng theo quyền owner. |
| UC-115 | Accept Refund Request | Medium / 5 | Medium / 5 | Duyệt dưới kiểm soát trạng thái, destination và thời hạn xử lý. |
| UC-116 | Reject Refund Request | Medium / 3 | Medium / 3 | Từ chối có lý do, lưu timeline và gửi thông báo. |
| UC-117 | Update Refund Transfer Status | Medium / 3 | Medium / 5 | Upload biên lai, kiểm tra chuyển trạng thái và chống xác nhận trùng. |
| UC-118 | Generate VietQR Code for Refund | Complex / 5 | Medium / 3 | Sinh VietQR từ ngân hàng/số tiền đã xác nhận; không đối soát giao dịch. |
| UC-119 | View Warning History | Simple / 2 | Simple / 2 | Danh sách cảnh báo và chỉ dẫn khắc phục của shop. |
| UC-120 | View Shop Platform Fee Statements | Medium / 3 | Medium / 3 | Danh sách bảng kê theo tháng và trạng thái thanh toán. |
| UC-121 | View Shop Platform Fee Statement Details | Medium / 3 | Medium / 3 | Hiển thị breakdown phí đã chốt và dữ liệu order liên quan. |
| UC-122 | Pay Shop Platform Fee Statement | Complex / 8 | Complex / 8 | Payment link bảng kê, retry, hết hạn và đối soát trạng thái khoản phí. |
| UC-123 | View Shop Platform Fee Report | Medium / 3 | Medium / 5 | Tổng hợp phí đã trả/còn nợ theo kỳ và đối chiếu bảng kê. |
| UC-124 | View Current Platform Fee Configuration | Simple / 2 | Simple / 2 | Đọc cấu hình mức phí đang có hiệu lực. |
| UC-125 | View Shop Registration Requests | Medium / 3 | Medium / 3 | Hàng đợi hồ sơ với phân trang và lọc trạng thái. |
| UC-126 | View Registration Details | Medium / 3 | Medium / 3 | Chi tiết hồ sơ, địa chỉ và nhiều giấy tờ. |
| UC-127 | Approve Registration | Medium / 5 | Medium / 5 | Tạo/kích hoạt shop, đổi role và lưu kết quả duyệt nhất quán. |
| UC-128 | Reject Registration | Medium / 3 | Medium / 3 | Từ chối có lý do và thông báo cho người đăng ký. |
| UC-129 | View Users | Medium / 3 | Medium / 3 | Danh sách người dùng, tìm kiếm, lọc role và phân trang. |
| UC-130 | View Account Details | Simple / 2 | Medium / 5 | Tổng hợp trust, penalty, shop và role từ nhiều quan hệ. |
| UC-131 | Update Account Status | Medium / 3 | Medium / 5 | Khóa/mở tài khoản, lý do, hiệu lực quyền truy cập và audit. |
| UC-132 | View Category List | Simple / 2 | Simple / 2 | Danh sách category và thứ tự hiển thị. |
| UC-133 | Create Category | Simple / 3 | Medium / 3 | Tạo tên/icon/mô tả, validation và kiểm tra trùng. |
| UC-134 | Update Category | Simple / 2 | Medium / 3 | Sửa nội dung và thứ tự, tái sử dụng form category. |
| UC-135 | Delete Category | Medium / 3 | Medium / 3 | Kiểm tra category còn được dùng trước khi xóa. |
| UC-136 | Update Category Status | Medium / 3 | Medium / 3 | Đổi trạng thái và kiểm soát category khả dụng khi chọn sản phẩm. |
| UC-137 | View Products for Admin | Medium / 3 | Medium / 3 | Danh sách sản phẩm toàn nền tảng, lọc shop/trạng thái. |
| UC-138 | View Product Content | Medium / 3 | Medium / 5 | Tổng hợp revision, ảnh, giá và dữ liệu phục vụ kiểm duyệt. |
| UC-139 | Remove Violating Product | Complex / 8 | Complex / 8 | Gỡ sản phẩm, hủy deal liên quan và bảo toàn đơn đã xác nhận. |
| UC-140 | Send Violation Warning | Medium / 3 | Medium / 3 | Ghi cảnh báo có chỉ dẫn và thông báo cho owner. |
| UC-141 | Approve Revised Product Content | Medium / 3 | Medium / 5 | Kiểm tra revision và trạng thái warning trước khi phục hồi nội dung. |
| UC-142 | View Shops | Medium / 3 | Medium / 3 | Danh sách shop với trạng thái, rating và bộ lọc. |
| UC-143 | View Shop Content | Simple / 2 | Medium / 5 | Chi tiết shop, hồ sơ pháp lý và lịch sử thay đổi trạng thái. |
| UC-144 | Update Shop Status | Medium / 3 | Complex / 8 | Suspension kéo theo hủy deal/đơn pending, refund và bảo toàn đơn confirmed. |
| UC-145 | View Level Benefits | Simple / 2 | Simple / 2 | Hiển thị quyền lợi và quy tắc cọc của level. |
| UC-146 | Update Level Benefits | Medium / 3 | Medium / 5 | Validation khoảng phần trăm cọc và tác động tới chính sách checkout. |
| UC-147 | View Level Requirements | Simple / 2 | Simple / 2 | Hiển thị ngưỡng điểm/đơn/tỷ lệ hoàn thành. |
| UC-148 | Update Level Requirements | Medium / 3 | Medium / 5 | Validation nhiều ngưỡng, tính nhất quán giữa level và lịch sử cấu hình. |
| UC-149 | View Level List | Medium / 3 | Simple / 2 | Danh sách level và trạng thái qua API cấu hình chung. |
| UC-150 | Add New Level | Medium / 3 | Medium / 5 | Tạo level cùng benefits/requirements trong một transaction. |
| UC-151 | Update Level Information | Medium / 3 | Simple / 2 | Sửa tên/mô tả và ghi lịch sử qua cơ chế chung. |
| UC-152 | Update Level Status | Medium / 3 | Medium / 5 | Ràng buộc level mặc định, level được gán và tác động chính sách. |
| UC-153 | View Level Configuration History | Medium / 3 | Medium / 3 | Lịch sử phiên bản và thay đổi cấu hình level. |
| UC-154 | View Penalty Rules | Simple / 2 | Simple / 2 | Đọc các quy tắc phạt đang hiệu lực. |
| UC-155 | Update Penalty Rules | Medium / 3 | Medium / 5 | Kiểm tra khoảng hiệu lực không chồng lấn và cập nhật lịch kích hoạt. |
| UC-156 | Add New Penalty Rule | Medium / 3 | Medium / 5 | Thêm ngưỡng vi phạm, điểm trừ và khoảng thời gian hợp lệ. |
| UC-157 | Delete Scheduled Penalty Rule | Medium / 3 | Medium / 3 | Chỉ xóa rule tương lai và đồng bộ job/lịch sử. |
| UC-158 | View Trust Score Rules | Simple / 2 | Simple / 2 | Hiển thị cấu hình các sự kiện cộng/trừ điểm. |
| UC-159 | Update Trust Score Rule | Medium / 3 | Medium / 5 | Validation điểm và ảnh hưởng tới engine tính trust, ghi lịch sử. |
| UC-160 | View Platform Overview | Medium / 3 | Medium / 5 | Tổng hợp KPI nhiều miền, kỳ thời gian và so sánh với toàn kỳ. |
| UC-161 | View User Statistics | Medium / 5 | Medium / 5 | Tăng trưởng/retention cần định nghĩa cohort và kiểm tra cách tính. |
| UC-162 | View Shop Statistics | Medium / 5 | Medium / 3 | Thống kê số shop theo trạng thái trên nền analytics chung. |
| UC-163 | View Revenue and Order Statistics | Medium / 5 | Medium / 5 | GMV, xu hướng đơn và completion rate theo kỳ nhất quán. |
| UC-164 | View Platform Flash Deal Statistics | Medium / 5 | Medium / 5 | Tổng hợp deal và food rescue với quy tắc đo lường thống nhất. |
| UC-165 | View Payment and Refund Statistics | Medium / 5 | Medium / 5 | Tách payment/refund, phân phối phương thức và tỷ lệ hoàn tiền. |
| UC-166 | View Revenue by Shop | Medium / 3 | Medium / 5 | Nhóm doanh thu theo shop và đối chiếu tổng nền tảng. |
| UC-167 | View Platform Fee Statistics | Medium / 5 | Medium / 5 | Tổng hợp phí thu được, còn nợ và quá hạn trên các bảng kê. |
| UC-168 | View Scheduled Platform Fee Configurations | Medium / 3 | Simple / 2 | Danh sách cấu hình phí tương lai và ngày hiệu lực. |
| UC-169 | View Customer Feedbacks for Admin | Medium / 3 | Medium / 3 | Danh sách review toàn hệ thống với lọc rating/keyword. |
| UC-170 | Moderate Feedback | Medium / 3 | Medium / 5 | Ẩn/xóa review, audit và ảnh hưởng rating công khai của shop. |
| UC-171 | Update Platform Fee Configuration | Medium / 3 | Medium / 5 | Lịch phí tương lai, khoảng hiệu lực và job activation. |
| UC-172 | View Platform Fee Configuration History | Medium / 3 | Medium / 3 | Lịch sử thay đổi cấu hình phí và actor. |
| UC-173 | Cancel Scheduled Platform Fee Configuration | Medium / 3 | Medium / 3 | Hủy cấu hình chưa có hiệu lực và cập nhật lịch kích hoạt. |
| UC-174 | View All Shops’ Platform Fee Statements | Medium / 3 | Medium / 3 | Danh sách bảng kê toàn hệ thống theo trạng thái và shop. |
| UC-175 | View Any Shop’s Platform Fee Statement Details | Medium / 3 | Medium / 3 | Dùng chung breakdown bảng kê, bổ sung quyền admin. |
| UC-176 | View Platform PayOS Configuration | Medium / 3 | Simple / 2 | Hiển thị credential đã che và cấu hình webhook nền tảng. |
| UC-177 | Update Platform PayOS Configuration | Medium / 3 | Complex / 8 | Test credential, mã hóa secret, rotation và ảnh hưởng link phí đang mở. |
| UC-178 | View Platform PayOS Configuration History | Medium / 3 | Medium / 3 | Lịch sử rotation với payload đã loại secret. |
| UC-179 | View Audit Log | Medium / 5 | Medium / 5 | Tìm kiếm nhiều chiều, phân trang và sanitize old/new payload. |
| UC-180 | View Audit Archive List | Medium / 3 | Simple / 2 | Danh sách archive với metadata, kỳ và số bản ghi. |
| UC-181 | Create Monthly Audit Archive | Medium / 3 | Complex / 8 | Streaming, nén, lưu trữ, checksum và tính nhất quán metadata khi retry. |
| UC-182 | Download Audit Archive | Medium / 3 | Medium / 5 | Yêu cầu ZIP mã hóa cần bổ sung so với ZIP thường đang triển khai. |
| UC-183 | View Archived Audit Logs | Medium / 3 | Medium / 5 | Đọc archive server, streaming/filter/phân trang và dọn file tạm. |
| UC-184 | View Audit Logs from Local File | Medium / 3 | Medium / 5 | Parse/validate JSONL local, lọc/phân trang và báo file lỗi; chưa gồm giải nén ZIP. |
| UC-185 | Process Order Payment Webhook | Medium / 3 | Complex / 8 | Xác minh chữ ký/số tiền, webhook lặp/đến muộn và transaction cập nhật đơn/tồn. |
| UC-186 | Process Platform Fee Payment Webhook | Medium / 3 | Complex / 8 | Xác minh webhook theo credential, chống lặp và chốt bảng kê/khôi phục shop. |

## Tách story trước sprint

UC-030 nên tách theo kết quả có thể nghiệm thu: (a) preview quote và ETA; (b) đặt đơn/giữ tồn có chống trùng; (c) xử lý quote cũ và cập nhật đồng thời. Các lát cắt này chưa được chấm điểm riêng; 13 SP hiện là mốc của story gốc và cần ước lượng lại sau khi tách.

Các UC 8 SP như 031, 072, 081, 095, 139, 144, 181, 185 và 186 cần acceptance criteria rõ cho lỗi, retry và chuyển trạng thái. Không nhân tổng điểm với một hệ số dự phòng tùy ý. Đội phát triển nên đối chiếu các mốc 2/3/5/8 với vài story đã hoàn thành, sau đó hiệu chỉnh theo velocity thực tế. Tổng điểm hiện tại chưa đủ để suy ra số sprint hoặc ngày bàn giao.
