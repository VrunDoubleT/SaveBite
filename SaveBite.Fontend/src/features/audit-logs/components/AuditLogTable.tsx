import { Fragment } from "react";
import { Bot } from "lucide-react";
import { StatusBadge } from "@/shared/components/analytics";
import { TONE } from "@/shared/components/analytics/tones";
import type { AuditLogItem } from "../types/auditLog.types";
import {
  auditActionStyle,
  auditDayKey,
  formatAuditAction,
  formatAuditDayHeading,
  formatAuditTime,
  shortId,
} from "../utils/formatAuditLog";

interface AuditLogTableProps {
  logs: AuditLogItem[];
  loading: boolean;
  onInspect: (log: AuditLogItem) => void;
}

function SkeletonRows() {
  return (
    <ul aria-hidden="true" className="divide-y divide-neutral-100">
      {Array.from({ length: 5 }).map((_, row) => (
        <li key={row} className="flex items-center gap-4 px-5 py-5">
          <div className="size-10 animate-pulse rounded-full bg-neutral-100" />
          <div className="flex-1 space-y-2">
            <div className="h-4 w-56 animate-pulse rounded bg-neutral-100" />
            <div className="h-3 w-40 animate-pulse rounded bg-neutral-100" />
          </div>
        </li>
      ))}
    </ul>
  );
}

/** A timeline grouped by day (Vietnam time). Icon and colour tell the kind of action before any text is read. */
export function AuditLogTable({ logs, loading, onInspect }: AuditLogTableProps) {
  if (loading && logs.length === 0) return <SkeletonRows />;

  if (logs.length === 0) {
    return (
      <div className="px-5 py-16 text-center">
        <p className="text-sm font-semibold text-neutral-800">No audit entries match</p>
        <p className="mt-1 text-sm text-neutral-500">Clear a filter or widen the date range to see more.</p>
      </div>
    );
  }

  return (
    <ul className={`transition-opacity ${loading ? "opacity-50" : ""}`} aria-busy={loading}>
      {logs.map((log, index) => {
        const showDay =
          index === 0 || auditDayKey(logs[index - 1].createdAt) !== auditDayKey(log.createdAt);
        const isSystem = log.actorType === "System";
        const { tone, icon: ActionIcon } = auditActionStyle(log.action);

        return (
          <Fragment key={log.id}>
            {showDay && (
              <li
                aria-hidden="true"
                className="border-y border-neutral-100 bg-neutral-50 px-5 py-2.5 text-xs font-semibold capitalize text-neutral-600 first:border-t-0"
              >
                {formatAuditDayHeading(log.createdAt)}
              </li>
            )}
            <li className="flex flex-wrap items-start gap-x-4 gap-y-3 border-b border-neutral-100 px-5 py-4 last:border-b-0 hover:bg-neutral-50/70">
              <span
                className={`flex size-10 shrink-0 items-center justify-center rounded-full ${TONE[tone].icon}`}
                aria-hidden="true"
              >
                <ActionIcon className="size-5" />
              </span>

              <div className="min-w-0 flex-1 basis-64">
                <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                  <p className="font-semibold text-neutral-900">{formatAuditAction(log.action)}</p>
                  <span className="rounded-md bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-700">
                    {log.targetType}
                  </span>
                  <span className="font-mono text-xs text-neutral-400" title={log.targetId}>
                    {shortId(log.targetId)}
                  </span>
                  {log.outcome !== "Succeeded" && <StatusBadge status={log.outcome} tone="danger" />}
                </div>
                <p className="mt-1 flex items-center gap-1.5 text-sm text-neutral-600">
                  {isSystem && <Bot className="size-4 text-neutral-400" aria-hidden="true" />}
                  <span>
                    by <span className="font-medium text-neutral-800">{log.actorName}</span>
                    {!isSystem && log.actorEmail && (
                      <span className="text-neutral-400"> ({log.actorEmail})</span>
                    )}
                    {isSystem && <span className="text-neutral-400"> (automatic)</span>}
                  </span>
                </p>
                {log.reason && (
                  <p className="mt-2 line-clamp-2 rounded-md border-l-2 border-neutral-300 bg-neutral-50 px-3 py-2 text-sm text-neutral-700">
                    {log.reason}
                  </p>
                )}
              </div>

              <div className="ml-auto flex shrink-0 items-center gap-3">
                <time dateTime={log.createdAt} className="text-sm font-medium tabular-nums text-neutral-500">
                  {formatAuditTime(log.createdAt)}
                </time>
                <button
                  type="button"
                  onClick={() => onInspect(log)}
                  aria-label={`View details of ${formatAuditAction(log.action)}`}
                  className="rounded-md border border-neutral-200 bg-white px-3 py-1.5 text-sm font-medium text-primary-700 shadow-sm transition-colors hover:bg-primary-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500"
                >
                  Details
                </button>
              </div>
            </li>
          </Fragment>
        );
      })}
    </ul>
  );
}
