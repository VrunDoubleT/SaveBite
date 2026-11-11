import { useEffect, useRef, useState, type ReactNode } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuthStore } from "@/shared/stores/authStore";
import { getApiErrorMessage } from "@/shared/api";
import { Modal, Button } from "@/shared/components/ui";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import {
  isUploadDraftActive,
  listUploadDrafts,
  removeUploadDraft,
  type UploadDraft,
} from "./uploadDraftStore";
import { matchesUploadTask, type UploadTaskScope } from "./uploadTaskScope";
import { uploadApi } from "./uploadApi";

export function UploadDraftChoice({
  scope,
  selectedDraftId,
  children,
  onClose,
}: {
  scope: UploadTaskScope;
  selectedDraftId?: string;
  children: (draftId?: string) => ReactNode;
  onClose?: () => void;
}) {
  const userId = useAuthStore((state) => state.user?.id);
  const location = useLocation();
  const navigate = useNavigate();
  const requestedId =
    selectedDraftId ?? (location.state as { uploadDraftId?: string } | null)?.uploadDraftId;
  const [drafts, setDrafts] = useState<UploadDraft[]>();
  const [choice, setChoice] = useState<{ id?: string }>();
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const working = useRef(false);
  const scopeKey = JSON.stringify(scope);

  useEffect(() => {
    let active = true;
    if (!userId) return;
    void listUploadDrafts(userId)
      .then((all) => {
        if (!active) return;
        const saved = all.filter((draft) => matchesUploadTask(draft, JSON.parse(scopeKey)));
        setDrafts(saved);
        if (requestedId && saved.some((draft) => draft.id === requestedId))
          setChoice({ id: requestedId });
      })
      .catch((err: unknown) => {
        if (active) setError(getApiErrorMessage(err));
      });
    return () => {
      active = false;
    };
  }, [userId, scopeKey, requestedId]);

  async function edit(draft: UploadDraft) {
    if (working.current || !userId) return;
    working.current = true;
    setBusy(true);
    setError("");
    try {
      const saved = (await listUploadDrafts(userId)).find((item) => item.id === draft.id);
      if (!saved || isUploadDraftActive(saved.id))
        throw new Error("This draft is unavailable or is still uploading. Please reopen the form.");
      if (!onClose)
        navigate(`${location.pathname}${location.search}`, {
          replace: true,
          state: { uploadDraftId: saved.id },
        });
      setChoice({ id: saved.id });
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      working.current = false;
      setBusy(false);
    }
  }

  async function discard() {
    if (working.current || !drafts) return;
    working.current = true;
    setBusy(true);
    setError("");
    try {
      for (const draft of drafts) {
        if (isUploadDraftActive(draft.id))
          throw new Error("Stop the running upload before deleting its draft.");
        if (draft.sessionId) await uploadApi.cancelSession(draft.sessionId);
        await removeUploadDraft(draft.id);
      }
      if (!onClose)
        navigate(`${location.pathname}${location.search}`, { replace: true, state: null });
      setDrafts([]);
      setChoice({});
      setConfirmDelete(false);
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      working.current = false;
      setBusy(false);
    }
  }

  if (drafts && (choice || drafts.length === 0)) return children(choice?.id);
  const content = (
    <section
      aria-labelledby="upload-draft-choice-title"
      className="rounded-lg border border-primary-200 bg-bg-surface p-5"
    >
      <h2 id="upload-draft-choice-title" className="text-base font-semibold text-text-primary">
        {drafts ? "You have a paused draft for this task" : "Checking saved work…"}
      </h2>
      {error && (
        <p role="alert" className="mt-3 text-sm text-danger-strong">
          {error}
        </p>
      )}
      {drafts && (
        <>
          <p className="mt-2 text-sm text-text-secondary">
            Edit your saved information and files, or delete this draft and start again. Uploading
            resumes only when you choose to upload or submit.
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            {drafts.map((draft) => (
              <Button key={draft.id} disabled={busy} onClick={() => void edit(draft)}>
                Edit draft{drafts.length > 1 ? ` · ${draft.title}` : ""}
              </Button>
            ))}
            <Button variant="danger" disabled={busy} onClick={() => setConfirmDelete(true)}>
              Delete draft and start again
            </Button>
          </div>
        </>
      )}
      <Modal
        isOpen={confirmDelete}
        onClose={() => {
          if (!busy) setConfirmDelete(false);
        }}
        closeOnClickOutside={!busy}
        title="Delete saved draft?"
      >
        <p className="text-sm text-text-secondary">
          Delete the saved files and form information for this task? You will start with the current
          saved data and no pending changes.
        </p>
        {error && (
          <p role="alert" className="mt-3 text-sm text-danger-strong">
            {error}
          </p>
        )}
        <div className="mt-5 flex flex-wrap justify-end gap-3">
          <Button variant="secondary" disabled={busy} onClick={() => setConfirmDelete(false)}>
            Keep draft
          </Button>
          <Button variant="danger" disabled={busy} onClick={() => void discard()}>
            {busy ? "Deleting…" : "Delete draft"}
          </Button>
        </div>
      </Modal>
    </section>
  );
  return onClose ? (
    <SharedModal
      isOpen
      onClose={() => {
        if (!busy) onClose();
      }}
      closeOnClickOutside={!busy}
      title="Saved work"
      maxWidthClass="max-w-xl"
    >
      {content}
    </SharedModal>
  ) : (
    content
  );
}
