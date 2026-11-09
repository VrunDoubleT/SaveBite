import { AuditArchivePanel } from "@/features/audit-logs/components/AuditArchivePanel";
import {
  AuditLogDetailModal,
  AuditLogFilterBar,
  AuditLogTable,
} from "@/features/audit-logs/components";
import { AUDIT_LOG_PAGE_SIZE, useAuditLogs } from "@/features/audit-logs/hooks/useAuditLogs";
import { ErrorModal } from "@/shared/components/modal/ErrorModal";
import { Pagination } from "@/shared/components/pagination";

export function AdminAuditLogsPage() {
  const {
    logs,
    filterOptions,
    loading,
    errorMessage,
    clearError,
    page,
    setPage,
    totalItems,
    totalPages,
    search,
    updateSearch,
    actorType,
    updateActorType,
    targetType,
    updateTargetType,
    action,
    updateAction,
    fromDate,
    updateFromDate,
    toDate,
    updateToDate,
    identifiers,
    updateIdentifiers,
    hasActiveFilters,
    resetFilters,
    selectedLog,
    setSelectedLog,
  } = useAuditLogs();

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Audit Logs</h1>
          <p className="mt-1 text-sm text-neutral-500">
            Who did what, to which record, and when. Secrets are never shown.
          </p>
        </div>
      </header>

      <AuditArchivePanel />
      <AuditLogFilterBar
        identifiers={identifiers}
        onIdentifiersChange={updateIdentifiers}
        search={search}
        onSearchChange={updateSearch}
        actorType={actorType}
        onActorTypeChange={updateActorType}
        targetType={targetType}
        onTargetTypeChange={updateTargetType}
        action={action}
        onActionChange={updateAction}
        fromDate={fromDate}
        onFromDateChange={updateFromDate}
        toDate={toDate}
        onToDateChange={updateToDate}
        filterOptions={filterOptions}
        hasActiveFilters={hasActiveFilters}
        onReset={resetFilters}
      />

      <div className="overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-sm">
        <AuditLogTable logs={logs} loading={loading} onInspect={setSelectedLog} />

        {totalItems > 0 && (
          <div className="border-t border-neutral-200 p-4">
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              totalItems={totalItems}
              pageSize={AUDIT_LOG_PAGE_SIZE}
              itemLabel="audit entries"
              onPageChange={setPage}
              showFirstLast={true}
            />
          </div>
        )}
      </div>

      <AuditLogDetailModal log={selectedLog} onClose={() => setSelectedLog(null)} />
      <ErrorModal message={errorMessage} onClose={clearError} />
    </div>
  );
}
