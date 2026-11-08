import { matchesUploadTask, type UploadTaskScope } from "./uploadTaskScope";
import { useEffect, useState, type ReactNode } from "react";
import { LoaderCircle } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { useAuthStore } from "@/shared/stores/authStore";
import { getActiveUploadDraftIds, isUploadDraftActive, listUploadDrafts } from "./uploadDraftStore";

/** Check work already running when this form is entered, without blocking its own uploads. */
export function UploadTaskGuard({
  scope,
  children,
  onClose,
}: {
  scope: UploadTaskScope;
  children: ReactNode;
  onClose?: () => void;
}) {
  const userId = useAuthStore((state) => state.user?.id);
  const [entryIds] = useState(() => new Set(getActiveUploadDraftIds()));
  const [state, setState] = useState<"checking" | "blocked" | "ready" | "error">("checking");
  const scopeKey = JSON.stringify(scope);

  useEffect(() => {
    let mounted = true;
    let request = 0;
    async function check() {
      const current = ++request;
      try {
        const drafts = userId && entryIds.size ? await listUploadDrafts(userId) : [];
        if (!mounted || current !== request) return;
        const parsed = JSON.parse(scopeKey) as UploadTaskScope;
        setState(
          drafts.some(
            (draft) =>
              entryIds.has(draft.id) &&
              isUploadDraftActive(draft.id) &&
              matchesUploadTask(draft, parsed),
          )
            ? "blocked"
            : "ready",
        );
      } catch {
        if (mounted && current === request) setState("error");
      }
    }
    void check();
    window.addEventListener("upload-drafts:changed", check);
    return () => {
      mounted = false;
      window.removeEventListener("upload-drafts:changed", check);
    };
  }, [entryIds, scopeKey, userId]);

  if (state === "ready") return children;
  const notice = (
    <div
      role="status"
      className="rounded-lg border border-primary-200 bg-primary-50 p-5 text-sm text-primary-800"
    >
      <p className="flex items-center gap-2 font-semibold">
        <LoaderCircle
          aria-hidden="true"
          className="size-5 shrink-0 animate-spin motion-reduce:animate-none"
        />
        {state === "checking"
          ? "Checking upload activity…"
          : state === "error"
            ? "Unable to check upload activity"
            : "There is already 1 process running for this task"}
      </p>
      {state === "blocked" && (
        <p className="mt-2">
          Please wait for the current upload to finish before submitting this form again. You can
          follow its progress in the upload panel.
        </p>
      )}
      {state === "error" && <p className="mt-2">Please close and reopen this form to try again.</p>}
    </div>
  );
  return onClose ? (
    <SharedModal isOpen onClose={onClose} title="Upload activity" maxWidthClass="max-w-xl">
      {notice}
    </SharedModal>
  ) : (
    notice
  );
}
