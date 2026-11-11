import type { AuditArchiveRow } from "../types/auditArchive.types";

function parsePayload(payload: unknown): unknown {
  if (typeof payload !== "string") return payload ?? null;
  try {
    return JSON.parse(payload);
  } catch {
    return payload;
  }
}

function normalizeRow(record: Record<string, unknown>): AuditArchiveRow {
  const fields = { ...record };
  delete fields.oldValuesJson;
  delete fields.newValuesJson;
  delete fields.actorUser;
  return {
    ...fields,
    actorType:
      record.actorType === 1 ? "User" : record.actorType === 2 ? "System" : record.actorType,
    oldValues: parsePayload(record.oldValues ?? record.oldValuesJson),
    newValues: parsePayload(record.newValues ?? record.newValuesJson),
  } as AuditArchiveRow;
}

/** Read exported JSONL, preserving original line numbers for validation errors. */
export function parseAuditArchive(text: string): AuditArchiveRow[] {
  const parsed: AuditArchiveRow[] = [];
  const lines = text.replace(/^\uFEFF/, "").split(/\r?\n/);
  for (const [index, line] of lines.entries()) {
    if (!line.trim()) continue;
    try {
      const value: unknown = JSON.parse(line);
      if (
        !value ||
        typeof value !== "object" ||
        !("id" in value) ||
        typeof value.id !== "string" ||
        !("action" in value) ||
        typeof value.action !== "string" ||
        !("targetType" in value) ||
        typeof value.targetType !== "string" ||
        !("createdAt" in value) ||
        typeof value.createdAt !== "string" ||
        !Number.isFinite(Date.parse(value.createdAt))
      )
        throw new Error("Invalid audit record");
      parsed.push(normalizeRow(value as Record<string, unknown>));
    } catch {
      throw new Error(
        `Invalid audit log at line ${index + 1}. Please select an exported JSONL file.`,
      );
    }
  }
  return parsed;
}
