import { Button } from "@/shared/components/ui";
import type { AuditArchive, AuditArchiveStatus } from "../types/auditArchive.types";

const STATUS_DETAILS: Partial<Record<AuditArchiveStatus, { className: string; message: string }>> =
  {
    Missing: { className: "text-amber-700", message: "File no longer exists on storage." },
    Deleted: { className: "text-neutral-500", message: "File deleted through the system." },
  };

interface AuditArchiveTableProps {
  archives: AuditArchive[];
  busy: boolean;
  onView: (archive: AuditArchive) => void;
  onDownload: (archive: AuditArchive) => void;
}

export function AuditArchiveTable({ archives, busy, onView, onDownload }: AuditArchiveTableProps) {
  return (
    <div className="min-w-0 max-w-full overflow-x-auto">
      <table className="w-full min-w-[560px] table-fixed text-left text-sm">
        <colgroup>
          <col className="w-[25%]" />
          <col className="w-[20%]" />
          <col className="w-[20%]" />
          <col className="w-[35%]" />
        </colgroup>
        <thead className="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-text-secondary">
          <tr className="border-b border-border-default">
            <th className="px-3 py-3">Month</th>
            <th className="px-3 py-3">Status</th>
            <th className="px-3 py-3">Records / size</th>
            <th className="px-3 py-3">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border-default text-sm text-text-primary">
          {archives.map((archive) => (
            <tr key={archive.id} className="border-b">
              <td className="px-3 py-3">
                <p className="font-semibold">
                  {archive.year}-{String(archive.month).padStart(2, "0")}
                </p>
                <details className="mt-2 text-xs text-neutral-500">
                  <summary className="cursor-pointer">Details</summary>
                  <dl className="mt-2 space-y-2 [overflow-wrap:anywhere]">
                    <div>
                      <dt>Created</dt>
                      <dd>
                        {new Date(archive.createdAt).toLocaleString("en-GB", {
                          timeZone: "Asia/Ho_Chi_Minh",
                        })}
                      </dd>
                    </div>
                    <div>
                      <dt>Archive ID</dt>
                      <dd>{archive.id}</dd>
                    </div>
                    <div>
                      <dt>SHA-256</dt>
                      <dd>{archive.sha256}</dd>
                    </div>
                  </dl>
                </details>
              </td>
              <td className="px-3 py-3">
                <span className={STATUS_DETAILS[archive.status]?.className}>{archive.status}</span>
                {STATUS_DETAILS[archive.status] && (
                  <p className={`text-xs ${STATUS_DETAILS[archive.status]?.className}`}>
                    {STATUS_DETAILS[archive.status]?.message}
                  </p>
                )}
                {archive.error && <p className="max-w-xs text-xs text-red-700">{archive.error}</p>}
              </td>
              <td className="px-3 py-3">
                {archive.recordCount.toLocaleString()} / {(archive.bytes / 1024).toFixed(1)} KB
              </td>
              <td className="px-3 py-3">
                <div className="flex flex-wrap gap-2">
                  <Button
                    variant="secondary"
                    disabled={busy || archive.status !== "Completed"}
                    onClick={() => onView(archive)}
                  >
                    View logs
                  </Button>
                  <Button
                    variant="secondary"
                    disabled={busy || archive.status !== "Completed"}
                    onClick={() => onDownload(archive)}
                  >
                    Download
                  </Button>
                </div>
              </td>
            </tr>
          ))}
          {!archives.length && (
            <tr>
              <td colSpan={4} className="p-6 text-center text-neutral-500">
                No audit archives yet.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
