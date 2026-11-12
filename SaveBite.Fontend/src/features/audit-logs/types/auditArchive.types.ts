export type AuditArchiveStatus = "Uploading" | "Completed" | "Failed" | "Missing" | "Deleted";

export type AuditArchive = {
  id: string;
  year: number;
  month: number;
  isTest: boolean;
  status: AuditArchiveStatus;
  recordCount: number;
  bytes: number;
  sha256: string;
  createdAt: string;
  error?: string;
};
export type AuditArchiveRow = {
  id: string;
  action: string;
  targetType: string;
  targetId: string;
  createdAt: string;
  actorType: string;
  actorUserId?: string | null;
  actorName?: string;
  actorEmail?: string;
  correlationId?: string | null;
  oldValues: unknown;
  newValues: unknown;
  reason?: string;
};

export const ARCHIVE_PAGE_SIZE = 25;
