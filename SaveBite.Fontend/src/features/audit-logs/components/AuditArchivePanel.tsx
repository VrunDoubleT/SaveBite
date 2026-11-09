import { ChevronDown } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { Button, Modal } from "@/shared/components/ui";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";

import { AuditArchiveTable } from "./AuditArchiveTable";
import { AuditArchiveViewer } from "./AuditArchiveViewer";
import { auditArchiveApi } from "../api/auditArchiveApi";
import {
  ARCHIVE_PAGE_SIZE,
  type AuditArchive,
  type AuditArchiveRow,
} from "../types/auditArchive.types";
import { parseAuditArchive } from "../utils/parseAuditArchive";

export function AuditArchivePanel() {
  const [expanded, setExpanded] = useState(false);
  const [archives, setArchives] = useState<AuditArchive[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [selected, setSelected] = useState<AuditArchive>();
  const fileInput = useRef<HTMLInputElement>(null);
  const [localFile, setLocalFile] = useState<{ name: string; rows: AuditArchiveRow[] }>();
  const [readingFile, setReadingFile] = useState(false);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<AuditArchiveRow[]>([]);
  const [loadingRows, setLoadingRows] = useState(false);
  const [confirmArchive, setConfirmArchive] = useState(false);
  const [month, setMonth] = useState(() => {
    const now = new Date(Date.now() + 7 * 60 * 60 * 1000);
    const previous = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 1, 1));
    return previous.toISOString().slice(0, 7);
  });
  const refresh = useCallback(async () => {
    setArchives(await auditArchiveApi.list());
  }, []);
  useEffect(() => {
    void Promise.resolve()
      .then(refresh)
      .catch((e: unknown) => setError(getApiErrorMessage(e)));
  }, [refresh]);
  useEffect(() => {
    if (!selected) return;
    const controller = new AbortController();
    void Promise.resolve().then(() => {
      if (!controller.signal.aborted) {
        setLoadingRows(true);
        setRows([]);
      }
    });
    void auditArchiveApi
      .logs(selected.id, page, controller.signal)
      .then((response) => {
        if (!controller.signal.aborted) setRows(response.items);
      })
      .catch((e: unknown) => {
        if (!controller.signal.aborted) setError(getApiErrorMessage(e));
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoadingRows(false);
      });
    return () => controller.abort();
  }, [selected, page]);
  async function run(action: () => Promise<unknown>) {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await action();
      await refresh();
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      setBusy(false);
      setConfirmArchive(false);
    }
  }
  async function archiveMonth() {
    const response = await auditArchiveApi.create(month);
    if (response.data == null) {
      toast.info(
        response.message ?? "No audit logs remain for this month. No archive was created.",
      );
    } else {
      toast.success("Audit logs archived successfully.");
    }
  }
  async function openLocalFile(file: File) {
    setReadingFile(true);
    try {
      const parsed = parseAuditArchive(await file.text());
      if (!parsed.length) {
        toast.info("This file contains no audit logs.");
        return;
      }
      setSelected(undefined);
      setLoadingRows(false);
      setLocalFile({ name: file.name, rows: parsed });
      setPage(1);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : "Unable to read this JSONL file.");
    } finally {
      setReadingFile(false);
    }
  }
  const visibleRows = localFile
    ? localFile.rows.slice((page - 1) * ARCHIVE_PAGE_SIZE, page * ARCHIVE_PAGE_SIZE)
    : rows;
  const totalRows = localFile?.rows.length ?? selected?.recordCount ?? 0;
  async function download(archive: AuditArchive) {
    const blob = await auditArchiveApi.download(archive.id);
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `audit-logs-${archive.year}-${String(archive.month).padStart(2, "0")}.zip`;
    link.click();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
  return (
    <section
      className="rounded-xl border border-neutral-200 bg-white p-5"
      aria-labelledby="audit-archives-title"
    >
      <button
        type="button"
        onClick={() => setExpanded((value) => !value)}
        aria-expanded={expanded}
        aria-controls="audit-archives-content"
        className="flex min-h-11 w-full items-center justify-between gap-3 rounded-md text-left focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500"
      >
        <span id="audit-archives-title" className="text-lg font-semibold">
          Audit log archives
        </span>
        <span className="flex items-center gap-2 text-sm text-neutral-500">
          {busy ? "Processing…" : expanded ? "Collapse" : "Expand"}
          <ChevronDown
            aria-hidden="true"
            className={`size-5 transition-transform motion-reduce:transition-none ${expanded ? "rotate-180" : ""}`}
          />
        </span>
      </button>
      <p className="mt-1 text-sm text-neutral-600">
        Runs with platform fees: day 1, 07:00 Vietnam time. Source records are removed only after
        the archive is verified. Files are retained.
      </p>
      <div id="audit-archives-content" hidden={!expanded}>
        <div className="space-y-4 pt-4">
          {error && (
            <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
              {error}
            </p>
          )}
          <div className="flex flex-wrap items-end gap-3">
            <label className="text-sm font-medium">
              Completed month
              <input
                type="month"
                value={month}
                onChange={(e) => setMonth(e.target.value)}
                className="mt-1 block min-h-11 rounded-md border border-neutral-300 px-3"
              />
            </label>
            <Button disabled={busy || !month} onClick={() => setConfirmArchive(true)}>
              Archive month
            </Button>
            <input
              ref={fileInput}
              type="file"
              accept=".jsonl"
              className="hidden"
              aria-label="Open local audit JSONL file"
              onChange={(event) => {
                const file = event.target.files?.[0];
                event.target.value = "";
                if (file) void openLocalFile(file);
              }}
            />
            <Button
              variant="secondary"
              disabled={readingFile}
              onClick={() => fileInput.current?.click()}
            >
              {readingFile ? "Reading file…" : "Open JSONL file"}
            </Button>
          </div>
          {busy && (
            <p role="status" className="text-sm text-primary-700">
              Processing archive…
            </p>
          )}
          <AuditArchiveTable
            archives={archives}
            busy={busy}
            onView={(archive) => {
              setLocalFile(undefined);
              setSelected(archive);
              setPage(1);
            }}
            onDownload={(archive) => void run(() => download(archive))}
          />
        </div>
      </div>
      <AuditArchiveViewer
        open={Boolean(selected || localFile)}
        source={
          localFile
            ? `${localFile.name} · Local file · ${totalRows.toLocaleString()} records`
            : selected
              ? `${selected.year}-${selected.month} · SHA-256: ${selected.sha256}`
              : ""
        }
        rows={visibleRows}
        totalRows={totalRows}
        page={page}
        loading={loadingRows}
        onPageChange={setPage}
        onClose={() => {
          setSelected(undefined);
          setLocalFile(undefined);
        }}
      />
      <Modal
        isOpen={confirmArchive}
        onClose={() => {
          if (!busy) setConfirmArchive(false);
        }}
        closeOnClickOutside={!busy}
        title="Archive this month?"
      >
        <p className="text-sm text-neutral-600">
          {`Archive ${month}? After upload and verification, only the exported audit records will be removed from the database.`}
        </p>
        <div className="mt-5 flex justify-end gap-3">
          <Button variant="secondary" disabled={busy} onClick={() => setConfirmArchive(false)}>
            Cancel
          </Button>
          <Button variant="danger" disabled={busy} onClick={() => void run(archiveMonth)}>
            {busy ? "Processing…" : "Confirm"}
          </Button>
        </div>
      </Modal>
    </section>
  );
}
