# Theme và primitives

Hoàn tất giai đoạn `ui-redesign-tokens`. Nguồn theme duy nhất: `src/index.css`.

- Giữ nguyên 10 giá trị `primary-50` đến `primary-900` so với trước thay đổi.
- Accent vàng ấm, secondary indigo, warning/info và các nền semantic khớp `ui-redesign-tokens/references/tokens.css`. Danger gốc vẫn giữ nguyên vì reference không đổi giá trị đó.
- Bổ sung `danger-strong` và `info-strong` cho chữ/nút đạt contrast; không đổi các semantic token tham chiếu. Muted mới đạt 4.51:1 trên trắng; primary button dùng primary-700 đạt 6.29:1.
- Inter được khai báo qua Google Fonts với display=swap; khi không có mạng dùng font hệ thống. Không cắt dấu tiếng Việt: body 14px, line-height 1.5; title 24/32, section 18/28, caption 12/16.
- Nền trang bg-body, card bg-surface trắng, border-default, shadow-sm; control radius-md, card radius-lg, pill radius-full. Padding 16px mobile/24px desktop; customer max-width 1280px. Workspace dashboard giữ full width.
- Focus rõ ràng, thời gian chuyển trạng thái 150ms, reduced-motion; Lucide stroke 1.75. Chart SVG đọc màu bằng CSS variables từ cùng theme, không giữ bản sao hex riêng.

## Dùng components

Import từ `@/shared/components/ui`:

```tsx
import { Button, Card, FormField, Input, PageHeader } from "@/shared/components/ui";

<PageHeader title="Products" description="Manage your products" />
<Card>
  <FormField label="Product name" hint="Use a short name.">
    {(field) => <Input {...field} name="name" />}
  </FormField>
  <Button loading={isSaving}>Save</Button>
</Card>
```

Có sẵn Button (primary/secondary/ghost/danger, sm/md/lg, loading/disabled), IconButton (bắt buộc aria-label), Input, Textarea, Select, Checkbox, Switch, FormField, Badge, Card, PageHeader, DataTable, Tabs, Modal, Drawer, Skeleton, EmptyState, ErrorState và StatCard.

Avatar, Pagination và Toast tái sử dụng component/store hiện có. `SharedModal` giữ API cũ, dùng chung Modal cho focus trap/Escape/backdrop/focus restore và scroll lock; children chỉ mount khi mở. Các analytics component hiện có vẫn dùng API cũ. Pagination giữ nguyên logic và props, đổi styling token và wrap controls trên mobile.

`FormField` nối label, hint/error và aria-invalid qua render prop; với Checkbox/Switch cần label bao ngoài hoặc aria-label. DataTable yêu cầu caption/rowKey, có sticky header và normal/loading/empty/error; Tabs hỗ trợ ArrowLeft/Right/Home/End. Modal/Drawer dùng native dialog, cần browser hiện đại hỗ trợ showModal.

Sidebar desktop thu gọn/mở rộng bằng state cục bộ trong WorkspaceLayout; mobile dùng drawer với Escape/focus trap và scroll lock. Giữ danh sách nav, badges, guards và auth/service logic. Các trang chức năng chưa được redesign.

## Xem showcase

Từ thư mục `SaveBite.Fontend`:

```bash
VITE_ENABLE_UI_SHOWCASE=true npm run dev
```

Mở `/__ui` trên URL Vite in ra. Route chỉ tồn tại khi **DEV và VITE_ENABLE_UI_SHOWCASE=true**, lazy loaded và bị loại khỏi production bundle. Đây là ví dụ tĩnh của primitives, không thay thế API, đăng nhập hoặc realtime. AppProvider vẫn dùng session thật. Không đưa dữ liệu mẫu của showcase vào các trang chức năng.

Chạy app bằng `npm run dev` với VITE_API_URL trỏ backend thật. Theo [WORKFLOW.md](./WORKFLOW.md); bỏ hoàn toàn bước ui-redesign-mock và mọi yêu cầu mock trong skill bên ngoài.

## Kiểm tra

- `npm run build`: TypeScript + production build.
- ESLint toàn bộ file TypeScript thay đổi và các primitives/showcase mới.
- So sánh token với reference và với primary scale trước thay đổi; kiểm tra contrast các cặp chữ/nền mới.
- Chromium/Playwright trên `/__ui`: desktop 1440px và mobile 390px, không tràn ngang; sidebar collapse/drawer, table normal/loading/empty/error, keyboard tabs, pagination, switch/toast, modal focus vòng kín/Escape/focus restore, reduced-motion, không có page error.
- Production bundle không chứa `/__ui` hoặc nội dung showcase.

Phạm vi kiểm tra trình duyệt là showcase. Chưa kiểm tra toàn bộ route có backend, bản đồ, thanh toán hoặc role thật. Các màu hardcoded và style cũ trong từng trang sẽ được chuyển dần sang primitive/token ở giai đoạn ui-redesign-page; chưa coi QA toàn app hoàn tất. Build vẫn có cảnh báo chunk lớn đã tồn tại trước thay đổi.

## Style trang đã được duyệt

Người dùng đã duyệt style trang chủ với điểm nhấn màu nhẹ; áp dụng tiếp cho flash deal detail và các trang redesign sau:

- Nền trắng/sáng, Inter; giữ nguyên thang xanh lá. Phần giới thiệu trang chủ có vạch xanh nhẹ; tiêu đề sản phẩm detail không có vạch màu. CTA chính xanh primary-700; icon/count/filter được chọn dùng nền primary-50.
- Danh mục và thông tin nhận hàng dùng secondary-50/700; flash deal/thời hạn đang hoạt động dùng accent-50/100/700. Giữ phần lớn nội dung trung tính để các điểm nhấn rõ ràng.
- Trạng thái luôn có chữ hoặc icon, không chỉ dựa vào màu. Không tạo rating, review count hay trạng thái mở cửa khi DTO không cung cấp.
- `ButtonLink` dùng cho CTA điều hướng. `MediaImage` dự phòng ảnh thiếu/hỏng, giữ không gian theo class của caller; không dùng ảnh sản phẩm khác làm ảnh thay thế. Cả hai có ví dụ ở `/__ui`.

## Phạm vi bắt buộc khi redesign từng trang

Thiết kế đầy đủ mọi section và component có trên trang, kể cả phần phụ, nội dung đang placeholder, empty/loading/error, modal, bộ lọc và phân trang. Trước khi làm phải kiểm kê; sau khi làm đối chiếu lại để không bỏ sót. Không tự xoá component vì DTO chưa có dữ liệu: giữ trạng thái trung thực hoặc dùng nguồn API hiện có với phạm vi được ghi rõ. Flash deal detail bao gồm cả Customer reviews ở cuối trang.


## Breadcrumb

Theo yêu cầu người dùng, mọi breadcrumb bắt đầu bằng nhãn/link chữ, không có mũi tên quay lại ở đầu. Giữ dấu phân cách giữa các cấp và aria-current cho trang hiện tại. Nút Back độc lập ngoài breadcrumb giữ hành vi/icon riêng.

Bộ lọc đã được duyệt: dùng shared `FilterBar` cho các trang tiếp theo có dropdown. Giữ nền trắng, border token, bo góc và shadow nhẹ; label bên trái, native dropdown căn phải, control cao tối thiểu 44px. Nhiều bộ lọc dùng các hàng trong cùng khung. Không tự đổi sang tabs/chips hoặc thêm nút Refresh.

Ưu tiên mật độ gọn theo yêu cầu mới: card danh sách/nội dung thường dùng padding 12–16px, heading nội dung 14px, ảnh nhận diện 40–48px, số tiền 14–18px; tránh banner lớn hoặc card ít thông tin nhưng quá cao. Giữ nút/chạm tối thiểu 44px, đủ dữ liệu và text wrap; không ép chiều cao khiến mất nội dung.

Account pagination: dùng `AccountPagination` từ `@/shared/components/pagination` để thống nhất nút Đầu/Trước/Sau/Cuối, size và trạng thái disabled. Hiện khi danh sách có dữ liệu, kể cả chỉ 1 trang; ẩn khi rỗng. Không tạo totalPages giả cho API cursor (Orders vẫn dùng tải thêm).
