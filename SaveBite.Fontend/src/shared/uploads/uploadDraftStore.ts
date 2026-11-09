export interface UploadDraftFile {
  id: string;
  field: string;
  file: File;
  status: "pending" | "uploading" | "uploaded";
  url?: string;
  metadata?: unknown;
}

export interface UploadDraft {
  id: string;
  sessionId?: string;
  userId: string;
  purpose: string;
  targetId?: string;
  title: string;
  path: string;
  createdAt: number;
  expiresAt: number;
  formData: unknown;
  files: UploadDraftFile[];
}

const DATABASE = "savebite-upload-drafts";
const STORE = "drafts";
const DAY = 24 * 60 * 60 * 1000;
const activeDraftIds = new Set<string>();

const uploadControllers = new Map<string, AbortController>();

export function registerUploadCancellation(id: string, controller: AbortController): void {
  uploadControllers.set(id, controller);
  window.dispatchEvent(new Event("upload-drafts:changed"));
}

export function clearUploadCancellation(id: string): void {
  uploadControllers.delete(id);
  window.dispatchEvent(new Event("upload-drafts:changed"));
}

export function canCancelUpload(id: string): boolean {
  const controller = uploadControllers.get(id);
  return Boolean(controller && !controller.signal.aborted);
}

export function cancelRunningUpload(id: string): void {
  uploadControllers.get(id)?.abort();
  window.dispatchEvent(new Event("upload-drafts:changed"));
}

export function getActiveUploadDraftIds(): string[] {
  return [...activeDraftIds];
}

export function markUploadDraftActive(id: string): void {
  activeDraftIds.add(id);
  window.dispatchEvent(new Event("upload-drafts:changed"));
}

export function releaseUploadDraft(id: string): void {
  activeDraftIds.delete(id);
  window.dispatchEvent(new Event("upload-drafts:changed"));
}

export function isUploadDraftActive(id: string): boolean {
  return activeDraftIds.has(id);
}

export function releaseActiveUploadDrafts(): void {
  activeDraftIds.clear();
}

function openDatabase(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DATABASE, 1);
    request.onupgradeneeded = () => {
      request.result.createObjectStore(STORE, { keyPath: "id" });
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () =>
      reject(request.error ?? new Error("Unable to open upload draft storage."));
  });
}

async function transact<T>(
  mode: IDBTransactionMode,
  operation: (store: IDBObjectStore) => IDBRequest<T>,
): Promise<T> {
  const db = await openDatabase();
  try {
    return await new Promise<T>((resolve, reject) => {
      const transaction = db.transaction(STORE, mode);
      const request = operation(transaction.objectStore(STORE));
      transaction.oncomplete = () => resolve(request.result);
      transaction.onabort = () =>
        reject(
          transaction.error ?? request.error ?? new Error("Upload draft transaction aborted."),
        );
      transaction.onerror = () =>
        reject(
          transaction.error ?? request.error ?? new Error("Unable to access upload draft storage."),
        );
    });
  } finally {
    db.close();
  }
}

export async function saveUploadDraft(draft: UploadDraft, notify = true): Promise<void> {
  await transact("readwrite", (store) => store.put(draft));
  if (notify || activeDraftIds.has(draft.id))
    window.dispatchEvent(new Event("upload-drafts:changed"));
}

export async function getUploadDraft(id: string): Promise<UploadDraft | undefined> {
  return transact("readonly", (store) => store.get(id));
}

export async function removeUploadDraft(id: string): Promise<void> {
  await transact("readwrite", (store) => store.delete(id));
  activeDraftIds.delete(id);
  window.dispatchEvent(new Event("upload-drafts:changed"));
}

export async function listUploadDrafts(userId: string): Promise<UploadDraft[]> {
  const all = await transact("readonly", (store) => store.getAll());
  const now = Date.now();
  const current: UploadDraft[] = [];
  for (const draft of all as UploadDraft[]) {
    if (draft.expiresAt <= now) {
      await transact("readwrite", (store) => store.delete(draft.id));
    } else if (draft.userId === userId) {
      current.push(draft);
    }
  }
  return current.sort((left, right) => left.createdAt - right.createdAt);
}

export function newUploadDraftFile(
  field: string,
  file: File,
  id: string = crypto.randomUUID(),
): UploadDraftFile {
  return { id, field, file, status: "pending" };
}

export function expiresIn24Hours(now = Date.now()): number {
  return now + DAY;
}
