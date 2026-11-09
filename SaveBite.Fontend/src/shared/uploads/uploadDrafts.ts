import { isSameUploadFile, matchUploadFiles, remapDraftAssetIds } from "./uploadDraftFiles";
import { uploadApi } from "./uploadApi";
import {
  expiresIn24Hours,
  getUploadDraft,
  isUploadDraftActive,
  markUploadDraftActive,
  releaseUploadDraft,
  registerUploadCancellation,
  clearUploadCancellation,
  newUploadDraftFile,
  saveUploadDraft,
  type UploadDraft,
  type UploadDraftFile,
} from "./uploadDraftStore";

export interface UploadFileInput {
  id?: string;
  field: string;
  file: File;
}

export interface UploadReference {
  field: string;
  assetId: string;
}

export interface StartUploadDraftInput {
  draftId?: string;
  userId: string;
  purpose: string;
  targetId?: string;
  title: string;
  path: string;
  formData: unknown;
  files: UploadFileInput[];
  appendFiles?: boolean;
  signal?: AbortSignal;
  keepActiveUntilSubmitted?: boolean;
  onQueued?: () => void;
}

export async function startOrResumeUploadDraft(
  input: StartUploadDraftInput,
  existingDraftId?: string,
): Promise<UploadDraft> {
  const id = existingDraftId ?? input.draftId ?? crypto.randomUUID();
  if (isUploadDraftActive(id))
    throw new Error("This task is still running. Follow its progress in the upload panel.");
  const controller = new AbortController();
  registerUploadCancellation(id, controller);
  const signal = input.signal
    ? AbortSignal.any([input.signal, controller.signal])
    : controller.signal;
  try {
    const draft = await runUploadDraft({ ...input, draftId: id, signal }, existingDraftId);
    if (!input.keepActiveUntilSubmitted) releaseUploadDraft(id);
    return draft;
  } catch (error) {
    releaseUploadDraft(id);
    throw error;
  } finally {
    clearUploadCancellation(id);
  }
}

async function runUploadDraft(
  input: StartUploadDraftInput,
  existingDraftId?: string,
): Promise<UploadDraft> {
  const draftId = existingDraftId ?? input.draftId ?? crypto.randomUUID();
  markUploadDraftActive(draftId);
  let draft = existingDraftId ? await getUploadDraft(existingDraftId) : undefined;
  if (draft && (draft.userId !== input.userId || draft.expiresAt <= Date.now())) {
    throw new Error("This saved upload has expired. Please select the files again.");
  }

  if (!draft) {
    const createdAt = Date.now();
    draft = {
      id: draftId,
      userId: input.userId,
      purpose: input.purpose,
      targetId: input.targetId,
      title: input.title,
      path: input.path,
      createdAt,
      expiresAt: expiresIn24Hours(createdAt),
      formData: input.formData,
      files: input.files.map(({ id, field, file }) => newUploadDraftFile(field, file, id)),
    };
    await saveUploadDraft(draft, false);
  } else {
    let replacedSession = false;
    draft.formData = input.formData;
    draft.title = input.title;
    draft.path = input.path;
    if (input.appendFiles) {
      for (const candidate of input.files) {
        const duplicate = draft.files.some((existing) => isSameUploadFile(existing, candidate));
        if (!duplicate)
          draft.files.push(newUploadDraftFile(candidate.field, candidate.file, candidate.id));
      }
    } else {
      const matched = matchUploadFiles(draft.files, input.files);
      if (matched) {
        draft.files = matched;
        remapDraftAssetIds(
          draft,
          new Map(
            input.files.flatMap((file, index) =>
              file.id ? [[file.id, matched[index].id] as const] : [],
            ),
          ),
        );
      }
      if (!matched && draft.sessionId) {
        await uploadApi.cancelSession(draft.sessionId);
        draft.sessionId = undefined;
        replacedSession = true;
        draft.createdAt = Date.now();
        draft.expiresAt = expiresIn24Hours(draft.createdAt);
      }
    }
    if (!draft.sessionId && !input.appendFiles) {
      const remappedIds = new Map<string, string>();
      draft.files = input.files.map(({ id, field, file }) => {
        const next = newUploadDraftFile(field, file, replacedSession ? undefined : id);
        if (id) remappedIds.set(id, next.id);
        return next;
      });
      if (replacedSession) remapDraftAssetIds(draft, remappedIds);
    }
    if (!draft.sessionId && input.appendFiles && input.files.length === 0)
      throw new Error("Select at least one file to start an upload session.");
    await saveUploadDraft(draft, false);
  }

  input.signal?.throwIfAborted();
  input.onQueued?.();
  await runPendingFiles(draft, input.signal);
  return draft;
}

export async function uploadPendingFiles(
  draft: UploadDraft,
  signal?: AbortSignal,
  keepActiveUntilSubmitted = false,
): Promise<void> {
  if (isUploadDraftActive(draft.id))
    throw new Error("This task is still running. Follow its progress in the upload panel.");
  const controller = new AbortController();
  markUploadDraftActive(draft.id);
  registerUploadCancellation(draft.id, controller);
  const combined = signal ? AbortSignal.any([signal, controller.signal]) : controller.signal;
  try {
    await runPendingFiles(draft, combined);
  } catch (error) {
    releaseUploadDraft(draft.id);
    throw error;
  } finally {
    clearUploadCancellation(draft.id);
    if (!keepActiveUntilSubmitted) releaseUploadDraft(draft.id);
  }
}

async function runPendingFiles(draft: UploadDraft, signal?: AbortSignal): Promise<void> {
  if (draft.expiresAt <= Date.now())
    throw new Error("This upload has expired. Please start again.");
  if (!draft.sessionId) {
    const session = await uploadApi.createSession(draft.purpose, draft.targetId);
    draft.sessionId = session.id;
    draft.expiresAt = Math.min(draft.expiresAt, new Date(session.expiresAt).getTime());
    await saveUploadDraft(draft, false);
  }

  const remote = await uploadApi.getSession(draft.sessionId);
  const remoteAssets = new Map(remote.assets.map((asset) => [asset.id, asset]));
  for (const file of draft.files) {
    const saved = remoteAssets.get(file.id);
    // Older drafts may contain asset IDs reserved by a replaced session.
    // Only reuse IDs confirmed to belong to the current server session.
    if (!saved) {
      const oldId = file.id;
      file.id = crypto.randomUUID();
      file.status = "pending";
      file.url = undefined;
      file.metadata = undefined;
      remapDraftAssetIds(draft, new Map([[oldId, file.id]]));
    }
    if (saved && ["Uploaded", "Attached"].includes(saved.status)) {
      file.status = "uploaded";
      file.url = saved.url;
      file.metadata = saved.metadata;
    }
  }
  await saveUploadDraft(draft, false);

  let next = 0;
  const failures: string[] = [];
  const worker = async () => {
    while (!signal?.aborted) {
      const index = next++;
      const file = draft.files[index];
      if (!file) return;
      if (file.status === "uploaded") continue;
      file.status = "uploading";
      await saveUploadDraft(draft, false);
      try {
        const uploaded = await uploadApi.upload(draft.sessionId!, file.id, file.file, signal);
        file.status = "uploaded";
        file.url = uploaded.url;
        file.metadata = uploaded.metadata;
      } catch (error) {
        file.status = "pending";
        failures.push(
          `${file.file.name}: ${error instanceof Error ? error.message : "Upload failed."}`,
        );
      }
      await saveUploadDraft(draft, false);
    }
  };
  await Promise.all(Array.from({ length: Math.min(3, draft.files.length) }, worker));
  await saveUploadDraft(draft);
  signal?.throwIfAborted();
  if (failures.length) throw new Error(failures.join(" "));
}

export function uploadReferences(files: UploadDraftFile[]): UploadReference[] {
  return files.map((file) => ({ field: file.field, assetId: file.id }));
}

export function uploadedUrls(files: UploadDraftFile[]) {
  return files.map((file) => {
    if (!file.url) throw new Error(`The upload for ${file.file.name} is not complete.`);
    return file.url;
  });
}

export type { UploadDraft, UploadDraftFile };
