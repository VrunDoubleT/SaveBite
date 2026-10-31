export interface AuditLogItem {
  id: string;
  actorUserId: string | null;
  actorType: "User" | "System";
  actorName: string;
  actorEmail: string | null;
  action: string;
  targetType: string;
  targetId: string;
  oldValues: Record<string, unknown> | null;
  newValues: Record<string, unknown> | null;
  reason: string | null;
  correlationId: string | null;
  outcome: string;
  createdAt: string;
}

export interface AuditLogFilterOptions {
  actions: string[];
  targetTypes: string[];
}

export interface AuditLogQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  actorType?: "User" | "System" | "";
  targetType?: string;
  action?: string;
  fromDate?: string;
  toDate?: string;
}

export interface AuditLogPageResult {
  items: AuditLogItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  filterOptions: AuditLogFilterOptions;
}