import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import type { ArchiveFilters } from "../utils/filterAuditArchive";
import type { AuditLogFilterOptions } from "../types/auditLog.types";
import { AuditLogFilterBar } from "./AuditLogFilterBar";
import { ChangedValues } from "@/features/users/components/ChangedValues";
import { auditActionVisual } from "../utils/formatAuditLog";
import { formatDateTime } from "@/features/store-analytics/utils/formatAnalytics";
import { TONE } from "@/shared/components/analytics/tones";
import { Modal, Pagination } from "@/shared/components/ui";
import { ARCHIVE_PAGE_SIZE, type AuditArchiveRow } from "../types/auditArchive.types";

interface AuditArchiveViewerProps {
  filters: ArchiveFilters;
  filterOptions: AuditLogFilterOptions;
  error: string;
  onFiltersChange: (filters: ArchiveFilters) => void;
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
  filters,
  filterOptions,
  error,
  onFiltersChange,
  open,
  source,
  rows,
  totalRows,
  page,
  loading,
  onPageChange,
  onClose,
}: AuditArchiveViewerProps) {
  const set = (patch: Partial<ArchiveFilters>) => onFiltersChange({ ...filters, ...patch });
  return (
    <Modal
      isOpen={open}
      onClose={onClose}
      title="Archived audit logs"
      closeOnClickOutside={false}
      maxWidthClass="max-w-5xl"
    >
      <p className="mb-3 break-all text-xs text-neutral-500">{source}</p>
      <AuditLogFilterBar
        identifiers={{
          actorUserId: filters.actorUserId,
          targetId: filters.targetId,
          correlationId: filters.correlationId,
        }}
        onIdentifiersChange={set}
        search={filters.search}
        onSearchChange={(search) => set({ search })}
        actorType={filters.actorType}
        onActorTypeChange={(actorType) => set({ actorType })}
        targetType={filters.targetType}
        onTargetTypeChange={(targetType) => set({ targetType, action: "" })}
        action={filters.action}
        onActionChange={(action) => set({ action })}
        fromDate={filters.fromDate}
        onFromDateChange={(fromDate) => set({ fromDate })}
        toDate={filters.toDate}
        onToDateChange={(toDate) => set({ toDate })}
        filterOptions={filterOptions}
        hasActiveFilters={Object.values(filters).some(Boolean)}
        onReset={() =>
          onFiltersChange({
            search: "",
            actorType: "",
            targetType: "",
            action: "",
            fromDate: "",
            toDate: "",
            actorUserId: "",
            targetId: "",
            correlationId: "",
          })
        }
      />
      <p role="status" className="my-3 text-sm text-neutral-600">
        {totalRows.toLocaleString()} matching audit entries
      </p>
      {error && (
        <p role="alert" className="my-3 rounded-md bg-red-50 p-3 text-sm text-red-700">
          {error}
        </p>
      )}
      {loading ? (
        <LoadingSkeleton variant="list" label="Loading archived records…" />
      ) : (
        <div className="max-h-[60vh] space-y-3 overflow-y-auto">
          {rows.map((row, index) => (
            <details key={`${row.id}-${index}`} className="rounded-lg border p-3">
              <summary className="cursor-pointer text-sm font-medium">
                {(() => {
                  const visual = auditActionVisual(row.action);
                  const Icon = visual.icon;
                  return (
                    <span
                      className={`inline-flex items-center gap-2 rounded-md px-2 py-1 ${TONE[visual.tone].icon}`}
                    >
                      <Icon className="size-4 shrink-0" aria-hidden="true" />
                      {visual.label}
                    </span>
                  );
                })()}{" "}
                · {row.targetType} · {formatDateTime(row.createdAt)}
              </summary>
              <div className="mt-3 grid min-w-0 gap-4 sm:grid-cols-2">
                {(["oldValues", "newValues"] as const).map((key) => (
                  <div key={key} className="min-w-0">
                    <h4 className="text-sm font-semibold">
                      {key === "oldValues" ? "Before" : "After"}
                    </h4>
                    {row[key] && typeof row[key] === "object" ? (
                      <ChangedValues values={row[key] as Record<string, unknown>} />
                    ) : (
                      <p className="mt-2 whitespace-pre-wrap text-sm [overflow-wrap:anywhere]">
                        {row[key] == null ? "Nothing recorded." : String(row[key])}
                      </p>
                    )}
                  </div>
                ))}
              </div>
              <p className="mt-3 text-xs text-neutral-500">Record details</p>
              <pre className="mt-2 min-w-0 whitespace-pre-wrap [overflow-wrap:anywhere] text-xs">
                {JSON.stringify(row, null, 2)}
              </pre>
            </details>
          ))}
          {!rows.length && <p>No audit entries match these filters.</p>}
        </div>
      )}
      <div className="mt-4">
        <Pagination
          currentPage={page}
          totalPages={Math.max(1, Math.ceil(totalRows / ARCHIVE_PAGE_SIZE))}
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
