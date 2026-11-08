import type { UploadDraft, UploadDraftFile } from "./uploadDraftStore";

interface FileSelection {
  id?: string;
  field: string;
  file: File;
}

export function isSameUploadFile(left: FileSelection, right: FileSelection): boolean {
  return (
    left.field === right.field &&
    left.file.name === right.file.name &&
    left.file.size === right.file.size &&
    left.file.lastModified === right.file.lastModified
  );
}

/** Match each selected file once while preserving its uploaded asset and new display order. */
export function matchUploadFiles(
  existing: UploadDraftFile[],
  selected: FileSelection[],
): UploadDraftFile[] | undefined {
  if (existing.length !== selected.length) return undefined;
  const available = [...existing];
  const matched: UploadDraftFile[] = [];
  for (const candidate of selected) {
    const index = available.findIndex((file) => isSameUploadFile(file, candidate));
    if (index < 0) return undefined;
    matched.push(...available.splice(index, 1));
  }
  return matched;
}

export function remapDraftAssetIds(draft: UploadDraft, ids: Map<string, string>): void {
  const data = draft.formData;
  if (
    typeof data !== "object" ||
    data === null ||
    !("assetIds" in data) ||
    !Array.isArray(data.assetIds)
  )
    return;
  draft.formData = { ...data, assetIds: data.assetIds.map((id: string) => ids.get(id) ?? id) };
}
