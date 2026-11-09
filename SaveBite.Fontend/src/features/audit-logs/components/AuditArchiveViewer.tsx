import { Modal, Pagination } from "@/shared/components/ui";
import { ARCHIVE_PAGE_SIZE, type AuditArchiveRow } from "../types/auditArchive.types";

interface AuditArchiveViewerProps {
  open: boolean;
  source: string;
  rows: AuditArchiveRow[];
  totalRows: number;
  page: number;
  loading: boolean;
  onPageChange: (page: number) => void;
  onClose: () => void;
}

export function AuditArchiveViewer({
  open,
  source,
  rows,
  totalRows,
  page,
  loading,
  onPageChange,
  onClose,
}: AuditArchiveViewerProps) {
  return (
    <Modal
      isOpen={open}
      onClose={onClose}
      title="Archived audit logs"
      closeOnClickOutside={false}
      maxWidthClass="max-w-5xl"
    >
      <p className="mb-3 break-all text-xs text-neutral-500">{source}</p>
      {loading ? (
        <p role="status">Loading archived records…</p>
      ) : (
        <div className="max-h-[60vh] space-y-3 overflow-y-auto">
          {rows.map((row, index) => (
            <details key={`${row.id}-${index}`} className="rounded-lg border p-3">
              <summary className="cursor-pointer text-sm font-medium">
                {row.action} · {row.targetType} · {new Date(row.createdAt).toLocaleString()}
              </summary>
              <pre className="mt-3 overflow-x-auto whitespace-pre-wrap break-all text-xs">
                {JSON.stringify(row, null, 2)}
              </pre>
            </details>
          ))}
          {!rows.length && <p>No records on this page.</p>}
        </div>
      )}
      <div className="mt-4">
        <Pagination
          currentPage={page}
          totalPages={Math.ceil(totalRows / ARCHIVE_PAGE_SIZE)}
          totalItems={totalRows}
          pageSize={ARCHIVE_PAGE_SIZE}
          itemLabel="audit entries"
          onPageChange={(nextPage) => {
            if (!loading) onPageChange(nextPage);
          }}
          showFirstLast={true}
        />
      </div>
    </Modal>
  );
}
