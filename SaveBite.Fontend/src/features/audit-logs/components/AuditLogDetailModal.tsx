import { useEffect, type ReactNode } from "react";
import type { AuditLogItem } from "../types/auditLog.types";
import { ChangedValues } from "@/features/users/components/ChangedValues";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { StatusBadge } from "@/shared/components/analytics";
import { TONE } from "@/shared/components/analytics/tones";
import { formatDateTime } from "@/features/store-analytics/utils/formatAnalytics";
import { auditActionStyle, formatAuditAction } from "../utils/formatAuditLog";

interface AuditLogDetailModalProps {
  log: AuditLogItem | null;
  onClose: () => void;
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs text-neutral-500">{label}</dt>
      <dd className="mt-0.5 break-words text-sm text-neutral-900">{children}</dd>
    </div>
  );
}

function Snapshot({ title, values, emptyText }: { title: string; values: Record<string, unknown> | null; emptyText: string }) {
  return (
    <div className="min-w-0">
      <h3 className="mb-2 text-sm font-semibold text-neutral-900">{title}</h3>
      {values ? (
        <ChangedValues values={values} />
      ) : (
        <p className="rounded-md border border-dashed border-neutral-200 p-3 text-sm text-neutral-400">
          {emptyText}
        </p>
      )}
    </div>
  );
}

export function AuditLogDetailModal({ log, onClose }: AuditLogDetailModalProps) {
  // The shared modal does not react to the keyboard, so Escape is handled here while this dialog is open.
  useEffect(() => {
    if (!log) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [log, onClose]);

  return (
    <SharedModal
      isOpen={Boolean(log)}
      onClose={onClose}
      title="Audit entry"
      maxWidthClass="max-w-2xl"
    >
      {log && (
        <div className="space-y-6">
          <div className="flex items-start gap-3">
            <span
              className={`flex size-12 shrink-0 items-center justify-center rounded-full ${TONE[auditActionStyle(log.action).tone].icon}`}
              aria-hidden="true"
            >
              {(() => {
                const ActionIcon = auditActionStyle(log.action).icon;
                return <ActionIcon className="size-6" />;
              })()}
            </span>
            <div>
              <p className="text-lg font-semibold text-neutral-900">{formatAuditAction(log.action)}</p>
              <p className="mt-0.5 text-sm text-neutral-500">
                {formatDateTime(log.createdAt)} (Vietnam time)
              </p>
            </div>
          </div>

          <dl className="grid grid-cols-1 gap-x-6 gap-y-4 border-y border-neutral-100 py-5 sm:grid-cols-2">
            <Field label="Done by">
              <span className="font-medium">{log.actorName}</span>
              <span className="block text-xs text-neutral-500">
                {log.actorEmail ?? (log.actorType === "System" ? "Automatic action" : "Administrator")}
              </span>
            </Field>
            <Field label="Result">
              <StatusBadge status={log.outcome} tone={log.outcome === "Succeeded" ? "primary" : "danger"} />
            </Field>
            <Field label="Record">
              <span className="font-medium">{log.targetType}</span>
              <span className="block font-mono text-xs text-neutral-500">{log.targetId}</span>
            </Field>
            <Field label="Action code">
              <span className="font-mono text-xs">{log.action}</span>
            </Field>
            {log.correlationId && (
              <Field label="Correlation ID">
                <span className="font-mono text-xs">{log.correlationId}</span>
              </Field>
            )}
            <Field label="Entry ID">
              <span className="font-mono text-xs">{log.id}</span>
            </Field>
          </dl>

          {log.reason && (
            <div>
              <h3 className="mb-1 text-sm font-semibold text-neutral-900">Reason</h3>
              <p className="text-sm leading-relaxed text-neutral-700">{log.reason}</p>
            </div>
          )}

          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
            <Snapshot title="Before" values={log.oldValues} emptyText="Nothing was recorded before this change." />
            <Snapshot title="After" values={log.newValues} emptyText="Nothing was recorded after this change." />
          </div>
        </div>
      )}
    </SharedModal>
  );
}
