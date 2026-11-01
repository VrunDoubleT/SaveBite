// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { person, past } from "./values";
import type { ApiResponse } from "@/shared/types";
import type {
  AuditLogFilterOptions,
  AuditLogItem,
} from "@/features/audit-logs/types/auditLog.types";
interface BackendAuditLogPageResponse {
  page: {
    items: AuditLogItem[];
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
  };
  filterOptions: AuditLogFilterOptions;
}
export const response0 = {
  data: {
    page: {
      items: [
        {
          id: "00000000-0000-4000-8000-000000000001",
          actorUserId: "00000000-0000-4000-8000-000000000001",
          actorType: "User",
          actorName: person(0),
          actorEmail: "minh1@example.test",
          action: "Tiêu chuẩn",
          targetType: "Tiêu chuẩn",
          targetId: "00000000-0000-4000-8000-000000000001",
          oldValues: {},
          newValues: {},
          reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
          correlationId: "00000000-0000-4000-8000-000000000001",
          outcome: "Tiêu chuẩn",
          createdAt: past,
        },
        {
          id: "00000000-0000-4000-8000-000000000002",
          actorUserId: "00000000-0000-4000-8000-000000000002",
          actorType: "System",
          actorName: person(1),
          actorEmail: "minh2@example.test",
          action: "Cỡ lớn",
          targetType: "Cỡ lớn",
          targetId: "00000000-0000-4000-8000-000000000002",
          oldValues: {},
          newValues: {},
          reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
          correlationId: "00000000-0000-4000-8000-000000000002",
          outcome: "Cỡ lớn",
          createdAt: past,
        },
        {
          id: "00000000-0000-4000-8000-000000000003",
          actorUserId: "00000000-0000-4000-8000-000000000003",
          actorType: "User",
          actorName: person(2),
          actorEmail: "minh3@example.test",
          action: "Không cay",
          targetType: "Không cay",
          targetId: "00000000-0000-4000-8000-000000000003",
          oldValues: {},
          newValues: {},
          reason:
            "Thực phẩm được chuẩn bị trong ngày, đóng gói cẩn thận. Vui lòng nhận trước giờ đóng cửa; sản phẩm có thể chứa đậu phộng và sữa.",
          correlationId: "00000000-0000-4000-8000-000000000003",
          outcome: "Không cay",
          createdAt: past,
        },
      ],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
    },
    filterOptions: {
      actions: ["Tiêu chuẩn", "Cỡ lớn"],
      targetTypes: ["Tiêu chuẩn", "Cỡ lớn"],
    },
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<BackendAuditLogPageResponse>;
export const response1 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    actorUserId: "00000000-0000-4000-8000-000000000001",
    actorType: "User",
    actorName: person(0),
    actorEmail: "minh1@example.test",
    action: "Tiêu chuẩn",
    targetType: "Tiêu chuẩn",
    targetId: "00000000-0000-4000-8000-000000000001",
    oldValues: {},
    newValues: {},
    reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
    correlationId: "00000000-0000-4000-8000-000000000001",
    outcome: "Tiêu chuẩn",
    createdAt: past,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<AuditLogItem>;
export default {
  "audit-logs-auditLogApi_getLogs_0": response0,
  "audit-logs-auditLogApi_getLogDetail_1": response1,
};
