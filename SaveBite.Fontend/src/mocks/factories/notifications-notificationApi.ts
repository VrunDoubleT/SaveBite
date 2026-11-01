// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { past } from "./values";
import type { ApiResponse } from "@/shared/types";
import type {
  NotificationItem,
  NotificationListResponse,
} from "@/features/notifications/types/notification.types";

export const response0 = {
  data: {
    page: {
      items: [
        {
          id: "00000000-0000-4000-8000-000000000001",
          type: "Tiêu chuẩn",
          title: "Đơn hàng mới",
          body: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
          actionUrl: "/images/deals/banh-mi/bm-overview.jpg",
          isRead: false,
          readAt: past,
          createdAt: past,
        },
        {
          id: "00000000-0000-4000-8000-000000000002",
          type: 2,
          title: "Đã xác nhận",
          body: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
          actionUrl: "/images/deals/banh-mi/bm-overview.jpg",
          isRead: false,
          readAt: past,
          createdAt: past,
        },
        {
          id: "00000000-0000-4000-8000-000000000003",
          type: "Không cay",
          title: "Cập nhật cửa hàng",
          body: "Thực phẩm được chuẩn bị trong ngày, đóng gói cẩn thận. Vui lòng nhận trước giờ đóng cửa; sản phẩm có thể chứa đậu phộng và sữa.",
          isRead: false,
          createdAt: past,
        },
      ],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
      hasPrevious: false,
      hasNext: false,
    },
    unreadCount: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<NotificationListResponse>;
export const response1 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    type: "Tiêu chuẩn",
    title: "Đơn hàng mới",
    body: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
    actionUrl: "/images/deals/banh-mi/bm-overview.jpg",
    isRead: false,
    readAt: past,
    createdAt: past,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<NotificationItem>;
export const response2 = {
  success: true,
  message: "Thao tác thành công.",
} satisfies unknown;
export const response3 = {
  success: true,
  message: "Thao tác thành công.",
} satisfies unknown;
export default {
  "notifications-notificationApi_getNotifications_0": response0,
  "notifications-notificationApi_markAsRead_1": response1,
  "notifications-notificationApi_markAllAsRead_2": response2,
  "notifications-notificationApi_deleteNotification_3": response3,
};
