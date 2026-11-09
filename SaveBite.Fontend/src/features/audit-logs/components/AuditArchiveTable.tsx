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
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b text-neutral-500">
            <th className="p-2">Month</th>
            <th className="p-2">Created</th>
            <th className="p-2">Status</th>
            <th className="p-2">Records / size</th>
            <th className="p-2">Actions</th>
          </tr>
        </thead>
        <tbody>
          {archives.map((archive) => (
            <tr key={archive.id} className="border-b">
              <td className="p-2">
                {archive.year}-{String(archive.month).padStart(2, "0")}
              </td>
              <td className="p-2">{new Date(archive.createdAt).toLocaleString()}</td>
              <td className="p-2">
                <span className={STATUS_DETAILS[archive.status]?.className}>{archive.status}</span>
                {STATUS_DETAILS[archive.status] && (
                  <p className={`text-xs ${STATUS_DETAILS[archive.status]?.className}`}>
                    {STATUS_DETAILS[archive.status]?.message}
                  </p>
                )}
                {archive.error && <p className="max-w-xs text-xs text-red-700">{archive.error}</p>}
              </td>
              <td className="p-2">
                {archive.recordCount.toLocaleString()} / {(archive.bytes / 1024).toFixed(1)} KB
              </td>
              <td className="p-2">
                <div className="flex gap-2">
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
              <td colSpan={5} className="p-6 text-center text-neutral-500">
                No audit archives yet.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
