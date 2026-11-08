import type { UploadDraft } from "./uploadDraftStore";

export interface UploadTaskScope {
  purpose: string;
  targetIds?: string[];
  operation?: string;
  withoutTarget?: boolean;
}

export function matchesUploadTask(draft: UploadDraft, scope: UploadTaskScope) {
  if (draft.purpose !== scope.purpose) return false;
  if (scope.withoutTarget && draft.targetId) return false;
  if (scope.targetIds && (!draft.targetId || !scope.targetIds.includes(draft.targetId)))
    return false;
  if (scope.operation) {
    const data = draft.formData as { operation?: string; kind?: string } | null;
    return (data?.operation ?? data?.kind) === scope.operation;
  }
  return true;
}
