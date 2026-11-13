import { submitUploadDraft } from "./submitUploadDraft";
import { toast } from "@/shared/stores/toastStore";
import { isUploadCancelled } from "./uploadErrors";
import { Modal, Button } from "@/shared/components/ui";
import { useCallback, useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  FileText,
  LoaderCircle,
  PauseCircle,
} from "lucide-react";
import { useAuthStore } from "@/shared/stores/authStore";
import { getApiErrorMessage } from "@/shared/api";
import {
  isUploadDraftActive,
  canCancelUpload,
  cancelRunningUpload,
  listUploadDrafts,
  removeUploadDraft,
  releaseActiveUploadDrafts,
  releaseUploadDraft,
  type UploadDraft,
} from "./uploadDraftStore";
import { uploadApi } from "./uploadApi";
import { uploadPendingFiles } from "./uploadDrafts";

export function UploadDraftResume() {
  const navigate = useNavigate();
  const location = useLocation();
  const userId = useAuthStore((state) => state.user?.id);
  const [drafts, setDrafts] = useState<UploadDraft[]>([]);
  const [busyId, setBusyId] = useState<string>();
  const [error, setError] = useState("");
  const [cancelTarget, setCancelTarget] = useState<UploadDraft | null>(null);
  const [collapsed, setCollapsed] = useState(true);
  const refreshRequestId = useRef(0);
  const activeUploads = drafts.filter((draft) => isUploadDraftActive(draft.id));
  const resumableDrafts = drafts.filter((draft) => !isUploadDraftActive(draft.id));
  const uploadsInProgress = activeUploads.filter(
    (draft) => draft.files.length === 0 || draft.files.some((file) => file.status !== "uploaded"),
  ).length;
  const uploadedFileCount = activeUploads.reduce(
    (count, draft) => count + draft.files.filter((file) => file.status === "uploaded").length,
    0,
  );
  const activeFileCount = activeUploads.reduce((count, draft) => count + draft.files.length, 0);
  const activeProgress =
    activeFileCount > 0 ? Math.round((uploadedFileCount / activeFileCount) * 100) : 0;

  const refresh = useCallback(async () => {
    const requestId = ++refreshRequestId.current;
    if (!userId) {
      setDrafts([]);
      setError("");
      return;
    }
    try {
      const nextDrafts = await listUploadDrafts(userId);
      if (requestId !== refreshRequestId.current) return;
      setDrafts(nextDrafts);
      setError("");
    } catch (refreshError) {
      if (requestId !== refreshRequestId.current) return;
      setError(getApiErrorMessage(refreshError));
    }
  }, [userId]);

  useEffect(() => {
    const initialRefresh = window.setTimeout(() => void refresh(), 0);
    const handlePageHide = () => releaseActiveUploadDrafts();
    const handlePageShow = () => void refresh();
    window.addEventListener("upload-drafts:changed", refresh);
    window.addEventListener("pagehide", handlePageHide);
    window.addEventListener("pageshow", handlePageShow);
    return () => {
      window.clearTimeout(initialRefresh);
      window.removeEventListener("upload-drafts:changed", refresh);
      window.removeEventListener("pagehide", handlePageHide);
      window.removeEventListener("pageshow", handlePageShow);
    };
  }, [location.key, refresh]);

  useEffect(() => {
    if (drafts.length === 0) return;
    const expiresAt = Math.min(...drafts.map((draft) => draft.expiresAt));
    const timeout = window.setTimeout(
      () => void refresh(),
      Math.max(0, expiresAt - Date.now()) + 1,
    );
    return () => window.clearTimeout(timeout);
  }, [drafts, refresh]);

  if (activeUploads.length === 0 && resumableDrafts.length === 0 && !error) return null;

  async function resume(draft: UploadDraft) {
    if (busyId || isUploadDraftActive(draft.id)) return;
    setBusyId(draft.id);
    setError("");
    try {
      await uploadPendingFiles(draft, undefined, true);
      await submitUploadDraft(draft);
      await removeUploadDraft(draft.id);
      toast.success("Your saved task has been submitted successfully.");
      await refresh();
    } catch (resumeError) {
      releaseUploadDraft(draft.id);
      if (!isUploadCancelled(resumeError)) {
        toast.error(getApiErrorMessage(resumeError));
        navigate(draft.path, { state: { uploadDraftId: draft.id } });
      }
      await refresh();
    } finally {
      releaseUploadDraft(draft.id);
      setBusyId(undefined);
    }
  }

  async function cancel(draft: UploadDraft) {
    if (busyId) return;
    setBusyId(draft.id);
    setError("");
    let remoteCancelError: unknown;
    if (draft.sessionId) {
      try {
        await uploadApi.cancelSession(draft.sessionId);
      } catch (cancelError) {
        remoteCancelError = cancelError;
      }
    }

    try {
      await removeUploadDraft(draft.id);
      await refresh();
      if (remoteCancelError) {
        setError(
          `The saved draft was deleted from this browser, but its temporary server upload could not be canceled: ${getApiErrorMessage(remoteCancelError)}`,
        );
      }
    } catch (removeError) {
      setError(getApiErrorMessage(removeError));
    } finally {
      setBusyId(undefined);
      setCancelTarget(null);
    }
  }

  return (
    <>
      <aside
        aria-label="Upload activity"
        className={`fixed bottom-3 z-50 rounded-xl border border-primary-200 bg-bg-surface shadow-xl sm:bottom-6 ${
          collapsed
            ? "right-3 w-72 max-w-[calc(100vw-1.5rem)] p-1.5 sm:right-6"
            : "right-3 w-96 max-w-[calc(100vw-1.5rem)] p-4 sm:right-6"
        }`}
      >
        <button
          type="button"
          aria-label={collapsed ? "Expand upload panel" : "Collapse upload panel"}
          aria-expanded={!collapsed}
          aria-controls="upload-draft-panel-content"
          onClick={() => setCollapsed((current) => !current)}
          className={`flex min-h-11 items-center gap-2 rounded-lg text-sm font-semibold text-text-primary hover:bg-primary-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 ${
            collapsed ? "px-3" : "w-full px-2"
          } ${collapsed ? "w-full" : ""}`}
        >
          {collapsed ? (
            <>
              <span className="min-w-0 flex-1">
                {uploadsInProgress > 0 ? (
                  <span className="flex flex-col gap-1">
                    <span className="flex items-center justify-between gap-3">
                      <span>Uploading {uploadsInProgress}</span>
                      {activeFileCount > 0 && (
                        <span className="text-xs tabular-nums text-text-secondary">
                          {uploadedFileCount}/{activeFileCount} files
                        </span>
                      )}
                    </span>
                    {activeFileCount > 0 && (
                      <span
                        role="progressbar"
                        aria-label="Overall upload progress"
                        aria-valuemin={0}
                        aria-valuemax={activeFileCount}
                        aria-valuenow={uploadedFileCount}
                        aria-valuetext={`${uploadedFileCount} of ${activeFileCount} files uploaded`}
                        className="h-1.5 w-full overflow-hidden rounded-full bg-primary-100"
                      >
                        <span
                          className="block h-full rounded-full bg-primary-700 transition-[width] motion-reduce:transition-none"
                          style={{ width: `${activeProgress}%` }}
                        />
                      </span>
                    )}
                  </span>
                ) : activeUploads.length > 0 ? (
                  `Finishing (${activeUploads.length})`
                ) : (
                  `Saved uploads (${resumableDrafts.length})`
                )}
              </span>
              <ChevronUp aria-hidden="true" className="size-4" />
            </>
          ) : (
            <>
              <span className="flex-1 text-left">Upload activity ({drafts.length})</span>
              <ChevronDown aria-hidden="true" className="size-4" />
            </>
          )}
        </button>
        <p role="status" className="sr-only">
          {uploadsInProgress > 0
            ? `${uploadsInProgress} uploads in progress. ${uploadedFileCount} of ${activeFileCount} files uploaded.`
            : `${resumableDrafts.length} saved uploads available to continue.`}
        </p>
        <div id="upload-draft-panel-content" hidden={collapsed} className="mt-3">
          <div className="max-h-[50dvh] space-y-3 overflow-y-auto overscroll-contain">
            {activeUploads.map((draft) => {
              const uploadedCount = draft.files.filter((file) => file.status === "uploaded").length;
              const complete = draft.files.length > 0 && uploadedCount === draft.files.length;
              const progress = draft.files.length
                ? Math.round((uploadedCount / draft.files.length) * 100)
                : 0;
              return (
                <section key={draft.id} className="rounded-lg border border-border-default p-3">
                  <div className="mb-2 flex items-center justify-between gap-3 text-sm">
                    <span className="min-w-0 truncate font-medium text-text-primary">
                      {draft.title}
                    </span>
                    <span
                      className={`flex shrink-0 items-center gap-1 ${
                        complete ? "font-medium text-primary-700" : "text-text-secondary"
                      }`}
                    >
                      {complete && <CheckCircle2 aria-hidden="true" className="size-4" />}
                      {complete ? "Files uploaded" : "Uploading"}
                    </span>
                  </div>
                  <div className="flex items-center gap-3">
                    <div
                      role="progressbar"
                      aria-label={`Upload progress for ${draft.title}`}
                      aria-valuemin={0}
                      aria-valuemax={draft.files.length}
                      aria-valuenow={uploadedCount}
                      aria-valuetext={`${uploadedCount} of ${draft.files.length} files uploaded`}
                      className="h-2 min-w-0 flex-1 overflow-hidden rounded-full bg-primary-100"
                    >
                      <div
                        className="h-full rounded-full bg-primary-700 transition-[width] motion-reduce:transition-none"
                        style={{ width: `${progress}%` }}
                      />
                    </div>
                    <span className="shrink-0 text-xs tabular-nums text-text-secondary">
                      {uploadedCount}/{draft.files.length}
                    </span>
                  </div>
                  <ul className="mt-3 space-y-2" aria-label={`Files for ${draft.title}`}>
                    {draft.files.map((file) => (
                      <li key={file.id} className="flex items-center gap-2 text-xs">
                        {file.status === "uploaded" ? (
                          <CheckCircle2
                            aria-hidden="true"
                            className="size-4 shrink-0 text-primary-700"
                          />
                        ) : file.status === "uploading" ? (
                          <LoaderCircle
                            aria-hidden="true"
                            className="size-4 shrink-0 animate-spin text-primary-700 motion-reduce:animate-none"
                          />
                        ) : (
                          <FileText
                            aria-hidden="true"
                            className="size-4 shrink-0 text-text-muted"
                          />
                        )}
                        <span
                          className="min-w-0 flex-1 truncate text-text-primary"
                          title={file.file.name}
                        >
                          {file.file.name}
                        </span>
                        <span className="shrink-0 text-text-secondary">
                          {file.status === "uploaded"
                            ? "Uploaded"
                            : file.status === "uploading"
                              ? "Uploading"
                              : "Queued"}
                        </span>
                      </li>
                    ))}
                  </ul>
                  <div className="mt-3 flex items-center justify-between gap-3">
                    <p className="text-xs text-text-secondary">
                      {canCancelUpload(draft.id)
                        ? "Stop uploading to edit or continue later. Your draft will be kept."
                        : "Finishing this operation…"}
                    </p>
                    <button
                      type="button"
                      disabled={!canCancelUpload(draft.id)}
                      onClick={() => cancelRunningUpload(draft.id)}
                      className="min-h-11 shrink-0 rounded-lg border border-danger-soft px-3 py-2 text-sm font-semibold text-danger-strong hover:bg-danger-soft focus-visible:outline-2 focus-visible:outline-danger-strong disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      Cancel upload
                    </button>
                  </div>
                  {complete && (
                    <p className="mt-3 text-xs text-text-secondary">
                      Files uploaded. Your form still needs to finish submitting. If you left the
                      form, continue from your saved work.
                    </p>
                  )}
                </section>
              );
            })}
            {resumableDrafts.length > 0 && (
              <>
                <h2 className="text-sm font-semibold text-text-primary">
                  Paused · ready to continue
                </h2>
                <p className="mt-1 text-sm text-text-secondary">
                  Edit your form before uploading again, or continue the upload now. Saved files and
                  form data expire 24 hours after starting.
                </p>
              </>
            )}
            {resumableDrafts.map((draft) => (
              <div key={draft.id} className="flex flex-wrap items-center justify-between gap-3">
                <div className="min-w-0">
                  <p className="flex items-center gap-2 text-sm font-medium text-text-primary">
                    <PauseCircle aria-hidden="true" className="size-4 shrink-0" />
                    <span className="truncate">{draft.title}</span>
                  </p>
                  <p className="text-xs text-text-secondary">
                    {draft.files.filter((file) => file.status === "uploaded").length}/
                    {draft.files.length} files uploaded
                  </p>
                  <p className="mt-1 text-xs text-text-secondary">
                    Saved until {new Date(draft.expiresAt).toLocaleString()}
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <button
                    type="button"
                    disabled={Boolean(busyId)}
                    onClick={() => {
                      if (isUploadDraftActive(draft.id)) return;
                      navigate(draft.path, { state: { uploadDraftId: draft.id } });
                    }}
                    className="min-h-11 rounded-lg border border-primary-300 px-4 py-2 text-sm font-semibold text-primary-800 hover:bg-primary-50 focus-visible:outline-2 focus-visible:outline-primary-500 disabled:opacity-60"
                  >
                    Edit form
                  </button>
                  <button
                    type="button"
                    disabled={Boolean(busyId)}
                    onClick={() => void resume(draft)}
                    className="min-h-11 rounded-lg bg-primary-700 px-4 py-2 text-sm font-semibold text-white hover:bg-primary-800 disabled:cursor-wait disabled:opacity-60"
                  >
                    {busyId === draft.id ? "Resuming…" : "Continue"}
                  </button>
                  <button
                    type="button"
                    disabled={Boolean(busyId)}
                    onClick={() => setCancelTarget(draft)}
                    className="min-h-11 rounded-lg border border-danger px-4 py-2 text-sm font-semibold text-danger hover:bg-red-50 disabled:cursor-wait disabled:opacity-60"
                  >
                    Cancel
                  </button>
                </div>
              </div>
            ))}
            {error && (
              <p role="alert" className="text-sm text-danger-strong">
                {error}
              </p>
            )}
          </div>
        </div>
      </aside>
      <Modal
        isOpen={Boolean(cancelTarget)}
        onClose={() => {
          if (!busyId) setCancelTarget(null);
        }}
        closeOnClickOutside={!busyId}
        title="Delete this saved upload?"
      >
        <p className="text-sm text-text-secondary">
          Cancel “{cancelTarget?.title}” and delete its saved files and form information? You will
          need to select the files and enter the information again.
        </p>
        <div className="mt-5 flex flex-wrap justify-end gap-3">
          <Button
            variant="secondary"
            disabled={Boolean(busyId)}
            onClick={() => setCancelTarget(null)}
          >
            Keep draft
          </Button>
          <Button
            variant="danger"
            disabled={Boolean(busyId)}
            onClick={() => {
              if (cancelTarget) void cancel(cancelTarget);
            }}
          >
            {busyId ? "Deleting…" : "Delete draft"}
          </Button>
        </div>
      </Modal>
    </>
  );
}
