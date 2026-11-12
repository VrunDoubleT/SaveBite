import type { AuditArchiveRow } from "../types/auditArchive.types";
import type { AuditLogFilterOptions } from "../types/auditLog.types";
import { auditDayKey } from "./formatAuditLog";

export interface ArchiveFilters {
  search: string;
  actorType: "" | "User" | "System";
  targetType: string;
  action: string;
  fromDate: string;
  toDate: string;
  actorUserId: string;
  targetId: string;
  correlationId: string;
}
export const EMPTY_ARCHIVE_FILTERS: ArchiveFilters = {
  search: "",
  actorType: "",
  targetType: "",
  action: "",
  fromDate: "",
  toDate: "",
  actorUserId: "",
  targetId: "",
  correlationId: "",
};
export function archiveFilterOptions(
  rows: AuditArchiveRow[],
  targetType: string,
): AuditLogFilterOptions {
  return {
    actions: [
      ...new Set(
        rows.filter((r) => !targetType || r.targetType === targetType).map((r) => r.action),
      ),
    ].sort(),
    targetTypes: [...new Set(rows.map((r) => r.targetType))].sort(),
  };
}
export function filterArchiveRows(
  rows: AuditArchiveRow[],
  filters: ArchiveFilters,
): AuditArchiveRow[] {
  const search = filters.search.trim().toLowerCase();
  return rows.filter((row) => {
    const day = auditDayKey(row.createdAt);
    if ((filters.fromDate && day < filters.fromDate) || (filters.toDate && day > filters.toDate))
      return false;
    if (
      (filters.actorType && row.actorType !== filters.actorType) ||
      (filters.targetType && row.targetType !== filters.targetType) ||
      (filters.action && row.action !== filters.action)
    )
      return false;
    for (const key of ["actorUserId", "targetId", "correlationId"] as const)
      if (filters[key] && row[key]?.toLowerCase() !== filters[key].toLowerCase()) return false;
    return (
      !search ||
      [
        row.id,
        row.actorUserId,
        row.actorName,
        row.actorEmail,
        row.actorType,
        row.action,
        row.targetType,
        row.targetId,
        row.reason,
        row.correlationId,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase()
        .includes(search)
    );
  });
}
