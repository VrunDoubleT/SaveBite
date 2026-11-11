export function isUploadCancelled(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    (("name" in error && error.name === "AbortError") ||
      ("code" in error && error.code === "ERR_CANCELED"))
  );
}
